using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace ScaredyCat.Tests
{
    public sealed class ProbeReceiver : MonoBehaviour, ISwatReceiver
    { public int Hits; public HitContext Last; public void ReceiveHit(HitContext hit) { Hits++; Last = hit; } }
    public sealed class FirstPlayableTests
    {
        GameFlow flow;
        CatMotor cat;
        PawAttack attack;
        [UnitySetUp] public IEnumerator LoadLab()
        {
            yield return SceneManager.LoadSceneAsync("MechanicsLab"); yield return null;
            flow = Object.FindFirstObjectByType<GameFlow>(); cat = flow.cat; attack = cat.GetComponent<PawAttack>();
            if (flow.Paused) flow.TogglePause();
            Assert.That(flow.State, Is.EqualTo(GameState.Playing));
        }
        [UnityTearDown] public IEnumerator Cleanup()
        { Time.timeScale = 1; AudioListener.pause = false; yield return null; }
        [UnityTest] public IEnumerator PlayerAcceleratesBrakesAndStopsAtWalls()
        {
            cat.input = null; cat.Teleport(new Vector3(-8, .03f, -6), 0);
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.transform.position = new Vector3(-8, 1, -4); wall.transform.localScale = new Vector3(2, 2, .2f);
            yield return new WaitForSeconds(.15f); cat.SetMovement(Vector2.up, true);
            yield return new WaitForSeconds(.2f); Assert.That(cat.Body.linearVelocity.z, Is.GreaterThan(4));
            yield return new WaitForSeconds(.6f); Assert.That(cat.transform.position.z, Is.LessThan(-4.2f));
            cat.SetMovement(Vector2.zero); yield return new WaitForSeconds(.2f);
            Assert.That(new Vector2(cat.Body.linearVelocity.x, cat.Body.linearVelocity.z).magnitude, Is.LessThan(.15f));
            Object.Destroy(wall);
        }
        [UnityTest] public IEnumerator BufferedJumpAndLowCeilingRespectCollision()
        {
            cat.input = null; cat.Teleport(new Vector3(-8, .13f, -6), 0); cat.RequestJump();
            yield return new WaitForSeconds(.14f); Assert.That(cat.Body.linearVelocity.y, Is.GreaterThan(1));
            cat.Teleport(new Vector3(-6, .03f, 3), 0); yield return new WaitForSeconds(.2f); cat.RequestJump();
            yield return new WaitForSeconds(.22f); Assert.That(cat.transform.position.y, Is.LessThan(.65f));
        }
        [UnityTest] public IEnumerator CoyoteJumpWorksAfterLeavingAnEdge()
        {
            cat.input = null;
            var platform = GameObject.CreatePrimitive(PrimitiveType.Cube); platform.transform.position = new Vector3(25, -.15f, 0); platform.transform.localScale = new Vector3(1, .3f, 2);
            cat.Teleport(new Vector3(25, .03f, 0), 0); yield return new WaitForSeconds(.2f); Assert.That(cat.Grounded, Is.True);
            cat.SetMovement(Vector2.right); float until = Time.time + 1;
            while (cat.Grounded && Time.time < until) yield return new WaitForFixedUpdate();
            Assert.That(cat.Grounded, Is.False); cat.RequestJump(); yield return new WaitForFixedUpdate(); yield return null;
            Assert.That(cat.Body.linearVelocity.y, Is.GreaterThan(1)); Object.Destroy(platform);
        }
        [UnityTest] public IEnumerator FallingOutOfBoundsRestoresThePlayer()
        {
            cat.Teleport(new Vector3(20, -30, 20), 0); yield return null; yield return new WaitForFixedUpdate();
            Assert.That(cat.transform.position.y, Is.GreaterThan(-1));
        }
        [UnityTest] public IEnumerator WalkableSlopeAndFurnitureJumpGainHeight()
        {
            cat.input = null; cat.Teleport(new Vector3(-3.5f, .03f, -1.7f), 0);
            yield return new WaitForSeconds(.2f); cat.SetMovement(Vector2.up);
            yield return new WaitForSeconds(.6f);
            Assert.That(cat.transform.position.y, Is.GreaterThan(.45f), "Walkable ramp should raise the cat.");
            cat.SetMovement(Vector2.zero); cat.Teleport(new Vector3(3, .03f, .5f), 0);
            yield return new WaitForSeconds(.2f); cat.RequestJump(); cat.SetMovement(Vector2.up);
            yield return new WaitForSeconds(.35f); cat.SetMovement(Vector2.zero);
            yield return new WaitForSeconds(.7f);
            Assert.That(cat.Grounded, Is.True);
            Assert.That(cat.transform.position.y, Is.InRange(.77f, .9f), "Jump should land on the table.");
        }
        GameObject Probe(out ProbeReceiver receiver)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube); obj.transform.position = new Vector3(25, 1, .7f); obj.transform.localScale = Vector3.one * .3f;
            receiver = obj.AddComponent<ProbeReceiver>();
            var aim = new GameObject("Test aim"); aim.transform.position = new Vector3(25, 1, 0); attack.aim = aim.transform;
            return obj;
        }
        [UnityTest] public IEnumerator ChargedSwatsAreStrongerAndCompoundCollidersCountOnce()
        {
            var obj = Probe(out var receiver); var extra = new GameObject("Second collider"); extra.transform.SetParent(obj.transform, false); extra.AddComponent<BoxCollider>();
            Physics.SyncTransforms(); attack.DebugStrike(0); Assert.That(receiver.Hits, Is.EqualTo(1)); float tap = receiver.Last.Impulse;
            yield return new WaitForSeconds(.3f); attack.DebugStrike(1);
            Assert.That(receiver.Hits, Is.EqualTo(2)); Assert.That(receiver.Last.Impulse, Is.GreaterThan(tap * 2));
            yield return new WaitForSeconds(.3f); Assert.That(attack.paw.gameObject.activeSelf, Is.False);
        }
        [UnityTest] public IEnumerator WallsOccludeTargetsAndCancelledChargeDoesNotStrike()
        {
            var obj = Probe(out var receiver);
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.transform.position = new Vector3(25, 1, .3f); wall.transform.localScale = new Vector3(1, 1, .1f);
            Physics.SyncTransforms(); attack.DebugStrike(1); Assert.That(receiver.Hits, Is.Zero);
            yield return new WaitForSeconds(.3f); Object.Destroy(wall); yield return null;
            attack.Press(); flow.TogglePause(); Assert.That(attack.State, Is.EqualTo(PawState.Idle));
            flow.TogglePause(); attack.Release(); Assert.That(receiver.Hits, Is.Zero);
        }
        [UnityTest] public IEnumerator FractureAwardsOnceAndRetainsPlayerAttribution()
        {
            var prop = Object.FindObjectsByType<MischiefProp>(FindObjectsSortMode.None).First(p => p.breakable);
            float before = flow.agro.Ledger.Value;
            prop.ReceiveHit(new HitContext(1, cat.gameObject, prop.transform.position, Vector3.up, 1, 4));
            Assert.That(prop.Broken, Is.True); Assert.That(flow.agro.Ledger.Value, Is.EqualTo(before - 10));
            var fragments = prop.brokenRoot.GetComponentsInChildren<Rigidbody>(); Assert.That(fragments.Length, Is.GreaterThan(1));
            Assert.That(fragments.All(f => !f.isKinematic && f.GetComponent<ImpulseAttribution>().Active), Is.True);
            prop.ReceiveHit(new HitContext(2, cat.gameObject, prop.transform.position, Vector3.up, 1, 4));
            prop.ResetProp(); prop.ReceiveHit(new HitContext(3, cat.gameObject, prop.transform.position, Vector3.up, 1, 4));
            Assert.That(flow.agro.Ledger.Value, Is.EqualTo(before - 10)); yield return null;
        }
        [UnityTest] public IEnumerator AmbientBreakHasNoRewardAndAttributionTransfersAcrossCollision()
        {
            yield return new WaitForSeconds(.6f); // Allow the spawn-settling grace period to finish.
            Assert.That(flow.agro.Ledger.Value, Is.EqualTo(100));
            var props = Object.FindObjectsByType<MischiefProp>(FindObjectsSortMode.None).Where(p => p.breakable).ToArray();
            props[0].Break(props[0].transform.position, Vector3.zero); Assert.That(flow.agro.Ledger.Value, Is.EqualTo(100));
            var target = props[1]; var targetBody = target.GetComponent<Rigidbody>();
            targetBody.position = new Vector3(25, 1, 1); target.transform.position = targetBody.position;
            targetBody.useGravity = false; targetBody.linearVelocity = Vector3.zero;
            Physics.SyncTransforms();
            var projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.transform.position = target.GetComponent<Collider>().bounds.center + Vector3.back * 2;
            projectile.transform.localScale = Vector3.one * .3f;
            var body = projectile.AddComponent<Rigidbody>(); body.useGravity = false; body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            projectile.AddComponent<ImpulseAttribution>().Mark(cat.gameObject, Time.timeAsDouble + 6); body.linearVelocity = Vector3.forward * 8;
            Physics.SyncTransforms(); yield return new WaitForSeconds(.5f);
            Assert.That(target.Broken, Is.True); Assert.That(flow.agro.Ledger.Value, Is.LessThan(100));
        }
        [UnityTest] public IEnumerator OwnerCanRecoverAndBeKnockedDownAgainWithoutDuplicateReward()
        {
            var owner = flow.owner; owner.recoveryDelay = .15f; owner.maximumDownTime = .4f; owner.blendDuration = .1f;
            owner.brain.Stay = true;
            for (int i = 0; i < 2; i++)
            {
                float before = flow.agro.Ledger.Value;
                var hit = new HitContext(i, cat.gameObject, owner.hips.position, Vector3.forward, 0, 1);
                owner.ReceiveHit(hit); owner.ReceiveHit(hit);
                Assert.That(flow.agro.Ledger.Value, Is.EqualTo(before - 12)); Assert.That(owner.animator.enabled, Is.False);
                float until = Time.time + 3;
                while (owner.State != OwnerState.Animated && Time.time < until) yield return null;
                Assert.That(owner.State, Is.EqualTo(OwnerState.Animated)); Assert.That(owner.animator.enabled, Is.True);
                Assert.That(owner.animator.isInitialized, Is.True);
                Assert.That(owner.animator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion"), Is.True);
                Assert.That(owner.bones.All(b => b.isKinematic), Is.True); Assert.That(owner.standingCollider.enabled, Is.True);
            }
        }
        [UnityTest] public IEnumerator OwnerWandersWithAnAdvancingLocomotionBlendTree()
        {
            var owner = flow.owner; yield return new WaitForSeconds(.2f);
            Assert.That(owner.animator.isInitialized, Is.True);
            Assert.That(owner.animator.avatar.isHuman, Is.True);
            float time = owner.animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            Vector3 start = owner.transform.position;
            float deadline = Time.time + 7;
            while (Vector3.Distance(start, owner.transform.position) < .5f && Time.time < deadline) yield return null;
            Assert.That(owner.animator.GetCurrentAnimatorStateInfo(0).normalizedTime, Is.GreaterThan(time));
            Assert.That(Vector3.Distance(start, owner.transform.position), Is.GreaterThan(.4f));
            Assert.That(owner.animator.GetFloat("Speed"), Is.GreaterThan(.1f));
        }
        [UnityTest] public IEnumerator OffNavigationRagdollRecoversToAClearFallback()
        {
            var owner = flow.owner; owner.brain.Stay = true;
            owner.ReceiveHit(new HitContext(1, cat.gameObject, owner.hips.position, Vector3.forward, 0, 1));
            foreach (var bone in owner.bones) bone.position += Vector3.right * 40;
            Physics.SyncTransforms();
            Assert.That(owner.TryRecoveryPosition(out var position), Is.True);
            Assert.That(Vector3.Distance(position, owner.brain.LastSafePosition), Is.LessThan(2));
            Assert.That(Physics.CheckCapsule(position + Vector3.up * .31f, position + Vector3.up * 1.35f,
                .27f, owner.recoveryObstacles, QueryTriggerInteraction.Ignore), Is.False);
            yield return null;
        }
        [UnityTest] public IEnumerator DebrisBudgetCapsSimultaneousFractures()
        {
            var budget = Object.FindFirstObjectByType<DebrisBudget>();
            foreach (var prop in Object.FindObjectsByType<MischiefProp>(FindObjectsSortMode.None))
                if (prop.breakable) prop.Break(prop.transform.position, Vector3.up);
            yield return new WaitForFixedUpdate();
            Assert.That(budget.ActiveCount, Is.EqualTo(128));
            Assert.That(flow.agro.Ledger.Value, Is.EqualTo(100));
        }
        [UnityTest] public IEnumerator SequenceSkipAndInvalidTargetsReleaseOnlyTheirOwnLock()
        {
            flow.cutscenes.Play(flow.intro); yield return null;
            Assert.That(flow.cutscenes.IsPlaying, Is.True); Assert.That(flow.input.Controls.IsLocked, Is.True);
            var external = flow.input.Controls.Acquire(); flow.cutscenes.Skip(); Assert.That(flow.input.Controls.IsLocked, Is.True);
            external.Dispose(); Assert.That(flow.input.Controls.IsLocked, Is.False);
            var test = new GameObject("Invalid target sequence").AddComponent<CutsceneSequence>(); test.waitForGrounded = false;
            var pose = new GameObject("Disposable pose").transform;
            test.shots = new[] { new CameraShot { pose = pose, duration = 5 } }; flow.cutscenes.Play(test); yield return null;
            Object.Destroy(pose.gameObject); yield return null; yield return null;
            Assert.That(flow.cutscenes.IsPlaying, Is.False); Assert.That(flow.input.Controls.IsLocked, Is.False);
            Assert.That(flow.cutscenes.director.Cinematic, Is.False);
        }
        [UnityTest] public IEnumerator PixelTextureIsPointFilteredAndCanBeResized()
        {
            var composition = Object.FindFirstObjectByType<PixelComposition>(); composition.pixelHeight = 240;
            yield return null; yield return null;
            Assert.That(composition.worldCamera.targetTexture, Is.Not.Null);
            Assert.That(composition.worldCamera.targetTexture.height, Is.EqualTo(240));
            Assert.That(composition.worldCamera.targetTexture.filterMode, Is.EqualTo(FilterMode.Point));
        }
        [UnityTest] public IEnumerator NarrativeCancelsChargeAndPauseFreezesItsClock()
        {
            Probe(out var receiver); attack.Press();
            var test = new GameObject("Pause test sequence").AddComponent<CutsceneSequence>(); test.waitForGrounded = false;
            var pose = new GameObject("Pause test pose").transform;
            var look = new GameObject("Pause test target").transform; look.position = Vector3.forward;
            test.shots = new[] { new CameraShot { pose = pose, lookAt = look, duration = .2f } };
            flow.cutscenes.Play(test); yield return null;
            Assert.That(attack.State, Is.EqualTo(PawState.Idle)); Assert.That(attack.paw.gameObject.activeSelf, Is.False);
            flow.TogglePause(); yield return new WaitForSecondsRealtime(.3f);
            Assert.That(flow.cutscenes.IsPlaying, Is.True);
            flow.TogglePause(); Object.Destroy(look.gameObject); yield return null; yield return null;
            Assert.That(flow.cutscenes.IsPlaying, Is.False); Assert.That(flow.input.Controls.IsLocked, Is.False);
            attack.Release(); Assert.That(receiver.Hits, Is.Zero);
        }
        [UnityTest] public IEnumerator ApartmentCompletesUsingPropsAndRestartsCleanly()
        {
            yield return SceneManager.LoadSceneAsync("Apartment"); yield return null;
            flow = Object.FindFirstObjectByType<GameFlow>(); cat = flow.cat;
            if (flow.Paused) flow.TogglePause();
            yield return null; flow.cutscenes.Skip(); yield return null; Assert.That(flow.State, Is.EqualTo(GameState.Playing));
            var props = Object.FindObjectsByType<MischiefProp>(FindObjectsSortMode.None);
            Assert.That(props.Where(p => p.breakable).Count() * 10, Is.GreaterThanOrEqualTo(130));
            foreach (var prop in props.Where(p => p.breakable)) prop.ReceiveHit(new HitContext(1, cat.gameObject, prop.transform.position, Vector3.up, 1, 2));
            float until = Time.time + 8; while (flow.State != GameState.Complete && Time.time < until) yield return null;
            Assert.That(flow.State, Is.EqualTo(GameState.Complete)); Assert.That(flow.agro.Ledger.Value, Is.Zero);
            Assert.That(flow.input.Controls.IsLocked, Is.True); Assert.That(flow.cutscenes.director.Cinematic, Is.True);
            flow.Restart(); yield return null; yield return null;
            var restarted = Object.FindFirstObjectByType<GameFlow>(); Assert.That(restarted.agro.Ledger.Value, Is.EqualTo(100));
            Assert.That(Object.FindObjectsByType<MischiefProp>(FindObjectsSortMode.None).All(p => !p.Broken), Is.True);
        }
    }
}
