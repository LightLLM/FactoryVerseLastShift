using UnityEngine;
using UnityEngine.SceneManagement;

namespace FactoryVerse
{
    /// <summary>
    /// Guided 3–4 minute demo of the FV-01 factory ("Last Shift"), playable on desktop or in VR:
    ///  1. ARIA briefs the engineer; walk to the F work cell and hold position to install the power core in Unit R-7.
    ///  2. R-7 boots, then a hull breach: Emergency lighting, Commander Hale orders evacuation via the west corridor.
    ///  3. Gravity fails in the maintenance corridor: Zero-G lighting and drifting debris.
    ///  4. Reach the escape pod bay and hold position inside the pod to launch; end card, then restart.
    /// Interactions are "stand in the glowing ring" dwell triggers, so they work the same with keyboard, controllers
    /// or hand tracking. Text is drawn in world space in front of the active camera, so it shows in the headset.
    /// </summary>
    public class FactoryDemoDirector : MonoBehaviour
    {
        [Header("Scene references (set by the builder)")]
        public FactoryLightingPresets lighting;
        public Transform robot;            // Unit R-7 behind the F workstation
        public GameObject robotCore;       // its chest power core (hidden until installed)
        public Transform robotHead;
        public Transform loosePart;        // power cell on the F workstation table
        public Material beaconMaterial, beamMaterial;
        public Font font;

        [Header("Trigger points (world)")]
        public Vector3 installPoint = new Vector3(-8f, 0f, -0.2f);
        public float corridorEntryX = -14.6f;
        public float podBayEntryX = -38.6f;
        public Vector3 podPoint = new Vector3(-44.2f, 0f, 0f);
        public float dwellSeconds = 2.5f, zoneRadius = 1.4f;

        [Header("Evacuation countdown")]
        [Tooltip("Seconds from the hull breach until the station is lost. Generous on purpose: it adds urgency, not difficulty.")]
        public float escapeSeconds = 180f;
        public float restartDelayEnd = 12f, restartDelayFailed = 9f;

        enum Beat { Intro, Install, Booting, Breach, Corridor, PodBay, Launch, End, Failed }
        float _escapeDeadline = -1f;
        string _objectiveBase = "";
        bool _restarting;
        Beat _beat = Beat.Intro;
        float _beatStart, _dwell;
        Transform _hud; TextMesh _objective, _subtitle, _card;
        Transform _beacon;
        Vector3 _partFrom, _partTo; Quaternion _headRest;
        string[] _lines; float _lineTime; int _lineIndex;

        void Start()
        {
            BuildHud();
            _beacon = BuildBeacon();
            if (robotCore) robotCore.SetActive(false);
            if (robotHead) _headRest = robotHead.localRotation;
            if (lighting) lighting.Set(FactoryLightingPresets.Preset.Normal);
            SetupGrab();
            Enter(Beat.Intro);
        }

        // Hands-first install: pinch the power cell on the table and release it at R-7's chest socket.
        // (Standing in the ring still works for desktop/controller play.)
        FactoryGrabbable _grab;
        void SetupGrab()
        {
            if (!loosePart) return;
            _grab = loosePart.GetComponent<FactoryGrabbable>();
            if (!_grab) _grab = loosePart.gameObject.AddComponent<FactoryGrabbable>();
            _grab.socket = robotCore ? robotCore.transform : robot;
            _grab.Installed += () => { if (_beat == Beat.Intro || _beat == Beat.Install) Enter(Beat.Booting); };
        }

