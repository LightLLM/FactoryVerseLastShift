using UnityEngine;

namespace FactoryVerse
{
    /// <summary>
    /// Switches the FV-01 factory interior between its three lighting moods:
    /// Normal (cool cyan operations), Emergency (pulsing red alarm) and Zero-G (dim blue with blinking amber warnings).
    /// Each mood is a child group of lights; this enables one group and sets the matching ambient colour.
    /// Keys 1 / 2 / 3 switch at runtime; changing the field in the Inspector previews it in the Editor.
    /// </summary>
    [ExecuteAlways]
    public class FactoryLightingPresets : MonoBehaviour
    {
        public enum Preset { Normal, Emergency, ZeroGravity }

        public Preset preset = Preset.Normal;
        public GameObject normalLights;
        public GameObject emergencyLights;
        public GameObject zeroGravityLights;

        [Header("Baked lightmaps per preset (filled by FactoryVerse > Bake Factory Lighting)")]
        [Tooltip("On Quest the lights are baked; switching presets swaps these lightmap sets instead of turning realtime lights on and off.")]
        public Texture2D[] normalLightmaps;
        public Texture2D[] emergencyLightmaps;
        public Texture2D[] zeroGravityLightmaps;

        [Header("Ambient colour per preset")]
        public Color normalAmbient = new Color(0.18f, 0.22f, 0.28f);
        public Color emergencyAmbient = new Color(0.16f, 0.03f, 0.03f);
        public Color zeroGravityAmbient = new Color(0.05f, 0.07f, 0.12f);

        [Header("Animation")]
        public float emergencyPulseSpeed = 2.5f;   // alarm pulses per second (approx.)
        public float warningBlinkSpeed = 1.2f;     // zero-G amber blink rate

        Light[] _emergency, _zeroG;
        float[] _emergencyBase, _zeroGBase;
        Preset _applied = (Preset)(-1);

        void OnEnable() { Cache(); Apply(); }
        void OnValidate() { Cache(); Apply(); }

        void Cache()
        {
            _emergency = emergencyLights ? emergencyLights.GetComponentsInChildren<Light>(true) : new Light[0];
            _zeroG = zeroGravityLights ? zeroGravityLights.GetComponentsInChildren<Light>(true) : new Light[0];
            _emergencyBase = BaseIntensities(_emergency);
            _zeroGBase = BaseIntensities(_zeroG);
        }

        static float[] BaseIntensities(Light[] lights)
        {
            var a = new float[lights.Length];
            for (int i = 0; i < lights.Length; i++) a[i] = lights[i].intensity;
            return a;
        }

        public void Set(Preset p) { preset = p; Apply(); }

        public void Apply()
        {
            if (normalLights) normalLights.SetActive(preset == Preset.Normal);
            if (emergencyLights) emergencyLights.SetActive(preset == Preset.Emergency);
            if (zeroGravityLights) zeroGravityLights.SetActive(preset == Preset.ZeroGravity);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = preset == Preset.Normal ? normalAmbient
                                        : preset == Preset.Emergency ? emergencyAmbient : zeroGravityAmbient;
            // Swap baked lightmaps (Play mode only, so the Editor's own baked data is left alone)
            var maps = preset == Preset.Normal ? normalLightmaps : preset == Preset.Emergency ? emergencyLightmaps : zeroGravityLightmaps;
            if (Application.isPlaying && maps != null && maps.Length > 0)
            {
                var data = new LightmapData[maps.Length];
                for (int i = 0; i < maps.Length; i++) data[i] = new LightmapData { lightmapColor = maps[i] };
                LightmapSettings.lightmaps = data;
            }
            _applied = preset;
        }

        void Update()
        {
            if (Application.isPlaying)
            {
                if (FactoryVerse.FVInput.GetKeyDown(KeyCode.Alpha1)) Set(Preset.Normal);
                if (FactoryVerse.FVInput.GetKeyDown(KeyCode.Alpha2)) Set(Preset.Emergency);
                if (FactoryVerse.FVInput.GetKeyDown(KeyCode.Alpha3)) Set(Preset.ZeroGravity);
            }
            if (_applied != preset) Apply();
            if (!Application.isPlaying || _emergency == null) return;

            float t = Time.time;
            if (preset == Preset.Emergency)
            {
                float k = 0.35f + 0.65f * (0.5f + 0.5f * Mathf.Sin(t * emergencyPulseSpeed * Mathf.PI * 2f));
                for (int i = 0; i < _emergency.Length; i++) _emergency[i].intensity = _emergencyBase[i] * k;
            }
            else if (preset == Preset.ZeroGravity)
            {
                bool on = Mathf.Repeat(t * warningBlinkSpeed, 1f) < 0.5f;
                for (int i = 0; i < _zeroG.Length; i++)
                    if (_zeroG[i].name.StartsWith("Warning")) _zeroG[i].intensity = on ? _zeroGBase[i] : 0f;
            }
        }
    }
}
