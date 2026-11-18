using UnityEngine;

namespace FactoryVerse
{
    /// <summary>
    /// Procedural idle for the FactoryVerse characters (Unit R-7 style rigs with Unity-humanoid bone names:
    /// Hips, Spine, Chest, Neck, Head, Left/RightUpperArm, Left/RightLowerArm).
    /// Layers breathing, a slow weight shift, arm sway and a look-around on top of the pose the character was
    /// placed in, and can turn the head toward the player when they come close.
    /// Hologram style adds a gentle hover and drift (for ARIA).
    /// Cheap enough for Quest: a dozen bone rotations per character per frame, no Animator needed.
    /// </summary>
    public class FactoryIdleMotion : MonoBehaviour
    {
        public enum Style { Human, Hologram }

        public Style style = Style.Human;
        [Range(0f, 2f)] public float amount = 1f;          // overall strength of the motion
        [Range(0.3f, 2f)] public float speed = 1f;         // overall tempo
        [Tooltip("Extra downward head tilt in degrees, e.g. someone looking at a workbench.")]
        public float headDown = 0f;
        [Header("Look at player")]
        public bool lookAtPlayer = true;
        public float lookRange = 6f;
        public float maxHeadYaw = 50f;

        Transform _hips, _spine, _chest, _neck, _head, _lUpper, _rUpper, _lLower, _rLower;
        Quaternion _hipsQ, _spineQ, _chestQ, _neckQ, _headQ, _lUpperQ, _rUpperQ, _lLowerQ, _rLowerQ;
        Vector3 _hipsPos;
        float _phase, _lookYaw;

        void Awake()   // before any walk clip samples a frame: capture the placed standing pose
        {
            _hips = Find("Hips"); _spine = Find("Spine"); _chest = Find("Chest"); _neck = Find("Neck"); _head = Find("Head");
            _lUpper = Find("LeftUpperArm"); _rUpper = Find("RightUpperArm");
            _lLower = Find("LeftLowerArm"); _rLower = Find("RightLowerArm");
            _hipsQ = Q(_hips); _spineQ = Q(_spine); _chestQ = Q(_chest); _neckQ = Q(_neck); _headQ = Q(_head);
            _lUpperQ = Q(_lUpper); _rUpperQ = Q(_rUpper); _lLowerQ = Q(_lLower); _rLowerQ = Q(_rLower);
            if (_hips) _hipsPos = _hips.localPosition;
            // each character breathes on its own rhythm
            _phase = Mathf.Repeat(transform.position.x * 1.37f + transform.position.z * 0.71f, 10f);
        }

        Transform Find(string bone)
        {
            foreach (var t in GetComponentsInChildren<Transform>(true)) if (t.name == bone) return t;
            return null;
        }
        static Quaternion Q(Transform t) => t ? t.localRotation : Quaternion.identity;

        // reset to the placed pose, then rotate about a world axis (keeps working whatever the bone's local axes are)
        static void Pose(Transform t, Quaternion baseLocal, Vector3 worldAxis, float degrees)
        {
            if (!t) return;
            t.localRotation = baseLocal;
            if (degrees != 0f) t.rotation = Quaternion.AngleAxis(degrees, worldAxis) * t.rotation;
        }
        static void Pose(Transform t, Quaternion baseLocal, Vector3 axisA, float degA, Vector3 axisB, float degB)
        {
            if (!t) return;
            t.localRotation = baseLocal;
            t.rotation = Quaternion.AngleAxis(degA, axisA) * Quaternion.AngleAxis(degB, axisB) * t.rotation;
        }

        void LateUpdate()
        {
            float t = Time.time * speed + _phase * 10f;
            float a = amount;
            Vector3 up = Vector3.up, right = transform.right, fwd = transform.forward;
            bool holo = style == Style.Hologram;

            float breathe = Mathf.Sin(t * Mathf.PI * 2f * 0.23f);                 // ~14 breaths / min
            float sway = Mathf.Sin(t * Mathf.PI * 2f * 0.07f);                    // slow weight shift
            float lookNoiseY = (Mathf.PerlinNoise(t * 0.12f, _phase) - 0.5f) * 2f;
            float lookNoiseP = (Mathf.PerlinNoise(_phase, t * 0.09f) - 0.5f) * 2f;

            // body
            if (_hips)
            {
                _hips.localPosition = _hipsPos;
                if (holo) _hips.position += up * (0.03f * Mathf.Sin(t * Mathf.PI * 2f * 0.3f) * a);   // hover
            }
            Pose(_hips, _hipsQ, fwd, 1.2f * sway * a, up, (holo ? 4f : 1.5f) * sway * a);
            Pose(_spine, _spineQ, right, -0.6f * breathe * a);
            Pose(_chest, _chestQ, right, -1.3f * breathe * a);
            Pose(_neck, _neckQ, right, 0.5f * breathe * a);

            // head: look around, or toward the player when they are close
            float targetYaw = lookNoiseY * 14f * a;
            var cam = lookAtPlayer ? Camera.main : null;
            if (cam && _head)
            {
                Vector3 to = cam.transform.position - _head.position; to.y = 0f;
                if (to.magnitude < lookRange && to.sqrMagnitude > 0.01f)
                    targetYaw = Mathf.Clamp(Vector3.SignedAngle(Vector3.ProjectOnPlane(fwd, up), to, up), -maxHeadYaw, maxHeadYaw);
            }
            _lookYaw = Mathf.Lerp(_lookYaw, targetYaw, 1f - Mathf.Exp(-2.5f * Time.deltaTime));
            Pose(_head, _headQ, up, _lookYaw, right, headDown + lookNoiseP * 4f * a);

            // arms: small counter-swing with the breath / weight shift (holograms drift more)
            float armAmp = holo ? 6f : 2.5f;
            Pose(_lUpper, _lUpperQ, right, armAmp * Mathf.Sin(t * 0.9f) * a, fwd, -1.2f * breathe * a);
            Pose(_rUpper, _rUpperQ, right, -armAmp * Mathf.Sin(t * 0.9f + 0.6f) * a, fwd, 1.2f * breathe * a);
            Pose(_lLower, _lLowerQ, right, -2.5f * (0.5f + 0.5f * Mathf.Sin(t * 0.7f)) * a);
            Pose(_rLower, _rLowerQ, right, -2.5f * (0.5f + 0.5f * Mathf.Sin(t * 0.7f + 1.3f)) * a);
        }
    }
}
