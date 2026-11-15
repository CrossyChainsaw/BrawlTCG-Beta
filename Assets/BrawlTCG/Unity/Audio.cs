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

    /// <summary>
    /// Background music: a frame drum (daf) synthesised in code, with no melodic instruments and no jingles.
    /// Optional vocal layers (Resources/BrawlTCG/Audio/music/&lt;track&gt;_voice) are mixed on top when present.
    /// </summary>
    public sealed class Music : MonoBehaviour
    {
        public enum Track { None, Menu, Battle }

        static Music _instance;
        AudioSource _a, _b, _voice;
        Track _current;
        float _fade = 1;
        readonly Dictionary<Track, AudioClip> _clips = new Dictionary<Track, AudioClip>();
        AudioClip[] _voices = Array.Empty<AudioClip>();
        float _nextVoice, _barSeconds = 2.5f;

        public static float Volume
        {
            get => PlayerPrefs.GetFloat("BrawlTCG.MusicVolume", 0.6f);
            set { PlayerPrefs.SetFloat("BrawlTCG.MusicVolume", Mathf.Clamp01(value)); PlayerPrefs.Save(); }
        }

        static Music Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("Music");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<Music>();
                    _instance._a = go.AddComponent<AudioSource>();
                    _instance._b = go.AddComponent<AudioSource>();
                    foreach (var s in new[] { _instance._a, _instance._b }) { s.loop = true; s.playOnAwake = false; s.volume = 0; }
                    // Voices sit in a big hall so a few crowd recordings sound like a war band.
                    var voiceGo = new GameObject("Voices");
                    voiceGo.transform.SetParent(go.transform);
                    _instance._voice = voiceGo.AddComponent<AudioSource>();
                    _instance._voice.playOnAwake = false;
                    var hall = voiceGo.AddComponent<AudioReverbFilter>();
                    hall.reverbPreset = AudioReverbPreset.Concerthall;
                }
                return _instance;
            }
        }

        /// <summary>Cross-fades to a track (no-op if it is already playing).</summary>
        public static void Play(Track track) => Instance.Switch(track);

        void Switch(Track track)
        {
            if (track == _current) return;
            _current = track;
            // The old track becomes "b" and fades out; the new one fades in on "a".
            (_a, _b) = (_b, _a);
            if (track == Track.None) { _a.Stop(); }
            else
            {
                DafSynth.Pattern pattern = track == Track.Battle ? DafSynth.Battle : DafSynth.Menu;
                _barSeconds = 240f / pattern.Bpm;
                if (!_clips.TryGetValue(track, out AudioClip clip))
                    _clips[track] = clip = DafSynth.Render(pattern);
                _a.clip = clip;
                _a.volume = 0;
                _a.Play();
                _voices = Resources.LoadAll<AudioClip>("BrawlTCG/Audio/music/" + track.ToString().ToLowerInvariant() + "_voice");
                _nextVoice = Time.unscaledTime + 6;
            }
            _fade = 0;
        }

        void Update()
        {
            _fade = Mathf.MoveTowards(_fade, 1, Time.unscaledDeltaTime / 1.5f);
            float v = Volume;
            _a.volume = _current == Track.None ? 0 : v * _fade;
            _b.volume = v * (1 - _fade);
            if (_fade >= 1 && _b.isPlaying) _b.Stop();

            // Crowd chants / roars layered over the drums, always starting on a bar line.
            if (_voices.Length > 0 && _current != Track.None && _a.isPlaying && _a.clip != null
                && Time.unscaledTime >= _nextVoice && !_voice.isPlaying)
            {
                float t = (float)_a.timeSamples / _a.clip.frequency;
                float wait = Mathf.Ceil(t / _barSeconds + 0.001f) * _barSeconds - t;
                _voice.clip = _voices[UnityEngine.Random.Range(0, _voices.Length)];
                _voice.volume = v * 0.75f;
                _voice.PlayScheduled(AudioSettings.dspTime + wait);
                // Next one a few bars after this ends.
                _nextVoice = Time.unscaledTime + wait + _voice.clip.length + _barSeconds * UnityEngine.Random.Range(1, 4);
            }
        }
    }

    /// <summary>
    /// Renders a looping daf rhythm with an optional hand-clap layer. Strokes: D = dum (deep centre hit), T = tek (rim),
    /// K = ka (light rim), B = big low war-drum accent, C = group clap, c = soft clap, . = rest. One character per 16th note.
    /// </summary>
    public static class DafSynth
    {
        public sealed class Pattern
        {
            public float Bpm;
            public string[] Bars;
            public float Reverb;
            /// <summary>Optional second layer (claps), same bar layout as <see cref="Bars"/>.</summary>
            public string[] Claps;
        }

        /// <summary>Marching war rhythm with a big accent every four bars.</summary>
        public static readonly Pattern Battle = new Pattern
        {
            Bpm = 96,
            Reverb = 0.28f,
            Bars = new[]
            {
                "B...T.K.D...T...", "D...T.K.D.D.T...", "D...T.K.D...T.KT", "D.D.T...D.KTT.KT",
                "B...T.K.D...T...", "D...T.K.D.D.T...", "D..KT.K.D..KT.K.", "B.D.T.D.B.D.TTTT",
            },
            // Viking-clap feel: claps join in the second half and build into the turnaround.
            Claps = new[]
            {
                "................", "................", "....c.......c...", "....c.......c...",
                "....C.......C...", "....C.......C...", "....C.......C.C.", "C...C...C.C.CCCC",
            },
        };

        /// <summary>Slow, sparse heartbeat for the menus.</summary>
        public static readonly Pattern Menu = new Pattern
        {
            Bpm = 70,
            Reverb = 0.42f,
            Bars = new[]
            {
                "B.......T.......", "D.......T...K...", "D.......T.......", "D...D...T...K...",
            },
        };

        const int Rate = 44100;

        public static AudioClip Render(Pattern p)
        {
            float step = 60f / p.Bpm / 4f; // a 16th note
            int steps = p.Bars.Length * 16;
            int length = Mathf.CeilToInt(steps * step * Rate);
            var buf = new float[length];
            var rng = new System.Random(7);
            for (int layer = 0; layer < 2; layer++)
            for (int bar = 0; bar < p.Bars.Length; bar++)
            {
                string[] bars = layer == 0 ? p.Bars : p.Claps;
                if (bars == null || bar >= bars.Length) continue;
                string s = bars[bar];
                for (int i = 0; i < 16 && i < s.Length; i++)
                {
                    int at = (int)(((bar * 16) + i) * step * Rate);
                    // A touch of human timing and dynamics.
                    at += rng.Next(-60, 60);
                    float vel = 0.85f + (float)rng.NextDouble() * 0.15f;
                    switch (s[i])
                    {
                        case 'D': Dum(buf, at, vel, 1f, rng); break;
                        case 'B': Dum(buf, at, vel, 1.7f, rng); break;
                        case 'T': Tek(buf, at, vel * 0.8f, rng); break;
                        case 'K': Tek(buf, at, vel * 0.45f, rng); break;
                        case 'C': Clap(buf, at, vel * 0.75f, rng); break;
                        case 'c': Clap(buf, at, vel * 0.4f, rng); break;
                    }
                }
            }
            ApplyReverb(buf, p.Reverb);
            Normalize(buf, 0.85f);
            var clip = AudioClip.Create("Daf " + p.Bpm, length, 1, Rate, false);
            clip.SetData(buf, 0);
            return clip;
        }

        /// <summary>Centre stroke: a low membrane thump with a falling pitch. <paramref name="size"/> &gt; 1 is a bigger, deeper drum.</summary>
        static void Dum(float[] buf, int at, float vel, float size, System.Random rng)
        {
            float f0 = 118f / size, f1 = 62f / size;
            float decay = 0.32f * size;
            int n = (int)(Rate * decay * 3);
            double phase = 0;
            for (int i = 0; i < n; i++)
            {
                int k = ((at + i) % buf.Length + buf.Length) % buf.Length; // ring past the loop end into its start
                float t = (float)i / Rate;
                float f = f1 + (f0 - f1) * Mathf.Exp(-t * 28);
                phase += 2 * Math.PI * f / Rate;
                float body = (float)Math.Sin(phase) * Mathf.Exp(-t / decay);
                float overtone = (float)Math.Sin(phase * 2.3) * 0.25f * Mathf.Exp(-t / (decay * 0.3f));
                float skin = ((float)rng.NextDouble() * 2 - 1) * 0.35f * Mathf.Exp(-t * 90); // palm on the skin
                buf[k] += (body + overtone + skin) * vel * (size > 1 ? 1.15f : 0.9f);
            }
        }

        /// <summary>Rim stroke: short, bright, knocky.</summary>
        static void Tek(float[] buf, int at, float vel, System.Random rng)
        {
            int n = (int)(Rate * 0.18f);
            double phase = 0;
            float lp = 0;
            for (int i = 0; i < n; i++)
            {
                int k = ((at + i) % buf.Length + buf.Length) % buf.Length; // ring past the loop end into its start
                float t = (float)i / Rate;
                phase += 2 * Math.PI * 340 / Rate;
                float tone = (float)Math.Sin(phase) * Mathf.Exp(-t * 38);
                float noise = (float)rng.NextDouble() * 2 - 1;
                lp += (noise - lp) * 0.35f; // soften the noise
                float crack = lp * Mathf.Exp(-t * 70);
                buf[k] += (tone * 0.55f + crack * 0.8f) * vel;
            }
        }

        /// <summary>A group hand clap: several clappers, each a few quick filtered noise bursts, slightly out of sync.</summary>
        static void Clap(float[] buf, int at, float vel, System.Random rng)
        {
            for (int person = 0; person < 6; person++)
            {
                int start = at + rng.Next(0, Rate / 40); // up to 25 ms apart
                float gain = vel * (0.6f + (float)rng.NextDouble() * 0.4f);
                float centre = 1100 + (float)rng.NextDouble() * 900; // each pair of hands sounds a bit different
                float f = 2 * Mathf.Sin(Mathf.PI * centre / Rate);
                float low = 0, band = 0;
                int n = (int)(Rate * 0.12f);
                for (int i = 0; i < n; i++)
                {
                    float t = (float)i / Rate;
                    // Three short bursts (the "flam" of a real clap), then a short room tail.
                    float env = t < 0.03f ? Mathf.Exp(-(t % 0.01f) * 600) : Mathf.Exp(-(t - 0.03f) * 45) * 0.6f;
                    float noise = (float)rng.NextDouble() * 2 - 1;
                    low += f * band;
                    float high = noise - low - 0.6f * band;
                    band += f * high;
                    int k = ((start + i) % buf.Length + buf.Length) % buf.Length;
                    buf[k] += band * env * gain * 0.5f;
                }
            }
        }

        /// <summary>A small room: a few feedback delays.</summary>
        static void ApplyReverb(float[] buf, float amount)
        {
            if (amount <= 0) return;
            // Run over two copies of the loop and keep the second, so the tail of the loop rings into its start
            // and the clip repeats seamlessly.
            int n = buf.Length;
            int[] taps = { 1601, 2269, 3011, 4021 };
            var wet = new float[n];
            var line = new float[n * 2];
            foreach (int d in taps)
            {
                const float g = 0.55f;
                Array.Clear(line, 0, line.Length);
                for (int i = 0; i < n * 2; i++)
                {
                    float delayed = i >= d ? line[i - d] : 0;
                    line[i] = buf[i % n] + delayed * g;
                    if (i >= n) wet[i - n] += delayed;
                }
            }
            for (int i = 0; i < n; i++) buf[i] += wet[i] * amount / taps.Length;
        }

        static void Normalize(float[] buf, float peak)
        {
            float max = 0.0001f;
            foreach (float v in buf) max = Mathf.Max(max, Mathf.Abs(v));
            float g = peak / max;
            for (int i = 0; i < buf.Length; i++) buf[i] *= g;
        }
    }
}
