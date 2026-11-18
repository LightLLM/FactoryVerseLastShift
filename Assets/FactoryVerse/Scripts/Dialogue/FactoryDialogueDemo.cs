using UnityEngine;

namespace FactoryVerse
{
    /// <summary>
    /// Drives the voice script in the FactoryInterior scene until the full mission flow is wired up:
    /// the Mission 01 intro plays on start, and changing the lighting preset plays the matching story beat
    /// (Emergency → Mission 03 failure, Zero-G → Mission 04, back to Normal from Zero-G → power restored).
    /// Number keys 4–9 replay a mission sequence for testing: 4 = M01, 5 = M02, 6 = M03, 7 = M04, 8 = M05, 9 = M06.
    /// </summary>
    public class FactoryDialogueDemo : MonoBehaviour
    {
        public FactoryLightingPresets lighting;
        public float introDelay = 1.5f;

        FactoryLightingPresets.Preset _last;
        static readonly (KeyCode key, string seq)[] TestKeys =
        {
            (KeyCode.Alpha4, "M01_Intro"), (KeyCode.Alpha5, "M02_R7Online"), (KeyCode.Alpha6, "M03_Failure"),
            (KeyCode.Alpha7, "M04_ZeroG"), (KeyCode.Alpha8, "M05_Escape"), (KeyCode.Alpha9, "M06_Launch"),
        };

        void Start()
        {
            if (lighting) _last = lighting.preset;
            Invoke(nameof(Intro), introDelay);
        }

        void Intro() => DialogueManager.PlaySequence("M01_Intro");

        void Update()
        {
            if (lighting && lighting.preset != _last)
            {
                var now = lighting.preset;
                DialogueManager.StopAll();
                if (now == FactoryLightingPresets.Preset.Emergency) DialogueManager.PlaySequence("M03_Failure");
                else if (now == FactoryLightingPresets.Preset.ZeroGravity) DialogueManager.PlaySequence("M04_ZeroG");
                else if (_last == FactoryLightingPresets.Preset.ZeroGravity) DialogueManager.PlaySequence("M04_Restored");
                _last = now;
            }
            foreach (var (key, seq) in TestKeys)
                if (FactoryVerse.FVInput.GetKeyDown(key)) { DialogueManager.StopAll(); DialogueManager.PlaySequence(seq); }
        }
    }
}