        // ------------------------------------------------------------ beats
        void Enter(Beat b)
        {
            _beat = b; _beatStart = Time.time; _dwell = 0f;
            switch (b)
            {
                case Beat.Intro:
                    Voice("ARIA_M01_001", "HALE_M01_001", "ENG_M01_001", "ARIA_M01_002", "ARIA_M02_001");
                    Objective("Go to the glowing ring at the R-7 workstation");
                    Beacon(installPoint);
                    break;
                case Beat.Booting:
                    Objective("Installing power cell...");
                    Beacon(null);
                    Voice(interrupt: false, "ARIA_M02_002", "ARIA_M02_006", "R7_M02_001", "ENG_M02_001", "R7_M02_002");
                    if (loosePart && robotCore) { _partFrom = loosePart.position; _partTo = robotCore.transform.position; }
                    break;
                case Beat.Breach:
                    if (lighting) lighting.Set(FactoryLightingPresets.Preset.Emergency);
                    _escapeDeadline = Time.time + escapeSeconds;
                    Voice("ARIA_M03_001", "HALE_M03_001", "ENG_M03_001", "ARIA_M03_002", "R7_M03_001", "ARIA_M05_001");
                    Objective("Evacuate: reach the maintenance corridor (west)");
                    Beacon(new Vector3(-13.2f, 0f, 0f));
                    break;
                case Beat.Corridor:
                    if (lighting) lighting.Set(FactoryLightingPresets.Preset.ZeroGravity);
                    Voice("ARIA_M04_001", "ENG_M04_001", "HALE_M04_001", "ARIA_M04_003");
                    Objective("Reach Escape Pod Bay D");
                    Beacon(null);
                    break;
                case Beat.PodBay:
                    if (lighting) lighting.Set(FactoryLightingPresets.Preset.Emergency);
                    Voice("R7_M05_001", "HALE_M05_001", "ARIA_M05_003", "R7_M05_002");
                    Objective("Board the escape pod");
                    Beacon(podPoint);
                    break;
                case Beat.Launch:
                    _escapeDeadline = -1f;   // made it: stop the clock
                    Voice("HALE_M06_001", "ENG_M06_001", "ARIA_M06_001", "R7_M06_001", "ARIA_M06_002", "HALE_M06_002");
                    Objective("Launching...");
                    Beacon(null);
                    break;
                case Beat.End:
                    Say();
                    Objective("");
                    break;
                case Beat.Failed:
                    _escapeDeadline = -1f;
                    DialogueManager.StopAll();
                    Say();
                    Objective("");
                    Beacon(null);
                    if (lighting) lighting.Set(FactoryLightingPresets.Preset.Emergency);
                    break;
            }
        }

        // Reload the demo without a frozen frame: async load keeps the headset rendering while the scene streams in.
        void Restart()
        {
            if (_restarting) return;
            _restarting = true;
            DialogueManager.StopAll();
            Time.timeScale = 1f;
            var scene = SceneManager.GetActiveScene();
            SceneManager.LoadSceneAsync(scene.buildIndex >= 0 ? scene.name : "FactoryInterior");
        }

