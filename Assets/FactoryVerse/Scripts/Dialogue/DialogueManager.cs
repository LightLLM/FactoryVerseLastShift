using System;
using System.Collections.Generic;
using UnityEngine;

namespace FactoryVerse
{
    /// <summary>
    /// Plays FactoryVerse dialogue one line at a time with subtitles for every line.
    /// - Critical lines (ARIA instructions/emergencies) jump ahead of everything queued and cut off an optional line in progress.
    /// - Optional lines (Engineer reactions) never wait: they are dropped if anything else is playing or queued.
    /// - Normal lines that sit in the queue too long go stale and are skipped, so story chatter never blocks the mission.
    /// Audio: put the voice clip at Resources/Dialogue/&lt;LineId&gt; (e.g. Resources/Dialogue/ARIA_M01_001.wav). Lines with no clip
    /// still show their subtitle, timed from the character's speaking rate.
    /// Subtitles are world-space and head-locked, so they work on Quest as well as on desktop.
    /// Use: DialogueManager.Say("ARIA_M01_001"); DialogueManager.PlaySequence("M03_Failure");
    /// </summary>
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        [Header("Timing")]
        public float gapBetweenLines = 0.35f;
        public float staleAfterSeconds = 20f;   // queued Normal/Optional lines older than this are skipped

        [Header("Subtitles")]
        public bool subtitles = true;
        public float subtitleDistance = 1.4f, subtitleDrop = 0.38f, subtitleFollow = 6f;

        public event Action<DialogueLine> LineStarted, LineFinished;
        public DialogueLine Current { get; private set; }
        /// <summary>True when nothing is speaking or waiting — story code can wait for a beat's lines to finish.</summary>
        public bool IsIdle => Current == null && _queue.Count == 0;
        public static bool Idle => !Instance || Instance.IsIdle;

        struct Queued { public DialogueLine Line; public float Time; }
        readonly List<Queued> _queue = new List<Queued>();
        readonly Dictionary<Speaker, AudioSource> _sources = new Dictionary<Speaker, AudioSource>();
        float _lineEnds, _nextAllowed;

        Transform _subtitleRoot; TextMesh _subtitleText; Transform _subtitleBack;

        static readonly Dictionary<Speaker, Color> SpeakerColor = new Dictionary<Speaker, Color>
        {
            { Speaker.ARIA, new Color(0.35f, 0.9f, 1f) },       // cyan
            { Speaker.Hale, new Color(1f, 0.42f, 0.38f) },      // alert red
            { Speaker.R7, new Color(0.9f, 0.93f, 1f) },         // light / white
            { Speaker.Engineer, new Color(1f, 0.62f, 0.2f) },   // engineer orange
        };
        static string Label(Speaker s) => s == Speaker.ARIA ? "ARIA" : s == Speaker.Hale ? "CMDR HALE" : s == Speaker.R7 ? "R-7" : "YOU";

        // ------------------------------------------------------------------ API
        public static void Say(string id)
        {
            var line = DialogueScript.Get(id);
            if (line == null) { Debug.LogWarning($"FactoryVerse dialogue: unknown line id '{id}'."); return; }
            Ensure().Enqueue(line);
        }

        public static void PlaySequence(string sequence)
        {
            if (!DialogueScript.Sequences.TryGetValue(sequence, out var ids)) { Debug.LogWarning($"FactoryVerse dialogue: unknown sequence '{sequence}'."); return; }
            var mgr = Ensure();
            // In a scripted sequence optional lines are part of the beat, so queue them as Normal instead of dropping them
            foreach (var id in ids)
            {
                var l = DialogueScript.Get(id);
                if (l != null) mgr.Enqueue(l, allowOptionalToWait: true);
            }
        }

        public static void StopAll()
        {
            if (!Instance) return;
            Instance._queue.Clear();
            Instance.EndCurrent();
        }

        static DialogueManager Ensure()
        {
            if (Instance) return Instance;
            return new GameObject("Dialogue Manager").AddComponent<DialogueManager>();
        }

        // ------------------------------------------------------------------ lifecycle
        void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            foreach (Speaker s in Enum.GetValues(typeof(Speaker))) _sources[s] = MakeSource(s);
            BuildSubtitle();
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        public void Enqueue(DialogueLine line, bool allowOptionalToWait = false)
        {
            bool busy = Current != null || _queue.Count > 0;
            if (line.Priority == DialoguePriority.Optional && busy && !allowOptionalToWait) return;   // never block on optional lines

            if (line.Priority == DialoguePriority.Critical && Current != null && Current.Priority == DialoguePriority.Optional)
                EndCurrent();   // urgent line cuts off an optional reaction

            // start loading the clip now (background load), so playback never waits on disk when the line comes up
            Resources.Load<AudioClip>(line.ResourcePath);

            // insert after everything of equal or higher priority (stable within a priority)
            int i = 0;
            while (i < _queue.Count && _queue[i].Line.Priority >= line.Priority) i++;
            _queue.Insert(i, new Queued { Line = line, Time = Time.time });
        }

        void Update()
        {
            if (Current != null && Time.time >= _lineEnds) EndCurrent();
            if (Current != null || Time.time < _nextAllowed) return;

            while (_queue.Count > 0)
            {
                var q = _queue[0]; _queue.RemoveAt(0);
                if (q.Line.Priority != DialoguePriority.Critical && Time.time - q.Time > staleAfterSeconds) continue;   // stale chatter
                Begin(q.Line);
                break;
            }
        }

        void LateUpdate() => FollowCamera(false);

