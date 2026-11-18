using UnityEngine;

namespace FactoryVerse
{
    /// <summary>Loose objects that slowly tumble and bob while the factory is in the Zero-G lighting preset.</summary>
    public class FactoryZeroGDrift : MonoBehaviour
    {
        public float bob = 0.12f, spinDegPerSec = 18f;
        FactoryLightingPresets _lighting;
        Vector3 _home, _axis; float _phase, _blend;

        void Start()
        {
            _lighting = FindAnyObjectByType<FactoryLightingPresets>();
            _home = transform.position;
            _phase = Mathf.Repeat(_home.x * 0.37f + _home.z * 0.53f, 6.28f);
            _axis = new Vector3(Mathf.Sin(_phase), 1f, Mathf.Cos(_phase * 1.7f)).normalized;
        }

        void Update()
        {
            bool zeroG = _lighting && _lighting.preset == FactoryLightingPresets.Preset.ZeroGravity;
            _blend = Mathf.MoveTowards(_blend, zeroG ? 1f : 0f, Time.deltaTime * 0.5f);
            if (_blend <= 0f) return;
            transform.position = _home + Vector3.up * (bob * _blend * Mathf.Sin(Time.time * 0.8f + _phase))
                                       + new Vector3(Mathf.Sin(Time.time * 0.3f + _phase), 0f, Mathf.Cos(Time.time * 0.25f + _phase)) * 0.08f * _blend;
            transform.Rotate(_axis, spinDegPerSec * _blend * Time.deltaTime, Space.World);
        }
    }
}