        void Update()
        {
            var cam = Camera.main;
            if (!cam) return;
            Vector3 p = cam.transform.position;
            float t = Time.time - _beatStart;

            switch (_beat)
            {
                case Beat.Intro:
                    if (t > 2f) { _beat = Beat.Install; }
                    break;
                case Beat.Install:
                    if (FactoryHandRig.HandsActive && _grab)
                    {
                        // hands: no dwell; guide the pinch-carry-release instead
                        Objective(_grab.Held ? (_grab.NearSocket ? "Release to install" : "Bring the cell to R-7's glowing chest socket")
                                             : "Point at the glowing power cell and pinch to grab it");
                    }
                    else if (Dwell(p, installPoint, "Hold position to install the power cell")) Enter(Beat.Booting);
                    break;
                case Beat.Booting:
                {
                    float k = Mathf.Clamp01(t / 1.6f);
                    if (loosePart && robotCore)
                    {
                        float arc = Mathf.Sin(k * Mathf.PI) * 0.35f;
                        loosePart.position = Vector3.Lerp(_partFrom, _partTo, Mathf.SmoothStep(0f, 1f, k)) + Vector3.up * arc;
                        loosePart.Rotate(Vector3.up, 360f * Time.deltaTime, Space.World);
                        if (k >= 1f) { loosePart.gameObject.SetActive(false); robotCore.SetActive(true); }
                    }
                    if (robotHead && t > 1.8f)   // R-7 "boots": looks up toward the engineer
                    {
                        Vector3 to = p - robotHead.position;
                        var look = Quaternion.LookRotation(Vector3.ProjectOnPlane(to, Vector3.up).normalized, Vector3.up);
                        float yaw = Mathf.DeltaAngle(robot.eulerAngles.y, look.eulerAngles.y);
                        robotHead.localRotation = _headRest * Quaternion.Euler(0f, Mathf.Clamp(yaw, -40f, 40f) * Mathf.Clamp01((t - 1.8f) / 1.2f), 0f);
                    }
                    if ((t > 3.5f && DialogueManager.Idle) || t > 16f) Enter(Beat.Breach);   // let R-7 finish its first words
                    break;
                }
                case Beat.Breach:
                    if (p.x < corridorEntryX && Mathf.Abs(p.z) < 2.5f) Enter(Beat.Corridor);
                    break;
                case Beat.Corridor:
                    if (p.x < podBayEntryX) Enter(Beat.PodBay);
                    break;
                case Beat.PodBay:
                    if (Dwell(p, podPoint, "Hold position inside the pod to launch")) Enter(Beat.Launch);
                    break;
                case Beat.Launch:
                    if (t > 4f) { Enter(Beat.End); }
                    break;
                case Beat.End:
                    if ((t > restartDelayEnd && DialogueManager.Idle) || t > restartDelayEnd + 15f) Restart();
                    break;
                case Beat.Failed:
                    if (t > restartDelayFailed) Restart();
                    break;
            }
            if (_escapeDeadline > 0f && Time.time > _escapeDeadline && _beat >= Beat.Breach && _beat <= Beat.PodBay) Enter(Beat.Failed);
            if (FactoryVerse.FVInput.GetKeyDown(KeyCode.R)) Restart();

            AnimateBeacon();
            AdvanceSubtitles();
        }

        bool Dwell(Vector3 cam, Vector3 point, string prompt)
        {
            Vector3 d = cam - point; d.y = 0f;
            if (d.magnitude < zoneRadius) _dwell += Time.deltaTime; else _dwell = Mathf.Max(0f, _dwell - Time.deltaTime * 2f);
            if (_dwell > 0f) Objective($"{prompt}   {Mathf.RoundToInt(100f * Mathf.Clamp01(_dwell / dwellSeconds))}%");
            return _dwell >= dwellSeconds;
        }

        // ------------------------------------------------------------ HUD (world-space, follows the camera)
        void BuildHud()
        {
            _hud = new GameObject("Demo HUD").transform;
            _objective = MakeText("Objective", new Vector3(0f, 0.30f, 0f), 0.030f, new Color(1f, 0.55f, 0.15f));
            _subtitle = MakeText("Subtitle", new Vector3(0f, -0.28f, 0f), 0.026f, new Color(0.55f, 0.9f, 1f));
            _card = MakeText("End Card", Vector3.zero, 0.07f, Color.white);
            _card.text = "";
        }

        TextMesh MakeText(string name, Vector3 local, float height, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_hud, false);
            go.transform.localPosition = local;
            var tm = go.AddComponent<TextMesh>();
            tm.font = font ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            go.GetComponent<MeshRenderer>().sharedMaterial = tm.font.material;
            tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center;
            tm.fontSize = 64; tm.characterSize = height / 6.4f; tm.color = color;
            return tm;
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            if (!cam || !_hud) return;
            _hud.position = cam.transform.position + cam.transform.forward * 1.0f;
            _hud.rotation = cam.transform.rotation;
            float since = Time.time - _beatStart;
            if (_beat == Beat.End)
            {
                float k = Mathf.Clamp01(since / 1.5f);
                _card.text = "FACTORYVERSE: LAST SHIFT\n\nEscape pod launched. Shift complete.\n\n<size=40>The shift restarts in a moment</size>";
                _card.color = new Color(1f, 1f, 1f, k);
            }
            else if (_beat == Beat.Failed)
            {
                float k = Mathf.Clamp01(since / 1.0f);
                int left = Mathf.Max(0, Mathf.CeilToInt(restartDelayFailed - since));
                _card.text = $"STATION LOST\n\nThe evacuation window closed.\n\n<size=40>Try again in {left}</size>";
                _card.color = new Color(1f, 0.45f, 0.35f, k);
            }

