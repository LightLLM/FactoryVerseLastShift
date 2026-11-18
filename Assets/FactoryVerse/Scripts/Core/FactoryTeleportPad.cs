using UnityEngine;

namespace FactoryVerse
{
    /// <summary>
    /// A seated-friendly teleport destination: point at it with an open hand and pinch to go there,
    /// facing the pad's forward direction. Hidden unless hand tracking is active (FactoryHandRig shows it).
    /// </summary>
    public class FactoryTeleportPad : MonoBehaviour
    {
        public string label = "";
        public Renderer[] visuals;
        public Material idleMaterial, hoverMaterial;
        bool _visible = true, _hover;

        public float Yaw => transform.eulerAngles.y;

        public void SetVisible(bool v)
        {
            if (v == _visible) return;
            _visible = v;
            foreach (var r in visuals) if (r) r.enabled = v;
        }

        public void SetHover(bool h)
        {
            if (h == _hover) return;
            _hover = h;
            foreach (var r in visuals) if (r) r.sharedMaterial = h && hoverMaterial ? hoverMaterial : idleMaterial;
        }

        void Update()
        {
            if (!_visible || visuals.Length == 0 || !visuals[0]) return;
            float s = _hover ? 1.15f : 1f + 0.05f * Mathf.Sin(Time.time * 3f + transform.position.x);
            visuals[0].transform.localScale = new Vector3(s, visuals[0].transform.localScale.y, s);
        }
    }
}
