using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Splines;

namespace ScaredyCat
{
    [DefaultExecutionOrder(-50)]
    public sealed class ApartmentIntroDirector : MonoBehaviour
    {
        public CutsceneSequence sequence;
        public CutsceneManager manager;
        public SplineContainer spline;
        public CatMotor cat;
        public OwnerRagdoll owner;
        public Transform bedroomDoor;
        public GameObject ownerCard, catCard;
        [Min(1)] public int ownerKnotIndex = 3;
        [Min(0.1f)] public float ownerApproachDuration = 2.8f;
        [Min(0.1f)] public float ownerHoldDuration = 2.8f;
        [Min(0.1f)] public float catApproachDuration = 4.6f;
        [Min(0.1f)] public float catHoldDuration = 2.4f;
        [Range(45, 120)] public float doorOpenAngle = 100;
        [Min(0.1f)] public float doorOpenDuration = 1.1f;

        Quaternion doorClosedRotation;
        Coroutine doorAnimation;
        bool doorOpened;
        bool ownerDanceStarted;

        void Awake()
        {
            if (!sequence) sequence = GetComponent<CutsceneSequence>();
            if (!manager) manager = FindFirstObjectByType<CutsceneManager>();
            if (bedroomDoor) doorClosedRotation = bedroomDoor.localRotation;
            if (ownerCard) ownerCard.SetActive(false);
            if (catCard) catCard.SetActive(false);

            EnsureOwnerWanderPoints();
            if (owner && owner.brain) owner.brain.Stay = true;
            ConfigureSequence();
            if (manager) manager.Finished += SequenceFinished;
        }

        void OnDestroy()
        {
            if (manager) manager.Finished -= SequenceFinished;
        }

        void ConfigureSequence()
        {
            if (!sequence || !spline || !cat || !owner) return;
            int knotCount = spline.Spline.Count;
            if (knotCount < 2) return;

            int knot = Mathf.Clamp(ownerKnotIndex, 1, knotCount - 1);
            float pauseAt = knot / (float)(knotCount - 1);
            var ownerLook = owner.transform;

            var approachOwner = SplineShot("Approach owner", 0, pauseAt, ownerApproachDuration,
                ownerLook, Vector3.up * 1.2f, "Someone is very committed to this routine.");
            approachOwner.onEnter.AddListener(BeginOwnerDance);

            var ownerHold = SplineShot("Owner introduction", pauseAt, pauseAt, ownerHoldDuration,
                ownerLook, Vector3.up * 1.2f, "Meet the human. The dancing is apparently normal.");
            ownerHold.onEnter.AddListener(() => { if (ownerCard) ownerCard.SetActive(true); });

            var revealBedroom = SplineShot("Bedroom door opens", pauseAt, 1, catApproachDuration,
                cat.head, Vector3.up * .08f, "And somewhere nearby, a cat is settling in.");
            revealBedroom.onEnter.AddListener(() =>
            {
                if (ownerCard) ownerCard.SetActive(false);
                OpenBedroomDoor();
            });

            var catHold = SplineShot("Cheeto under the spotlight", 1, 1, catHoldDuration,
                cat.head, Vector3.up * .08f, "Cheeto. New home. No plans to behave.");
            catHold.onEnter.AddListener(() => { if (catCard) catCard.SetActive(true); });

            sequence.shots = new[] { approachOwner, ownerHold, revealBedroom, catHold };
        }

        CameraShot SplineShot(string label, float start, float end, float duration,
            Transform target, Vector3 targetOffset, string caption)
        {
            return new CameraShot
            {
                label = label,
                spline = this.spline,
                splineStart = start,
                splineEnd = end,
                duration = duration,
                blendIn = .18f,
                fieldOfView = 62,
                lookAt = target,
                lookAtOffset = targetOffset,
                caption = caption
            };
        }

        void BeginOwnerDance()
        {
            if (ownerDanceStarted) return;
            ownerDanceStarted = true;
            if (owner && owner.brain) owner.brain.BeginIntroDance();
        }

        void EnsureOwnerWanderPoints()
        {
            if (!owner || !owner.brain) return;
            if (owner.brain.wanderPoints != null && System.Array.Exists(owner.brain.wanderPoints, point => point != null)) return;

            Vector3[] candidates =
            {
                owner.transform.position + Vector3.right * 1.5f,
                new Vector3(-.8f, 0, -5.3f),
                new Vector3(1.3f, 0, -4.7f),
                new Vector3(.8f, 0, -.7f),
                new Vector3(-1.3f, 0, -2.8f),
                cat ? cat.transform.position + Vector3.left * 1.5f : Vector3.zero
            };
            var points = new List<Transform>();
            foreach (var candidate in candidates)
            {
                Vector3 position = candidate;
                if (NavMesh.SamplePosition(candidate, out var sample, 1.5f, NavMesh.AllAreas)) position = sample.position;
                bool duplicate = false;
                foreach (var existing in points)
                    if ((existing.position - position).sqrMagnitude < 1.25f * 1.25f) { duplicate = true; break; }
                if (duplicate) continue;

                var point = new GameObject("Owner roam point " + (points.Count + 1)).transform;
                point.SetParent(transform.parent, true);
                point.position = position;
                points.Add(point);
            }
            owner.brain.wanderPoints = points.ToArray();
        }

        void OpenBedroomDoor()
        {
            if (!bedroomDoor || doorOpened) return;
            doorOpened = true;
            // The spline opens the panel visually. The NavMeshLink only helps
            // NavMeshAgents; the cat uses Rigidbody movement and still needs a clear collider gap.
            SetBedroomDoorBlocking(false);
            doorAnimation = StartCoroutine(OpenDoorRoutine());
        }

        IEnumerator OpenDoorRoutine()
        {
            Quaternion openRotation = doorClosedRotation * Quaternion.Euler(0, doorOpenAngle, 0);
            for (float elapsed = 0; elapsed < doorOpenDuration; elapsed += Time.deltaTime)
            {
                bedroomDoor.localRotation = Quaternion.Slerp(doorClosedRotation, openRotation,
                    Mathf.SmoothStep(0, 1, elapsed / doorOpenDuration));
                yield return null;
            }
            bedroomDoor.localRotation = openRotation;
            doorAnimation = null;
        }

        void SequenceFinished(CutsceneSequence finished, SequenceResult result)
        {
            if (finished != sequence) return;
            if (ownerCard) ownerCard.SetActive(false);
            if (catCard) catCard.SetActive(false);
            BeginOwnerDance();
            if (result == SequenceResult.Completed) OpenBedroomDoor();
            else OpenBedroomDoorImmediately();
        }

        void OpenBedroomDoorImmediately()
        {
            if (!bedroomDoor) return;
            if (doorAnimation != null) StopCoroutine(doorAnimation);
            doorAnimation = null;
            doorOpened = true;
            SetBedroomDoorBlocking(false);
            bedroomDoor.localRotation = doorClosedRotation * Quaternion.Euler(0, doorOpenAngle, 0);
        }

        void SetBedroomDoorBlocking(bool blocked)
        {
            foreach (var collider in bedroomDoor.GetComponentsInChildren<Collider>(true))
                collider.enabled = blocked;
        }
    }
}
