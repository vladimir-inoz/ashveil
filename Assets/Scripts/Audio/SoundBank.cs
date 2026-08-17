using UnityEngine;

namespace Ashveil
{
    public static class SoundBank
    {
        static AudioSource _src;
        static AudioClip _plasma, _cannon, _missile, _boom, _engine;

        public static void Init(Transform root)
        {
            var go = new GameObject("Audio");
            go.transform.SetParent(root, false);
            _src = go.AddComponent<AudioSource>();
            _src.spatialBlend = 0f;
            _src.playOnAwake = false;
            _plasma = Tone(880, 0.06f, 0.18f);
            _cannon = Noise(0.05f, 0.22f);
            _missile = Sweep(220, 80, 0.28f);
            _boom = Noise(0.35f, 0.55f);
            _engine = Tone(70, 1.2f, 0.08f);
            var loop = go.AddComponent<AudioSource>();
            loop.clip = _engine;
            loop.loop = true;
            loop.volume = 0.12f;
            loop.Play();
        }

        public static void PlayPlasma(Vector3 _) { Play(_plasma, 0.22f, 0.95f + Random.Range(-0.05f, 0.08f)); }
        public static void PlayCannon(Vector3 _) { Play(_cannon, 0.18f, 0.7f + Random.Range(0f, 0.2f)); }
        public static void PlayMissile(Vector3 _) { Play(_missile, 0.35f, 1f); }
        public static void PlayExplosion(Vector3 _) { Play(_boom, 0.45f, 0.55f + Random.Range(0f, 0.2f)); }

        static void Play(AudioClip clip, float vol, float pitch)
        {
            if (_src == null || clip == null) return;
            _src.pitch = pitch;
            _src.PlayOneShot(clip, vol);
        }

        static AudioClip Tone(float hz, float dur, float vol)
        {
            int n = Mathf.CeilToInt(44100 * dur);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / 44100f;
                float env = Mathf.Exp(-t * 18f);
                data[i] = Mathf.Sin(2f * Mathf.PI * hz * t) * env * vol;
            }
            var c = AudioClip.Create("tone", n, 1, 44100, false);
            c.SetData(data, 0);
            return c;
        }

        static AudioClip Sweep(float from, float to, float dur)
        {
            int n = Mathf.CeilToInt(44100 * dur);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float hz = Mathf.Lerp(from, to, t);
                data[i] = Mathf.Sin(2f * Mathf.PI * hz * t * dur) * (1f - t) * 0.4f;
            }
            var c = AudioClip.Create("sweep", n, 1, 44100, false);
            c.SetData(data, 0);
            return c;
        }

        static AudioClip Noise(float dur, float vol)
        {
            int n = Mathf.CeilToInt(44100 * dur);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float env = 1f - i / (float)n;
                data[i] = (Random.value * 2f - 1f) * env * vol;
            }
            var c = AudioClip.Create("noise", n, 1, 44100, false);
            c.SetData(data, 0);
            return c;
        }
    }
}
