using System;
using UnityEngine;

namespace FactoryVerse
{
    /// <summary>
    /// A part the player can pinch and carry with tracked hands (FactoryHandRig).
    /// Released within <see cref="snapDistance"/> of its socket it fires <see cref="Installed"/>; released anywhere else
    /// it glides back to where it came from, so a part can never be lost on a seated, hands-only playthrough.
    /// </summary>
    public class FactoryGrabbable : MonoBehaviour
    {
        public Transform socket;
        public float snapDistance = 0.35f;
        public float grabRadius = 0.12f;          // how close the pinch must be to the part
        public string hint = "Pinch to grab";

        public event Action Installed;
        public bool Held { get; private set; }
        public bool Hovered { get; set; }
        public bool Done { get; private set; }

        Vector3 _restPos, _scale; Quaternion _restRot;
        float _returnT = 1f; Vector3 _returnFrom; Quaternion _returnFromRot;

        void Awake() { _restPos = transform.position; _restRot = transform.rotation; _scale = transform.localScale; }

        public bool InReach(Vector3 pinchPoint)
        {
            if (Done || !isActiveAndEnabled) return false;
            var r = GetComponentInChildren<Renderer>();
            Vector3 c = r ? r.bounds.center : transform.position;
            float size = r ? r.bounds.extents.magnitude : 0.1f;
            return Vector3.Distance(pinchPoint, c) < grabRadius + size * 0.6f;
        }

        public void Grab() { if (!Done) { Held = true; _returnT = 1f; } }

        public void Carry(Vector3 pinchPoint, Quaternion handRot)
        {
            transform.position = Vector3.Lerp(transform.position, pinchPoint, 0.6f);
            transform.rotation = Quaternion.Slerp(transform.rotation, handRot * Quaternion.Euler(90f, 0f, 0f), 0.3f);
        }

        public void Release()
        {
            if (!Held) return;
            Held = false;
            bool install = NearSocket;
            SocketTargeted = false;
            if (install)
            {
                Done = true;
                Installed?.Invoke();
                return;
            }
            _returnFrom = transform.position; _returnFromRot = transform.rotation; _returnT = 0f;   // glide home
        }

        /// <summary>Set by the hand rig while the holder points at (or stands next to) the socket: releasing then installs.</summary>
        public bool SocketTargeted { get; set; }

        public bool NearSocket => socket && (SocketTargeted || Vector3.Distance(transform.position, socket.position) < snapDistance);

        void Update()
        {
            if (!Held && !Done && _returnT < 1f)
            {
                _returnT = Mathf.Min(1f, _returnT + Time.deltaTime * 1.5f);
                float k = Mathf.SmoothStep(0f, 1f, _returnT);
                transform.position = Vector3.Lerp(_returnFrom, _restPos, k);
                transform.rotation = Quaternion.Slerp(_returnFromRot, _restRot, k);
            }
            // gentle pulse while it can be grabbed: a visual cue instead of a wall of text
            float pulse = Done ? 1f : Held ? 1.06f : Hovered ? 1.1f : 1f + 0.03f * Mathf.Sin(Time.time * 4f);
            transform.localScale = _scale * pulse;
        }
    }
}