            // objective line + evacuation clock (turns red in the last 30 s)
            if (_objective)
            {
                if (_escapeDeadline > 0f && !string.IsNullOrEmpty(_objectiveBase))
                {
                    float left = Mathf.Max(0f, _escapeDeadline - Time.time);
                    _objective.text = $"EVACUATE  T-{(int)left / 60}:{(int)left % 60:00}\n{_objectiveBase}";
                    _objective.color = left < 30f && Mathf.Repeat(Time.time, 1f) < 0.5f ? new Color(1f, 0.25f, 0.2f) : new Color(1f, 0.55f, 0.15f);
                }
                else _objective.text = _objectiveBase;
            }
        }

        void Objective(string text) { _objectiveBase = string.IsNullOrEmpty(text) ? "" : "OBJECTIVE  " + text; if (_objective) _objective.text = _objectiveBase; }

        void Say(params string[] lines) { _lines = lines; _lineIndex = 0; _lineTime = Time.time; ShowLine(); }

        // Voiced story lines from the dialogue script (DialogueManager plays the clip and shows the subtitle).
        // A new beat interrupts whatever was still being said, unless interrupt is false.
        void Voice(params string[] ids) => Voice(true, ids);
        void Voice(bool interrupt, params string[] ids)
        {
            Say();   // the director's own text subtitles stay empty; DialogueManager shows them
            if (interrupt) DialogueManager.StopAll();
            foreach (var id in ids)
            {
                var line = DialogueScript.Get(id);
                if (line == null) { Debug.LogWarning($"FactoryDemoDirector: unknown dialogue line '{id}'."); continue; }
                if (!DialogueManager.Instance) DialogueManager.Say(id);   // creates the manager on first use
                else DialogueManager.Instance.Enqueue(line, allowOptionalToWait: true);
            }
        }
        void ShowLine() { if (_subtitle) _subtitle.text = (_lines != null && _lineIndex < _lines.Length) ? Wrap(_lines[_lineIndex], 58) : ""; }
        void AdvanceSubtitles()
        {
            if (_lines == null || _lineIndex >= _lines.Length) return;
            float dur = 2.2f + _lines[_lineIndex].Length * 0.045f;   // reading time
            if (Time.time - _lineTime > dur) { _lineIndex++; _lineTime = Time.time; ShowLine(); }
        }
        static string Wrap(string s, int width)
        {
            var sb = new System.Text.StringBuilder(); int col = 0;
            foreach (var word in s.Split(' '))
            {
                if (col + word.Length > width && col > 0) { sb.Append('\n'); col = 0; }
                else if (col > 0) { sb.Append(' '); col++; }
                sb.Append(word); col += word.Length;
            }
            return sb.ToString();
        }

        // ------------------------------------------------------------ beacon (glowing ring + light beam)
        Transform BuildBeacon()
        {
            var root = new GameObject("Objective Beacon").transform;
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(ring.GetComponent<Collider>());
            ring.transform.SetParent(root, false); ring.transform.localScale = new Vector3(2f * zoneRadius * 0.7f, 0.01f, 2f * zoneRadius * 0.7f);
            ring.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            if (beamMaterial) ring.GetComponent<Renderer>().sharedMaterial = beamMaterial;
            var beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(beam.GetComponent<Collider>());
            beam.transform.SetParent(root, false); beam.transform.localScale = new Vector3(0.08f, 1.5f, 0.08f);
            beam.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            if (beaconMaterial) beam.GetComponent<Renderer>().sharedMaterial = beaconMaterial;
            root.gameObject.SetActive(false);
            return root;
        }
        void Beacon(Vector3? at)
        {
            if (!_beacon) return;
            _beacon.gameObject.SetActive(at.HasValue);
            if (at.HasValue) _beacon.position = at.Value;
        }
        void AnimateBeacon()
        {
            if (!_beacon || !_beacon.gameObject.activeSelf) return;
            float s = 1f + 0.08f * Mathf.Sin(Time.time * 4f);
            _beacon.localScale = new Vector3(s, 1f, s);
        }
    }
}