        void Begin(DialogueLine line)
        {
            Current = line;
            var clip = Resources.Load<AudioClip>(line.ResourcePath);
            float duration;
            if (clip)
            {
                var src = _sources[line.Speaker];
                src.clip = clip; src.Play();
                duration = clip.length;
            }
            else
            {
                int words = line.Text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
                duration = Mathf.Max(1.6f, words / (DialogueScript.WordsPerMinute(line.Speaker) / 60f) + 0.6f);
            }
            _lineEnds = Time.time + duration;
            ShowSubtitle(line);
            LineStarted?.Invoke(line);
        }

        void EndCurrent()
        {
            if (Current == null) return;
            var done = Current;
            _sources[done.Speaker].Stop();
            Current = null;
            _nextAllowed = Time.time + gapBetweenLines;
            HideSubtitle();
            LineFinished?.Invoke(done);
        }

        // ------------------------------------------------------------------ audio treatment per character
        AudioSource MakeSource(Speaker s)
        {
            var go = new GameObject("Voice " + s);
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false; src.spatialBlend = 0f; src.volume = 1f;
            switch (s)
            {
                case Speaker.Hale:   // radio: band-limited, a little grit
                    go.AddComponent<AudioHighPassFilter>().cutoffFrequency = 320f;
                    go.AddComponent<AudioLowPassFilter>().cutoffFrequency = 3400f;
                    go.AddComponent<AudioDistortionFilter>().distortionLevel = 0.25f;
                    break;
                case Speaker.R7:     // subtle electronic modulation
                    var chorus = go.AddComponent<AudioChorusFilter>(); chorus.dryMix = 0.7f; chorus.wetMix1 = 0.35f; chorus.wetMix2 = 0f; chorus.wetMix3 = 0f; chorus.rate = 3f; chorus.depth = 0.08f;
                    go.AddComponent<AudioDistortionFilter>().distortionLevel = 0.12f;
                    break;
                case Speaker.ARIA:   // clean synthetic, minimal reverb
                    var rev = go.AddComponent<AudioReverbFilter>(); rev.reverbPreset = AudioReverbPreset.Room; rev.dryLevel = 0f; rev.room = -2500f;
                    break;
            }
            return src;
        }

        // ------------------------------------------------------------------ head-locked subtitles (VR + desktop)
        void BuildSubtitle()
        {
            _subtitleRoot = new GameObject("Subtitles").transform;
            _subtitleRoot.SetParent(transform, false);

            var back = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(back.GetComponent<Collider>());
            back.name = "Backing";
            back.transform.SetParent(_subtitleRoot, false);
            back.transform.localPosition = new Vector3(0f, 0f, 0.01f);
            var mat = new Material(Shader.Find("Sprites/Default")) { color = new Color(0.01f, 0.02f, 0.05f, 0.72f) };
            back.GetComponent<Renderer>().sharedMaterial = mat;
            _subtitleBack = back.transform;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(_subtitleRoot, false);
            _subtitleText = textGo.AddComponent<TextMesh>();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _subtitleText.font = font;
            textGo.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            _subtitleText.fontSize = 64; _subtitleText.characterSize = 0.006f;   // ~4 cm lines at 1.4 m: readable in a headset
            _subtitleText.anchor = TextAnchor.MiddleCenter; _subtitleText.alignment = TextAlignment.Center;
            _subtitleText.richText = true;
            _subtitleRoot.gameObject.SetActive(false);
        }

        void ShowSubtitle(DialogueLine line)
        {
            if (!subtitles || !_subtitleText) return;
            var c = ColorUtility.ToHtmlStringRGB(SpeakerColor[line.Speaker]);
            _subtitleText.text = $"<b><color=#{c}>{Label(line.Speaker)}</color></b>\n{Wrap(line.Text, 46)}";
            int rows = _subtitleText.text.Split('\n').Length;
            _subtitleBack.localScale = new Vector3(1.2f, 0.04f + rows * 0.062f, 1f);
            _subtitleRoot.gameObject.SetActive(true);
            FollowCamera(true);
        }

        void HideSubtitle() { if (_subtitleRoot) _subtitleRoot.gameObject.SetActive(false); }

        void FollowCamera(bool snap)
        {
            if (!_subtitleRoot || !_subtitleRoot.gameObject.activeSelf) return;
            var cam = Camera.main; if (!cam) return;
            var t = cam.transform;
            var flatForward = Vector3.ProjectOnPlane(t.forward, Vector3.up).normalized;
            if (flatForward.sqrMagnitude < 0.01f) flatForward = t.up;
            var target = t.position + flatForward * subtitleDistance + Vector3.down * subtitleDrop;
            var rot = Quaternion.LookRotation(target - t.position);
            float k = snap ? 1f : 1f - Mathf.Exp(-subtitleFollow * Time.deltaTime);   // lazy follow: comfortable in VR
            _subtitleRoot.position = Vector3.Lerp(_subtitleRoot.position, target, k);
            _subtitleRoot.rotation = Quaternion.Slerp(_subtitleRoot.rotation, rot, k);
        }

        static string Wrap(string text, int width)
        {
            var words = text.Split(' ');
            var sb = new System.Text.StringBuilder(); int col = 0;
            foreach (var w in words)
            {
                if (col > 0 && col + 1 + w.Length > width) { sb.Append('\n'); col = 0; }
                else if (col > 0) { sb.Append(' '); col++; }
                sb.Append(w); col += w.Length;
            }
            return sb.ToString();
        }
    }
}
