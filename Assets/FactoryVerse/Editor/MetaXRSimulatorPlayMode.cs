#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FactoryVerse
{
    /// <summary>
    /// Runs Play mode in the Meta XR Simulator (no headset needed) by selecting it as Unity's
    /// "Play Mode OpenXR Runtime" (Project Settings > XR Plug-in Management > OpenXR). Editor-only:
    /// it changes neither the Android/Quest build nor the machine-wide OpenXR runtime.
    /// Toggle: FactoryVerse > XR > Use Meta XR Simulator in Play Mode. The choice is remembered per machine.
    /// </summary>
    [InitializeOnLoad]
    public static class MetaXRSimulatorPlayMode
    {
        const string MenuPath = "FactoryVerse/XR/Use Meta XR Simulator in Play Mode";
        const string PrefKey = "FactoryVerse.UseMetaXRSimulator";
        const string SimRoot = @"C:\Program Files\MetaXRSimulator";

        static MetaXRSimulatorPlayMode()
        {
            EditorApplication.delayCall += () => { if (Enabled) Apply(true, quiet: true); };
        }

        static bool Enabled
        {
            get => EditorPrefs.GetBool(PrefKey, false);
            set => EditorPrefs.SetBool(PrefKey, value);
        }

        /// <summary>Newest installed simulator runtime JSON, or null.</summary>
        public static string FindRuntimeJson()
        {
            if (!Directory.Exists(SimRoot)) return null;
            return Directory.GetDirectories(SimRoot)
                .OrderByDescending(d => d, StringComparer.OrdinalIgnoreCase)
                .Select(d => Path.Combine(d, "meta_openxr_simulator.json"))
                .FirstOrDefault(File.Exists);
        }

        [MenuItem(MenuPath)]
        static void Toggle()
        {
            Enabled = !Enabled;
            Apply(Enabled, quiet: false);
        }

        [MenuItem(MenuPath, true)]
        static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, Enabled);
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        public static void Apply(bool on, bool quiet)
        {
            var asm = typeof(UnityEditor.XR.OpenXR.Features.OpenXRFeatureSetManager).Assembly;
            var selector = asm.GetType("UnityEditor.XR.OpenXR.OpenXRRuntimeSelector");
            var other = asm.GetType("UnityEditor.XR.OpenXR.OpenXRRuntimeSelector+OtherRuntime");
            var setSelected = selector?.GetMethod("SetSelectedRuntime", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            if (selector == null || other == null || setSelected == null)
            {
                Debug.LogWarning("FactoryVerse: this OpenXR package version has no Play Mode runtime selector; pick the simulator JSON in Project Settings > XR Plug-in Management > OpenXR > Play Mode OpenXR Runtime > Other.");
                return;
            }

            if (!on)
            {
                setSelected.Invoke(null, new object[] { "" });   // back to System Default
                if (!quiet) Debug.Log("FactoryVerse: Play mode uses the system OpenXR runtime again (Quest Link, if connected).");
                return;
            }

            var json = FindRuntimeJson();
            if (json == null)
            {
                Debug.LogWarning($"FactoryVerse: Meta XR Simulator not found under {SimRoot}. Install it from Meta Quest Developer Hub > Tools.");
                return;
            }

            // Register the JSON as the "Other" runtime, then select it; the selector sets XR_RUNTIME_JSON on entering Play mode.
            var detectorsProp = selector.GetProperty("RuntimeDetectors", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            if (detectorsProp?.GetValue(null) is System.Collections.IEnumerable detectors)
                foreach (var d in detectors)
                    if (other.IsInstanceOfType(d))
                        other.GetMethod("SetOtherRuntimeJsonPath").Invoke(d, new object[] { json });
            Environment.SetEnvironmentVariable("OTHER_XR_RUNTIME_JSON", json);
            setSelected.Invoke(null, new object[] { json });
            if (!quiet) Debug.Log("FactoryVerse: Play mode now runs in the Meta XR Simulator (" + json + "). Press Play.");
        }
    }
}
#endif
