using UnityEngine;

namespace FactoryVerse
{
    /// <summary>
    /// Walks a character along a list of waypoints (ping-pong), pausing at each one, playing the character's
    /// Walk clip (legacy Animation imported from the Blender FBX) while moving and handing over to
    /// FactoryIdleMotion while standing. The clip's playback speed follows the walking speed so feet don't slide.
    /// </summary>
    public class FactoryNPCWalker : MonoBehaviour
    {
        [System.Serializable]
        public struct Stop
        {
            public Vector3 position;
            [Tooltip("Seconds to stand here before moving on.")] public float wait;
            [Tooltip("Direction to face while standing (degrees, Y). Use -1 to keep the walking direction.")] public float faceYaw;
        }

        public Stop[] route;
        public float walkSpeed = 1.15f;          // m/s
        public float turnSpeed = 240f;           // deg/s
        [Tooltip("Ground distance covered by one full Walk cycle at speed 1 (two steps).")]
        public float strideLength = 1.3f;

        Animation _anim;
        FactoryIdleMotion _idle;
        Transform[] _bones; Quaternion[] _restRot; Vector3[] _restPos;
        int _target = 1, _dir = 1;
        float _waitUntil;
        bool _walking;

        void Awake()
        {
            // Awake runs before the Animation component samples its first frame: remember the placed standing pose
            // so it can be restored exactly when the walk stops.
            _anim = GetComponentInChildren<Animation>();
            _idle = GetComponent<FactoryIdleMotion>();
            _bones = GetComponentsInChildren<Transform>();
            _restRot = new Quaternion[_bones.Length]; _restPos = new Vector3[_bones.Length];
            for (int i = 0; i < _bones.Length; i++) { _restRot[i] = _bones[i].localRotation; _restPos[i] = _bones[i].localPosition; }
            if (_anim)
            {
                _anim.playAutomatically = false;
                _anim.cullingType = AnimationCullingType.AlwaysAnimate;   // keep feet in step even when off-screen
            }
        }

        void Start()
        {
            if (_anim && _anim.clip)
            {
                _anim[_anim.clip.name].wrapMode = WrapMode.Loop;
                _anim.Stop();
            }
            if (route != null && route.Length > 0)
            {
                transform.position = route[0].position;
                _waitUntil = Time.time + route[0].wait;
            }
            SetWalking(false);
        }

        void SetWalking(bool walking)
        {
            if (_walking == walking && Time.frameCount > 1) return;
            _walking = walking;
            if (_anim && _anim.clip)
            {
                if (walking)
                {
                    _anim[_anim.clip.name].speed = walkSpeed / Mathf.Max(0.1f, strideLength / _anim.clip.length);
                    _anim.CrossFade(_anim.clip.name, 0.2f);
                }
                else
                {
                    _anim.Stop();
                    for (int i = 1; i < _bones.Length; i++) { _bones[i].localRotation = _restRot[i]; _bones[i].localPosition = _restPos[i]; }
                }
            }
            if (_idle) _idle.enabled = !walking;
        }

        void Update()
        {
            if (route == null || route.Length < 2) return;
            var stop = route[_target];
            Vector3 to = stop.position - transform.position; to.y = 0f;

            if (Time.time < _waitUntil)
            {
                // standing: turn to the stop's facing, idle plays
                var prev = route[Mathf.Clamp(_target - _dir, 0, route.Length - 1)];
                if (prev.faceYaw >= 0f) TurnTowards(prev.faceYaw);
                return;
            }

            if (to.magnitude < 0.05f)
            {
                // arrived: wait, then pick the next stop (ping-pong along the route)
                transform.position = new Vector3(stop.position.x, transform.position.y, stop.position.z);
                SetWalking(false);
                _waitUntil = Time.time + stop.wait;
                if (_target + _dir >= route.Length || _target + _dir < 0) _dir = -_dir;
                _target += _dir;
                return;
            }

            SetWalking(true);
            TurnTowards(Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg);
            // only advance once roughly facing the way we're going
            float facing = Vector3.Angle(transform.forward, to);
            float step = walkSpeed * Time.deltaTime * Mathf.Clamp01(1.2f - facing / 90f);
            transform.position += to.normalized * Mathf.Min(step, to.magnitude);
        }

        void TurnTowards(float yaw)
        {
            var target = Quaternion.Euler(0f, yaw, 0f);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * Time.deltaTime);
        }
    }
}
