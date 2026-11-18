using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BrawlTCG.UnityApp;
using UnityEditor;
using UnityEngine;

namespace BrawlTCG.EditorTools
{
    /// <summary>
    /// BrawlTCG ▸ Showcase: plays the scripted trailer clips in Play Mode and, with Unity Recorder installed, saves each
    /// one as a 1080p60 MP4 in &lt;project&gt;/Recordings. Recorder is driven through reflection so the project still
    /// compiles without the package (the window offers to install it).
    /// </summary>
    public sealed class ShowcaseWindow : EditorWindow
    {
        [MenuItem("BrawlTCG/Showcase/Clips Window", priority = 0)]
        static void Open() => GetWindow<ShowcaseWindow>("Showcase").minSize = new Vector2(420, 300);

        [MenuItem("BrawlTCG/Showcase/Play All Clips", priority = 20)]
        static void PlayAll() => ShowcaseRecorder.Launch("all", false);

        [MenuItem("BrawlTCG/Showcase/Record All Clips", priority = 21)]
        static void RecordAll() => ShowcaseRecorder.Launch("all", true);

        [MenuItem("BrawlTCG/Showcase/Play Deck Building Clips", priority = 40)]
        static void PlayDecks() => ShowcaseRecorder.Launch("decks", false);

        [MenuItem("BrawlTCG/Showcase/Record Deck Building Clips", priority = 41)]
        static void RecordDecks() => ShowcaseRecorder.Launch("decks", true);

        Vector2 _scroll;

        void OnGUI()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Showcase clips", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Play runs a clip in Play Mode. Record also saves it as a 1080p60 MP4 in the Recordings folder (needs Unity Recorder). " +
                "Keep the mouse off the Game view while a clip plays.", MessageType.Info);

            if (!ShowcaseRecorder.RecorderAvailable)
            {
                EditorGUILayout.HelpBox("Unity Recorder is not installed, so clips can be played but not recorded.", MessageType.Warning);
                if (GUILayout.Button("Install Unity Recorder")) ShowcaseRecorder.InstallRecorder();
            }

            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Play all", GUILayout.Height(26))) ShowcaseRecorder.Launch("all", false);
                using (new EditorGUI.DisabledScope(!ShowcaseRecorder.RecorderAvailable))
                    if (GUILayout.Button("Record all", GUILayout.Height(26))) ShowcaseRecorder.Launch("all", true);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(6);

