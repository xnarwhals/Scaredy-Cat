using UnityEngine;
namespace ScaredyCat
{
    public sealed class FeedbackDirector : MonoBehaviour
    {
        public FeedbackSettings settings;
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
        void Awake()
        {
            temporary = new[] { Synth("Paw swish", .1f, 170, .8f), Synth("Bonk", .16f, 100, .25f),
                Synth("Ceramic", .3f, 850, .85f), Synth("Landing", .1f, 65, .25f),
                Synth("Calming", .15f, 650, .03f), Synth("At home", .9f, 330, .01f) };
            for (int i = 0; i < voices.Length; i++) { voices[i] = gameObject.AddComponent<AudioSource>(); voices[i].playOnAwake = false; }
        }
        void OnEnable() { if (cat) cat.Landed += Land; if (agro) agro.Rewarded += Reward; }
        void OnDisable() { if (cat) cat.Landed -= Land; if (agro) agro.Rewarded -= Reward; }
        void LateUpdate()
        {
            shake = Mathf.MoveTowards(shake, 0, Time.deltaTime * 3);
            float intensity = shake * (settings ? settings.cameraMotion : .6f) * MotionMultiplier;
            float t = Time.time * 42;
            CameraOffset = new Vector3(Mathf.Sin(t), Mathf.Sin(t * 1.7f), 0) * intensity * .025f;
            CameraRotation = Quaternion.Euler(Mathf.Sin(t * .8f) * intensity * 1.6f, 0, Mathf.Sin(t * 1.3f) * intensity);
        }
        public void Attack(HitContext hit, bool connected)
        {
            Play(connected ? (settings ? settings.impact : null) : (settings ? settings.swish : null), connected ? 1 : 0);
            if (!connected) return;
            shake = Mathf.Max(shake, Mathf.Lerp(.25f, .8f, hit.Charge));
            Emit(hit.Point, Color.Lerp(new Color(1, .8f, .4f), Color.white, hit.Charge), 5);
        }
        public void Shatter(Vector3 point) { Play(settings ? settings.shatter : null, 2); Emit(point, new Color(1, .85f, .55f), 12); shake = Mathf.Max(shake, .5f); }
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
            source.pitch = Random.Range(.94f, 1.07f); source.PlayOneShot(clip ? clip : temporary[fallback], volume * (settings ? settings.volume : .65f));
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
