using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace FactoryVerse
{
    /// <summary>
    /// Hands-first input for the FV-01 factory (no controllers needed), using OpenXR hand tracking via XR Hands.
    ///  - Both hands are drawn from tracked joints (glowing fingertips).
    ///  - Each hand casts an aim ray from the shoulder through the palm. Point at a teleport pad and pinch
    ///    (thumb + index) to jump there: seated-friendly locomotion through every zone.
    /// The controller gloves of FactoryVRRig are hidden while hands are tracked, and come back if you pick up controllers.
    /// </summary>
    public class FactoryHandRig : MonoBehaviour
    {
        public FactoryVRRig rig;
        public Transform cameraOffset;
        public Camera head;
        public GameObject[] controllerHands;
        public Material jointMaterial, tipMaterial;
        public FactoryTeleportPad[] pads;

        [Header("Gestures")]
        public float pinchStart = 0.02f, pinchEnd = 0.035f;   // thumb–index distance (m), with hysteresis
        public float maxRayLength = 16f, padHoverRadius = 0.75f;
        public float distanceGrabRange = 4f;   // point-and-pinch pulls a part from this far (m)
        public float socketAimRange = 6f, socketNearRange = 1.8f;   // release installs when aimed at / this close to the socket

        class Hand
        {
            public Transform root; public Transform[] joints; public LineRenderer ray; public Transform reticle;
            public bool tracked, pinching; public Vector3 origin, dir; public FactoryTeleportPad hover;
            public FactoryGrabbable held, near;
        }

        FactoryGrabbable[] _grabbables = new FactoryGrabbable[0];
        float _nextGrabbableScan;

        /// <summary>True while either hand is tracked: lets other systems switch prompts from "walk" to "pinch".</summary>
        public static bool HandsActive { get; private set; }

        Hand _left, _right;
        XRHandSubsystem _subsystem;
        static readonly List<XRHandSubsystem> _found = new List<XRHandSubsystem>();
        int _jointCount;
        const int RayMask = ~(1 << 2);   // ignore the "Ignore Raycast" layer (the player's own body capsule)

        void Start()
        {
            _jointCount = XRHandJointID.EndMarker.ToIndex();
            _left = Build("Tracked Hand L");
            _right = Build("Tracked Hand R");
            ShowPads(false);
            BuildHint();
            BuildFade();
        }

        // ---- onboarding: a small label floats above the right hand until the player has moved once
        TextMesh _hint; bool _hasTeleported, _hasGrabbed;
        void BuildHint()
        {
            var go = new GameObject("Hand Hint");
            go.transform.SetParent(transform, false);
            _hint = go.AddComponent<TextMesh>();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _hint.font = font; go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            _hint.fontSize = 64; _hint.characterSize = 0.0028f; _hint.anchor = TextAnchor.LowerCenter; _hint.alignment = TextAlignment.Center;
            _hint.color = new Color(0.6f, 0.95f, 1f);
            go.SetActive(false);
        }
        void UpdateHint()
        {
            var h = _right.tracked ? _right : _left.tracked ? _left : null;
            bool show = h != null && (!_hasTeleported || (h.near && !_hasGrabbed));
            _hint.gameObject.SetActive(show);
            if (!show) return;
            _hint.text = h.near ? "Pinch to grab" : h.hover ? "Pinch to move here" : "Point at a glowing ring\nthen pinch to move";
            var palm = h.joints[XRHandJointID.Palm.ToIndex()].position;
            _hint.transform.position = palm + Vector3.up * 0.09f;
            _hint.transform.rotation = Quaternion.LookRotation(palm - head.transform.position);
        }

        // ---- comfort: a brief black fade hides the jump when teleporting
        Material _fadeMat; Transform _fadeQuad; float _fade, _fadeTarget; System.Action _atBlack;
        void BuildFade()
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(q.GetComponent<Collider>());
            q.name = "Teleport Fade";
            q.transform.SetParent(head.transform, false);
            q.transform.localPosition = new Vector3(0f, 0f, head.nearClipPlane + 0.02f);
            q.transform.localScale = new Vector3(1f, 1f, 1f);
            _fadeMat = new Material(Shader.Find("Sprites/Default")) { color = new Color(0f, 0f, 0f, 0f), renderQueue = 4000 };
            q.GetComponent<Renderer>().sharedMaterial = _fadeMat;
            _fadeQuad = q.transform;
            q.SetActive(false);
        }
        void FadeTeleport(Vector3 point, float yaw)
        {
            if (_fadeTarget > 0f) return;   // already mid-fade
            _fadeTarget = 1f; _fadeQuad.gameObject.SetActive(true);
            _atBlack = () => { rig.TeleportTo(point, yaw); _hasTeleported = true; };
        }
        void UpdateFade()
        {
            if (!_fadeQuad || !_fadeQuad.gameObject.activeSelf) return;
            _fade = Mathf.MoveTowards(_fade, _fadeTarget, Time.unscaledDeltaTime / 0.12f);
            _fadeMat.color = new Color(0f, 0f, 0f, _fade);
            if (_fadeTarget > 0f && _fade >= 1f) { _atBlack?.Invoke(); _atBlack = null; _fadeTarget = 0f; }
            else if (_fadeTarget == 0f && _fade <= 0f) _fadeQuad.gameObject.SetActive(false);
        }

        Hand Build(string name)
        {
            var h = new Hand { root = new GameObject(name).transform, joints = new Transform[_jointCount] };
            h.root.SetParent(cameraOffset ? cameraOffset : transform, false);
            for (int i = 0; i < _jointCount; i++)
            {
                var id = XRHandJointIDUtility.FromIndex(i);
                bool tip = id == XRHandJointID.ThumbTip || id == XRHandJointID.IndexTip || id == XRHandJointID.MiddleTip
                           || id == XRHandJointID.RingTip || id == XRHandJointID.LittleTip;
                var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(s.GetComponent<Collider>());
                s.name = id.ToString();
                s.transform.SetParent(h.root, false);
                s.transform.localScale = Vector3.one * (tip ? 0.014f : id == XRHandJointID.Palm ? 0.03f : 0.016f);
                s.GetComponent<Renderer>().sharedMaterial = tip ? tipMaterial : jointMaterial;
                h.joints[i] = s.transform;
            }
            var lr = new GameObject("Aim Ray").AddComponent<LineRenderer>();
            lr.transform.SetParent(h.root, false);
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.widthCurve = AnimationCurve.Linear(0f, 0.006f, 1f, 0.002f);
            lr.positionCount = 2; lr.useWorldSpace = true;
            h.ray = lr;
            var ret = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(ret.GetComponent<Collider>());
            ret.name = "Reticle"; ret.transform.SetParent(h.root, false); ret.transform.localScale = Vector3.one * 0.06f;
            ret.GetComponent<Renderer>().sharedMaterial = tipMaterial;
            h.reticle = ret.transform;
            h.root.gameObject.SetActive(false);
            return h;
        }

        void Update()
        {
            if (_subsystem == null || !_subsystem.running)
            {
                _subsystem = null;
                SubsystemManager.GetSubsystems(_found);
                foreach (var s in _found) if (s.running) { _subsystem = s; break; }
            }

            if (Time.time >= _nextGrabbableScan)   // grabbables can appear per beat; refresh cheaply
            {
                _grabbables = FindObjectsByType<FactoryGrabbable>();
                _nextGrabbableScan = Time.time + 1f;
            }
            foreach (var g in _grabbables) if (g) g.Hovered = false;

            UpdateHand(_left, _subsystem != null ? _subsystem.leftHand : default, -1f);
            UpdateHand(_right, _subsystem != null ? _subsystem.rightHand : default, 1f);

            bool any = _left.tracked || _right.tracked;
            HandsActive = any;
            UpdateHint();
            UpdateFade();
            if (controllerHands != null) foreach (var g in controllerHands) if (g) g.SetActive(!any);
            ShowPads(any);
        }

        void UpdateHand(Hand h, XRHand hand, float side)
        {
            h.tracked = _subsystem != null && hand.isTracked;
            h.root.gameObject.SetActive(h.tracked);
            if (h.hover) { h.hover.SetHover(false); h.hover = null; }
            if (!h.tracked)
            {
                h.pinching = false;
                if (h.held) { h.held.SocketTargeted = false; h.held.Release(); h.held = null; }   // tracking lost: drop (it glides home)
                return;
            }

            for (int i = 0; i < _jointCount; i++)
                if (hand.GetJoint(XRHandJointIDUtility.FromIndex(i)).TryGetPose(out Pose p))
                { h.joints[i].localPosition = p.position; h.joints[i].localRotation = p.rotation; }

            Vector3 thumb = h.joints[XRHandJointID.ThumbTip.ToIndex()].position;
            Vector3 index = h.joints[XRHandJointID.IndexTip.ToIndex()].position;
            Vector3 palm = h.joints[XRHandJointID.Palm.ToIndex()].position;
            float pinchDist = Vector3.Distance(thumb, index);
            bool wasPinching = h.pinching;
            h.pinching = h.pinching ? pinchDist < pinchEnd : pinchDist < pinchStart;
            bool pinchDown = h.pinching && !wasPinching;

            // ---- direct grab: pinch close to a grabbable part, carry it, release on its socket
            Vector3 pinchPoint = (thumb + index) * 0.5f;
            if (h.held)
            {
                if (h.pinching)
                {
                    var wrist = h.joints[XRHandJointID.Wrist.ToIndex()];
                    h.held.Carry(pinchPoint, wrist.rotation);
                    h.reticle.gameObject.SetActive(false);
                    // point-to-install: roughly aiming at the socket, or standing next to it, makes the release install
                    bool targeted = false;
                    var sock = h.held.socket;
                    if (sock)
                    {
                        var hy = Quaternion.Euler(0f, head.transform.eulerAngles.y, 0f);
                        Vector3 sh = head.transform.position + hy * new Vector3(0.17f * side, -0.2f, 0f);
                        Vector3 dir = (palm - sh).normalized;
                        Vector3 to = sock.position - palm;
                        float along = Vector3.Dot(to, dir);
                        bool aimed = along > 0.1f && along < socketAimRange && Vector3.Distance(palm + dir * along, sock.position) < 0.6f + along * 0.12f;
                        Vector3 flat = sock.position - head.transform.position; flat.y = 0f;
                        targeted = aimed || flat.magnitude < socketNearRange;
                    }
                    h.held.SocketTargeted = targeted;
                    h.ray.enabled = targeted;
                    if (targeted)
                    {
                        var gc = new Color(1f, 0.85f, 0.3f, 0.9f);
                        h.ray.SetPosition(0, pinchPoint); h.ray.SetPosition(1, sock.position);
                        h.ray.startColor = gc; h.ray.endColor = new Color(gc.r, gc.g, gc.b, 0.3f);
                    }
                    return;
                }
                h.held.Release(); h.held = null;
            }
            h.near = null;
            foreach (var g in _grabbables)
                if (g && !g.Held && g.InReach(pinchPoint)) { h.near = g; g.Hovered = true; break; }
            if (h.near)
            {
                h.ray.enabled = false; h.reticle.gameObject.SetActive(false);
                if (pinchDown) { h.held = h.near; h.held.Grab(); _hasGrabbed = true; }
                return;   // a part in reach takes priority over pointing/teleporting
            }
            h.ray.enabled = true;

            // aim ray: from an estimated shoulder through the palm (stable while pinching)
            var yaw = Quaternion.Euler(0f, head.transform.eulerAngles.y, 0f);
            Vector3 shoulder = head.transform.position + yaw * new Vector3(0.17f * side, -0.2f, 0f);
            h.origin = palm;
            h.dir = (palm - shoulder).normalized;

            float length = maxRayLength;
            if (Physics.Raycast(h.origin, h.dir, out RaycastHit hit, maxRayLength, RayMask, QueryTriggerInteraction.Ignore)) length = hit.distance;

            // ---- distance grab: point at a part and pinch to pull it into the hand (seated play, Meta XR Simulator)
            FactoryGrabbable farBest = null; float farAlong = float.MaxValue; Vector3 farPoint = Vector3.zero;
            foreach (var g in _grabbables)
            {
                if (!g || g.Held || g.Done || !g.isActiveAndEnabled) continue;
                var r = g.GetComponentInChildren<Renderer>();
                Vector3 c = r ? r.bounds.center : g.transform.position;
                float along = Vector3.Dot(c - h.origin, h.dir);
                if (along <= 0.2f || along > distanceGrabRange) continue;
                if (Vector3.Distance(h.origin + h.dir * along, c) > 0.2f + along * 0.06f) continue;   // cone around the ray
                var to = c - h.origin;
                if (Physics.Raycast(h.origin, to.normalized, to.magnitude - 0.3f, RayMask, QueryTriggerInteraction.Ignore)) continue;   // blocked
                if (along < farAlong) { farAlong = along; farBest = g; farPoint = c; }
            }
            if (farBest)
            {
                farBest.Hovered = true;
                var gc = new Color(1f, 0.85f, 0.3f, 0.9f);
                h.ray.SetPosition(0, h.origin); h.ray.SetPosition(1, farPoint);
                h.ray.startColor = gc; h.ray.endColor = new Color(gc.r, gc.g, gc.b, 0.2f);
                h.reticle.gameObject.SetActive(false);
                if (pinchDown) { h.held = farBest; farBest.Grab(); _hasGrabbed = true; }   // Carry() pulls it to the pinch
                return;   // a part under the ray takes priority over teleport pads
            }

            // nearest visible pad near the ray
            FactoryTeleportPad best = null; float bestAlong = float.MaxValue;
            if (pads != null)
                foreach (var pad in pads)
                {
                    if (!pad) continue;
                    Vector3 c = pad.transform.position + Vector3.up * 0.05f;
                    float along = Vector3.Dot(c - h.origin, h.dir);
                    if (along <= 0.3f || along > maxRayLength) continue;
                    if (Vector3.Distance(h.origin + h.dir * along, c) > padHoverRadius + along * 0.03f) continue;
                    var to = c - h.origin;
                    if (Physics.Raycast(h.origin, to.normalized, to.magnitude - 0.35f, RayMask, QueryTriggerInteraction.Ignore)) continue;   // behind a wall
                    if (along < bestAlong) { bestAlong = along; best = pad; }
                }

            h.hover = best;
            if (best) best.SetHover(true);
            Vector3 end = best ? best.transform.position + Vector3.up * 0.05f : h.origin + h.dir * length;
            h.ray.SetPosition(0, h.origin); h.ray.SetPosition(1, end);
            var col = best ? new Color(0.3f, 1f, 0.5f, 0.9f) : new Color(0.35f, 0.9f, 1f, h.pinching ? 0.9f : 0.45f);
            h.ray.startColor = col; h.ray.endColor = new Color(col.r, col.g, col.b, 0.1f);
            h.reticle.gameObject.SetActive(best);
            if (best) h.reticle.position = end;

            if (pinchDown && best && rig) FadeTeleport(best.transform.position, best.Yaw);
        }

        void ShowPads(bool v) { if (pads != null) foreach (var p in pads) if (p) p.SetVisible(v); }
    }
}