                _scroll = EditorGUILayout.BeginScrollView(_scroll);
                Section("MATCHES", "matches", ShowcaseClips.All);
                Section("DECK BUILDING", "decks", ShowcaseDecks.All);
                EditorGUILayout.EndScrollView();
            }

            if (GUILayout.Button("Open Recordings folder"))
            {
                Directory.CreateDirectory(ShowcaseRecorder.OutputFolder);
                EditorUtility.RevealInFinder(ShowcaseRecorder.OutputFolder);
            }
        }

        static void Section(string title, string group, System.Collections.Generic.IEnumerable<ShowcaseItem> items)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            if (GUILayout.Button("Play these", GUILayout.Width(90))) ShowcaseRecorder.Launch(group, false);
            using (new EditorGUI.DisabledScope(!ShowcaseRecorder.RecorderAvailable))
                if (GUILayout.Button("Record these", GUILayout.Width(100))) ShowcaseRecorder.Launch(group, true);
            EditorGUILayout.EndHorizontal();
            foreach (ShowcaseItem clip in items)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"{clip.Title}\n{clip.Subtitle}", EditorStyles.wordWrappedLabel, GUILayout.Height(34));
                if (GUILayout.Button("Play", GUILayout.Width(60), GUILayout.Height(30))) ShowcaseRecorder.Launch(clip.Id, false);
                using (new EditorGUI.DisabledScope(!ShowcaseRecorder.RecorderAvailable))
                    if (GUILayout.Button("Record", GUILayout.Width(60), GUILayout.Height(30))) ShowcaseRecorder.Launch(clip.Id, true);
                EditorGUILayout.EndHorizontal();
            }
        }
    }

    [InitializeOnLoad]
    static class ShowcaseRecorder
    {
        const string RecordKey = "BrawlTCG.Showcase.Record";
        const int Width = 1920, Height = 1080;
        const float FrameRate = 60;

        public static string OutputFolder => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Recordings"));

        static object _controller; // UnityEditor.Recorder.RecorderController while recording

        static ShowcaseRecorder()
        {
            ShowcaseDirector.ClipStarted += OnClipStarted;
            ShowcaseDirector.ClipEnded += OnClipEnded;
            ShowcaseDirector.AllDone += OnAllDone;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.ExitingPlayMode) StopRecording();
                if (state == PlayModeStateChange.EnteredEditMode) SessionState.EraseBool(RecordKey);
            };
        }

        static bool Recording => SessionState.GetBool(RecordKey, false);

        public static void Launch(string ids, bool record)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[Showcase] Stop Play Mode first.");
                return;
            }
            if (record && !RecorderAvailable)
            {
                Debug.LogWarning("[Showcase] Unity Recorder is not installed (BrawlTCG ▸ Showcase ▸ Clips Window ▸ Install).");
                return;
            }
            SessionState.SetString(ShowcaseDirector.RequestKey, ids);
            SessionState.SetBool(RecordKey, record);
            EditorApplication.EnterPlaymode();
        }

        static void OnClipStarted(ShowcaseItem clip)
        {
            if (Recording) StartRecording(clip.Id);
        }

        static void OnClipEnded(ShowcaseItem clip) => StopRecording();

        static void OnAllDone()
        {
            if (!Recording) return;
            StopRecording();
            Debug.Log($"[Showcase] Clips saved to {OutputFolder}");
            EditorApplication.ExitPlaymode();
            EditorApplication.delayCall += () => EditorUtility.RevealInFinder(OutputFolder);
        }

        // ───────────── Unity Recorder through reflection ─────────────

        static Type FindType(string fullName) =>
            AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(fullName, false)).FirstOrDefault(t => t != null);

        public static bool RecorderAvailable => FindType("UnityEditor.Recorder.RecorderController") != null;

        public static void InstallRecorder()
        {
            UnityEditor.PackageManager.Client.Add("com.unity.recorder");
            Debug.Log("[Showcase] Installing Unity Recorder… Recording is available once the import finishes.");
        }

        static void Set(object target, string property, object value)
        {
            PropertyInfo p = target.GetType().GetProperty(property, BindingFlags.Public | BindingFlags.Instance);
            if (p == null || !p.CanWrite) { Debug.LogWarning($"[Showcase] Recorder has no settable {target.GetType().Name}.{property}"); return; }
            if (value is string s && p.PropertyType.IsEnum) value = Enum.Parse(p.PropertyType, s);
            p.SetValue(target, value);
        }

        static object Get(object target, string property) =>
            target.GetType().GetProperty(property, BindingFlags.Public | BindingFlags.Instance)?.GetValue(target);

        static object Call(object target, string method, params object[] args)
        {
            MethodInfo m = target.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .First(x => x.Name == method && x.GetParameters().Length == args.Length);
            return m.Invoke(target, args);
        }

        static void StartRecording(string clipId)
        {
            StopRecording();
            try
            {
                Directory.CreateDirectory(OutputFolder);
                var settings = ScriptableObject.CreateInstance(FindType("UnityEditor.Recorder.RecorderControllerSettings"));
                var movie = ScriptableObject.CreateInstance(FindType("UnityEditor.Recorder.MovieRecorderSettings"));
                movie.name = "BrawlTCG Showcase";
                Set(movie, "Enabled", true);

                object input = Activator.CreateInstance(FindType("UnityEditor.Recorder.Input.GameViewInputSettings"));
                Set(input, "OutputWidth", Width);
                Set(input, "OutputHeight", Height);
                Set(movie, "ImageInputSettings", input);

                object audio = Get(movie, "AudioInputSettings");
                if (audio != null) Set(audio, "PreserveAudio", true);

                // MP4 (H.264) is the default; ask for the high quality preset when this Recorder version has it.
                Type encoderType = FindType("UnityEditor.Recorder.Encoder.CoreEncoderSettings");
                if (encoderType != null)
                {
                    object encoder = Activator.CreateInstance(encoderType);
                    Set(encoder, "Codec", "MP4");
                    Set(encoder, "EncodingQuality", "High");
                    Set(movie, "EncoderSettings", encoder);
                }

                string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
                Set(movie, "OutputFile", Path.Combine(OutputFolder, $"{clipId}_{stamp}").Replace('\\', '/'));

                Call(settings, "AddRecorderSettings", movie);
                Call(settings, "SetRecordModeToManual");
                Set(settings, "FrameRate", FrameRate);

                Type controllerType = FindType("UnityEditor.Recorder.RecorderController");
                ConstructorInfo ctor = controllerType.GetConstructor(new[] { settings.GetType() })
                    ?? throw new MissingMethodException("RecorderController(RecorderControllerSettings) not found");
                _controller = ctor.Invoke(new object[] { settings });
                Call(_controller, "PrepareRecording");
                Call(_controller, "StartRecording");
                Debug.Log($"[Showcase] Recording {clipId}");
            }
            catch (Exception e)
            {
                _controller = null;
                Debug.LogError($"[Showcase] Could not start Unity Recorder: {e.GetBaseException().Message}");
            }
        }

        static void StopRecording()
        {
            if (_controller == null) return;
            try { Call(_controller, "StopRecording"); }
            catch (Exception e) { Debug.LogError($"[Showcase] Could not stop Unity Recorder: {e.GetBaseException().Message}"); }
            _controller = null;
        }
    }
}
