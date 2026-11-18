using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace FactoryVerse
{
    /// <summary>
    /// Minimal OpenXR rig for the FV-01 factory interior (Meta Quest / Quest Link). No XR Interaction Toolkit needed:
    /// head and controller poses come from UnityEngine.XR.InputDevices, which the OpenXR plug-in feeds.
    ///
    /// Controls (Touch controllers):
    ///   Left stick   move (relative to where you look)      Right stick X  snap turn (30°)
    ///   A            teleport to the next zone              B              cycle lighting (Normal / Emergency / Zero-G)
    ///   X            headlamp
    ///   Zero-G lighting = zero-G movement: left stick flies where you look, right stick Y goes up/down, left grip brakes.
    /// If no headset is active when the scene starts, this rig switches itself off and the desktop rig takes over.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FactoryVRRig : MonoBehaviour
    {
        public Transform cameraOffset;
        public Camera headCamera;
        public Transform leftHand, rightHand;
        public Light headlamp;
        public FactoryLightingPresets lighting;
        public Transform[] teleportPoints;
        public GameObject desktopRig;

        public float moveSpeed = 2.5f, gravity = 9.8f, snapAngle = 30f;
        public float zeroGSpeed = 2.2f, zeroGAccel = 2.5f;
        public float seatedEyeHeight = 1.65f;   // used only if the runtime can't give floor-level tracking

        CharacterController _cc;
        Vector3 _velocity;
        bool _snapReady = true, _prevA, _prevB, _prevX;
        int _teleport = -1;
        static readonly List<XRInputSubsystem> _subsystems = new List<XRInputSubsystem>();

        bool ZeroG => lighting && lighting.preset == FactoryLightingPresets.Preset.ZeroGravity;

        void Awake()
        {
            if (!XRSettings.isDeviceActive)
            {
                // No headset yet: hand over to the keyboard/mouse rig, but keep watching. OpenXR can start its
                // session a moment after the scene loads (Quest Link, Meta XR Simulator), so switch back if it does.
                if (desktopRig) desktopRig.SetActive(true);
                new GameObject("XR Late Start Watcher").AddComponent<LateStartWatcher>().rig = this;
                gameObject.SetActive(false);
                return;
            }
            Init();
        }

        /// <summary>Polls (one bool per frame) for a late XR session; switches to VR the moment one starts.</summary>
        class LateStartWatcher : MonoBehaviour
        {
            public FactoryVRRig rig;
            void Update()
            {
                if (!rig) { Destroy(gameObject); return; }
                if (!XRSettings.isDeviceActive) return;
                rig.gameObject.SetActive(true);
                rig.Init();
                Destroy(gameObject);
            }
        }

        Vector3 _spawnPos; float _spawnYaw;

        void Init()
        {
            _spawnPos = headCamera ? new Vector3(headCamera.transform.position.x, transform.position.y, headCamera.transform.position.z) : transform.position;
            _spawnYaw = transform.eulerAngles.y;
            if (desktopRig) desktopRig.SetActive(false);
            _cc = GetComponent<CharacterController>();
            if (headlamp) headlamp.enabled = false;

            bool floor = false;
            SubsystemManager.GetSubsystems(_subsystems);
            foreach (var s in _subsystems) floor |= s.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor);
            if (cameraOffset) cameraOffset.localPosition = new Vector3(0f, floor ? 0f : seatedEyeHeight, 0f);
        }

        void OnEnable() { Application.onBeforeRender += ApplyPoses; }
        void OnDisable() { Application.onBeforeRender -= ApplyPoses; }

        void Update()
        {
            ApplyPoses();
            var left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            var right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            left.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 ls);
            right.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 rs);

            // Buttons (edge-triggered)
            if (Pressed(right, CommonUsages.primaryButton, ref _prevA)) NextZone();
            if (Pressed(right, CommonUsages.secondaryButton, ref _prevB) && lighting)
                lighting.Set((FactoryLightingPresets.Preset)(((int)lighting.preset + 1) % 3));
            if (Pressed(left, CommonUsages.primaryButton, ref _prevX) && headlamp) headlamp.enabled = !headlamp.enabled;

            // Keep the body capsule under the head
            var head = headCamera.transform.localPosition + (cameraOffset ? cameraOffset.localPosition : Vector3.zero);
            float h = Mathf.Clamp(head.y, 1.0f, 2.1f);
            _cc.height = h; _cc.center = new Vector3(head.x, h / 2f + _cc.skinWidth, head.z);

            // Snap turn around the head (not in zero-G, where right stick Y is up/down)
            if (Mathf.Abs(rs.x) > 0.75f && _snapReady)
            {
                transform.RotateAround(headCamera.transform.position, Vector3.up, Mathf.Sign(rs.x) * snapAngle);
                _snapReady = false;
            }
            else if (Mathf.Abs(rs.x) < 0.3f) _snapReady = true;

            var yaw = Quaternion.Euler(0f, headCamera.transform.eulerAngles.y, 0f);
            if (ZeroG)
            {
                var wish = headCamera.transform.rotation * new Vector3(ls.x, 0f, ls.y) + Vector3.up * (Mathf.Abs(rs.y) > 0.3f ? rs.y : 0f);
                _velocity = Vector3.MoveTowards(_velocity, Vector3.ClampMagnitude(wish, 1f) * zeroGSpeed, zeroGAccel * Time.deltaTime);
                if (left.TryGetFeatureValue(CommonUsages.gripButton, out bool brake) && brake)
                    _velocity = Vector3.MoveTowards(_velocity, Vector3.zero, 8f * Time.deltaTime);
            }
            else
            {
                var move = yaw * new Vector3(ls.x, 0f, ls.y) * moveSpeed;
                _velocity.x = move.x; _velocity.z = move.z;
                _velocity.y = _cc.isGrounded ? -1f : _velocity.y - gravity * Time.deltaTime;
            }
            _cc.Move(_velocity * Time.deltaTime);

            // Safety net: walked through a wall or off the hull and fell out of the station? Put the player back at the start.
            if (transform.position.y < _spawnPos.y - 20f) TeleportTo(_spawnPos, _spawnYaw);
        }

        void ApplyPoses()
        {
            Pose(XRNode.Head, headCamera ? headCamera.transform : null);
            Pose(XRNode.LeftHand, leftHand);
            Pose(XRNode.RightHand, rightHand);
        }

        static void Pose(XRNode node, Transform t)
        {
            if (!t) return;
            var d = InputDevices.GetDeviceAtXRNode(node);
            if (d.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 p)) t.localPosition = p;
            if (d.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion r)) t.localRotation = r;
        }

        static bool Pressed(InputDevice d, InputFeatureUsage<bool> usage, ref bool previous)
        {
            d.TryGetFeatureValue(usage, out bool now);
            bool down = now && !previous;
            previous = now;
            return down;
        }

        void NextZone()
        {
            if (teleportPoints == null || teleportPoints.Length == 0) return;
            _teleport = (_teleport + 1) % teleportPoints.Length;
            var t = teleportPoints[_teleport];
            TeleportTo(t.position, t.eulerAngles.y);
        }

        /// <summary>Puts the player's head (not the rig origin) above <paramref name="point"/>, looking along <paramref name="yaw"/>.</summary>
        public void TeleportTo(Vector3 point, float yaw)
        {
            if (!_cc) _cc = GetComponent<CharacterController>();
            float headYaw = headCamera.transform.localEulerAngles.y;
            _cc.enabled = false;
            transform.rotation = Quaternion.Euler(0f, yaw - headYaw, 0f);
            var headOffset = headCamera.transform.position - transform.position; headOffset.y = 0f;
            transform.position = point - headOffset;
            _cc.enabled = true;
            _velocity = Vector3.zero;
        }
    }
}
