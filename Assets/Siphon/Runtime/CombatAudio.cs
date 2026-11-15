using UnityEngine;

namespace Siphon
{
    public sealed class CombatAudio : MonoBehaviour
    {
        private static CombatAudio instance;
        private readonly AudioSource[] voices = new AudioSource[16];
        private AudioClip shot, heavy, impact, signal;
        private int voice;
        private void Awake()
        {
            instance = this;
            shot = Tone("Distant rifle", 190, 0.09f, 0.45f);
            heavy = Tone("Cannon report", 65, 0.24f, 0.65f);
            impact = Tone("Impact", 95, 0.12f, 0.9f);
            signal = Tone("Field signal", 580, 0.12f, 0.05f);
            for (int i = 0; i < voices.Length; i++)
            {
                var root = new GameObject("Spatial voice"); root.transform.SetParent(transform);
                var source = voices[i] = root.AddComponent<AudioSource>(); source.playOnAwake = false;
                source.spatialBlend = 1; source.minDistance = 5; source.maxDistance = 130;
                source.rolloffMode = AudioRolloffMode.Linear; source.dopplerLevel = 0.15f;
            }
        }
        public static void Play(Vector3 position, int kind, float volume = 0.3f)
        {
            if (instance == null)
                instance = new GameObject("Combat audio").AddComponent<CombatAudio>();
            var source = instance.voices[instance.voice++ % instance.voices.Length];
            source.transform.position = position;
            source.clip = kind == 0 ? instance.shot : kind == 1 ? instance.heavy : kind == 2 ? instance.impact : instance.signal;
            source.volume = volume; source.pitch = Random.Range(0.93f, 1.07f); source.Play();
        }
        private static AudioClip Tone(string name, float pitch, float duration, float noise)
        {
            const int rate = 22050;
            var data = new float[(int)(rate * duration)]; var random = new System.Random(41);
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)rate, envelope = 1 - i / (float)data.Length;
                data[i] = (Mathf.Sin(t * pitch * 6.283f * (1 - t)) * (1 - noise)
                    + ((float)random.NextDouble() * 2 - 1) * noise) * envelope * envelope;
            }
            var clip = AudioClip.Create(name, data.Length, 1, rate, false); clip.SetData(data, 0); return clip;
        }
        private void OnDestroy()
        {
            if (instance == this) instance = null;
            Destroy(shot); Destroy(heavy); Destroy(impact); Destroy(signal);
        }
    }
}
