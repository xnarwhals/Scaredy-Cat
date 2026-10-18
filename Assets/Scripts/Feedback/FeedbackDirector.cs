using System;
using System.Collections;
using UnityEngine;
namespace ScaredyCat
{
    public sealed class FeedbackDirector : MonoBehaviour
    {
        public FeedbackSettings settings;
        public GameFlow flow;
        public CatMotor cat;
        public AgroMeter agro;
        public ParticleSystem sparks;
        public float MotionMultiplier { get; set; } = 1;
        public Vector3 CameraOffset { get; private set; }
        public Quaternion CameraRotation { get; private set; } = Quaternion.identity;
        readonly AudioSource[] voices = new AudioSource[6];
        AudioClip[] temporary;
        int voice;
        float shake;
        Coroutine impactRoutine;
        void Awake()
        {
            if (!flow) flow = FindFirstObjectByType<GameFlow>();
            temporary = new[] { Synth("Paw swish", .1f, 170, .8f), Synth("Bonk", .16f, 100, .25f),
                Synth("Ceramic", .3f, 850, .85f), Synth("Landing", .1f, 65, .25f),
                Synth("Calming", .15f, 650, .03f), Synth("At home", .9f, 330, .01f) };
            for (int i = 0; i < voices.Length; i++) { voices[i] = gameObject.AddComponent<AudioSource>(); voices[i].playOnAwake = false; }
        }
        void OnEnable() { if (cat) cat.Landed += Land; if (agro) agro.Rewarded += Reward; }
        void OnDisable()
        {
            if (cat) cat.Landed -= Land;
            if (agro) agro.Rewarded -= Reward;
            if (impactRoutine != null)
            {
                StopCoroutine(impactRoutine);
                impactRoutine = null;
                if (!flow || !flow.Paused) Time.timeScale = 1;
            }
        }
        void LateUpdate()
        {
            shake = Mathf.MoveTowards(shake, 0, Time.unscaledDeltaTime * 4.5f);
            float intensity = shake * (settings ? settings.cameraMotion : .6f) * MotionMultiplier;
            float t = Time.unscaledTime * 42;
            CameraOffset = new Vector3(Mathf.Sin(t), Mathf.Sin(t * 1.7f), 0) * intensity * .055f;
            CameraRotation = Quaternion.Euler(Mathf.Sin(t * .8f) * intensity * 3.2f, 0, Mathf.Sin(t * 1.3f) * intensity * 1.4f);
        }
        public void Attack(HitContext hit, bool connected)
        {
            Play(connected ? (settings ? settings.impact : null) : (settings ? settings.swish : null), connected ? 1 : 0);
            if (!connected) return;
            shake = Mathf.Max(shake, Mathf.Lerp(.45f, 1.15f, hit.Charge));
            Emit(hit.Point, Color.Lerp(new Color(1, .8f, .4f), Color.white, hit.Charge), 5);
        }
        public void SuccessfulHit(HitContext hit, Action resolveHit)
        {
            if (impactRoutine != null)
            {
                resolveHit?.Invoke();
                Attack(hit, true);
                return;
            }
            if (!settings || Time.timeScale <= 0 || (flow && flow.Paused))
            {
                resolveHit?.Invoke();
                Attack(hit, true);
                return;
            }
            impactRoutine = StartCoroutine(ImpactRoutine(hit, resolveHit));
        }
        IEnumerator ImpactRoutine(HitContext hit, Action resolveHit)
        {
            float originalScale = Time.timeScale;
            if (settings.preImpactSlowDuration > 0)
            {
                Time.timeScale = Mathf.Min(originalScale, settings.preImpactTimeScale);
                float elapsed = 0;
                while (elapsed < settings.preImpactSlowDuration)
                {
                    if (flow && flow.Paused)
                    {
                        resolveHit?.Invoke();
                        Attack(hit, true);
                        impactRoutine = null;
                        yield break;
                    }
                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            resolveHit?.Invoke();
            Attack(hit, true);
            if (settings.impactStopDuration > 0)
            {
                Time.timeScale = Mathf.Min(originalScale, settings.impactStopTimeScale);
                float elapsed = 0;
                while (elapsed < settings.impactStopDuration)
                {
                    if (flow && flow.Paused)
                    {
                        impactRoutine = null;
                        yield break;
                    }
                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }
            }
            if (!flow || !flow.Paused) Time.timeScale = originalScale;
            impactRoutine = null;
        }
        public void Shatter(Vector3 point) { Play(settings ? settings.shatter : null, 2); Emit(point, new Color(1, .85f, .55f), 12); shake = Mathf.Max(shake, .5f); }
        public void MarkTerritory(Vector3 point)
        {
            Emit(point, new Color(.32f, 1f, .48f), 18);
            shake = Mathf.Max(shake, .2f);
        }
        public void ZoomieStart(Vector3 point)
        {
            Play(settings ? settings.swish : null, 0, .75f);
            Emit(point + Vector3.up * .15f, new Color(.65f, .85f, 1f), 8);
            shake = Mathf.Max(shake, .16f);
        }
        void Land(float speed) { Play(settings ? settings.landing : null, 3, Mathf.Clamp01(speed / 12)); shake = Mathf.Max(shake, Mathf.Clamp01(speed / 15) * .45f); }
        void Reward(MischiefEvent outcome) => Play(settings ? settings.reward : null, 4, .4f);
        public void Accept() => Play(settings ? settings.acceptance : null, 5);
        void Emit(Vector3 point, Color color, int count)
        {
            if (!sparks) return;
            sparks.Emit(new ParticleSystem.EmitParams { position = point, startColor = color }, count);
        }
        void Play(AudioClip clip, int fallback, float volume = 1)
        {
            var source = voices[voice++ % voices.Length];
            source.pitch = UnityEngine.Random.Range(.94f, 1.07f); source.PlayOneShot(clip ? clip : temporary[fallback], volume * (settings ? settings.volume : .65f));
        }
        static AudioClip Synth(string name, float duration, float frequency, float noise)
        {
            const int rate = 22050;
            var samples = new float[Mathf.CeilToInt(duration * rate)]; var random = new System.Random(name.Length * 97);
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / rate;
                float envelope = Mathf.Min(1, t / .008f) * Mathf.Pow(1 - (float)i / samples.Length, 2);
                float tone = Mathf.Sin(2 * Mathf.PI * frequency * t * (1 - t * .3f));
                samples[i] = (tone * (1 - noise) + ((float)random.NextDouble() * 2 - 1) * noise) * envelope * .3f;
            }
            var clip = AudioClip.Create(name, samples.Length, 1, rate, false); clip.SetData(samples, 0); return clip;
        }
        void OnDestroy() { if (temporary != null) foreach (var clip in temporary) if (clip) Destroy(clip); }
    }
}
