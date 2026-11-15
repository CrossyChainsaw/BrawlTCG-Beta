using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrawlTCG.UnityApp
{
    /// <summary>
    /// Sound effects. A cue name maps to Resources/BrawlTCG/Audio/&lt;cue&gt;/ (one clip is picked at random, so a cue
    /// can have variations) or to a single clip Resources/BrawlTCG/Audio/&lt;cue&gt;. Missing cues are silent, so the
    /// game works before every sound exists.
    /// </summary>
    public static class Sfx
    {
        const int Voices = 12;
        static readonly Dictionary<string, AudioClip[]> Cache = new Dictionary<string, AudioClip[]>();
        static readonly Dictionary<string, float> LastPlayed = new Dictionary<string, float>();
        static AudioSource[] _pool;
        static int _next;

        public static float Volume
        {
            get => PlayerPrefs.GetFloat("BrawlTCG.SfxVolume", 0.8f);
            set { PlayerPrefs.SetFloat("BrawlTCG.SfxVolume", Mathf.Clamp01(value)); PlayerPrefs.Save(); }
        }

        public static bool Muted
        {
            get => PlayerPrefs.GetInt("BrawlTCG.Muted", 0) == 1;
            set
            {
                PlayerPrefs.SetInt("BrawlTCG.Muted", value ? 1 : 0);
                PlayerPrefs.Save();
                AudioListener.volume = value ? 0 : 1;
            }
        }

        static void EnsurePool()
        {
            if (_pool != null && _pool[0] != null) return;
            var go = new GameObject("Sfx");
            UnityEngine.Object.DontDestroyOnLoad(go);
            _pool = new AudioSource[Voices];
            for (int i = 0; i < Voices; i++)
            {
                _pool[i] = go.AddComponent<AudioSource>();
                _pool[i].playOnAwake = false;
            }
            AudioListener.volume = Muted ? 0 : 1;
        }

        static AudioClip[] Clips(string cue)
        {
            if (Cache.TryGetValue(cue, out AudioClip[] clips)) return clips;
            clips = Resources.LoadAll<AudioClip>("BrawlTCG/Audio/" + cue);
            if (clips.Length == 0)
            {
                var single = Resources.Load<AudioClip>("BrawlTCG/Audio/" + cue);
                clips = single != null ? new[] { single } : Array.Empty<AudioClip>();
            }
            Cache[cue] = clips;
            return clips;
        }

        /// <summary>Plays a cue. The same cue is not restarted within <paramref name="minGap"/> seconds (avoids stacking).</summary>
        public static void Play(string cue, float volume = 1, float pitchJitter = 0.05f, float minGap = 0.04f)
        {
            AudioClip[] clips = Clips(cue);
            if (clips.Length == 0) return;
            float now = Time.unscaledTime;
            if (LastPlayed.TryGetValue(cue, out float last) && now - last < minGap) return;
            LastPlayed[cue] = now;
            EnsurePool();
            AudioSource src = _pool[_next];
            _next = (_next + 1) % _pool.Length;
            src.clip = clips[UnityEngine.Random.Range(0, clips.Length)];
            src.volume = Volume * volume;
            src.pitch = 1 + UnityEngine.Random.Range(-pitchJitter, pitchJitter);
            src.Play();
        }
    }
}
