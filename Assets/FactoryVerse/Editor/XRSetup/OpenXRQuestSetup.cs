using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.XR.Management;

namespace FactoryVerse
{
    /// <summary>
    /// Turns on OpenXR for PC (Quest Link) and Android (Quest standalone), with the Meta Quest feature and Touch controller
    /// profiles. Compiles only once com.unity.xr.management is installed (this folder's asmdef has a version define).
    /// OpenXR feature classes are found by name so a renamed/missing profile can't break compilation.
    /// Run: FactoryVerse > XR > Configure OpenXR for Quest.
    /// </summary>
    public static class OpenXRQuestSetup
    {
        const string OpenXRLoader = "UnityEngine.XR.OpenXR.OpenXRLoader";

        [MenuItem("FactoryVerse/XR/Configure OpenXR for Quest")]
        public static void Configure()
        {
            var log = new System.Text.StringBuilder("FactoryVerse OpenXR setup:\n");
            foreach (var g in new[] { BuildTargetGroup.Standalone, BuildTargetGroup.Android })
                log.AppendLine($"  {g}: OpenXR loader {(EnableOpenXR(g) ? "assigned" : "FAILED")}");

            // HandTracking (XR Hands joint data) + MetaHandTrackingAim (pinch/aim) make the game hands-first
            log.AppendLine("  Android features: " + EnableFeatures(BuildTargetGroup.Android,
                "MetaQuestFeature", "HandTracking", "MetaHandTrackingAim",
                "OculusTouchControllerProfile", "MetaQuestTouchPlusControllerProfile", "MetaQuestTouchProControllerProfile"));
            log.AppendLine("  Standalone features: " + EnableFeatures(BuildTargetGroup.Standalone,
                "HandTracking", "MetaHandTrackingAim",
                "OculusTouchControllerProfile", "MetaQuestTouchPlusControllerProfile", "MetaQuestTouchProControllerProfile"));
            AssetDatabase.SaveAssets();
            Debug.Log(log.ToString());
        }

        static bool EnableOpenXR(BuildTargetGroup group)
        {
            if (!EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.settingsKey, out XRGeneralSettingsPerBuildTarget perTarget) || perTarget == null)
            {
                Directory.CreateDirectory("Assets/XR");
                perTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                AssetDatabase.CreateAsset(perTarget, "Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
                EditorBuildSettings.AddConfigObject(XRGeneralSettings.settingsKey, perTarget, true);
            }
            if (!perTarget.HasSettingsForBuildTarget(group)) perTarget.CreateDefaultSettingsForBuildTarget(group);
            if (!perTarget.HasManagerSettingsForBuildTarget(group)) perTarget.CreateDefaultManagerSettingsForBuildTarget(group);
            var settings = perTarget.SettingsForBuildTarget(group);
            settings.InitManagerOnStart = true;   // start XR with the app; FactoryVRRig falls back to desktop if no headset
            bool ok = XRPackageMetadataStore.AssignLoader(settings.Manager, OpenXRLoader, group);
            EditorUtility.SetDirty(settings);
            EditorUtility.SetDirty(perTarget);
            return ok || settings.Manager.activeLoaders.Any(l => l && l.GetType().FullName == OpenXRLoader);
        }

        static string EnableFeatures(BuildTargetGroup group, params string[] typeNames)
        {
            // FeatureHelpers.RefreshFeatures(group) populates the feature list for a target the first time
            var helpers = FindType("UnityEditor.XR.OpenXR.Features.FeatureHelpers");
            var groupArg = new[] { typeof(BuildTargetGroup) };   // both methods have overloads; pick the BuildTargetGroup one
            helpers?.GetMethod("RefreshFeatures", BindingFlags.Public | BindingFlags.Static, null, groupArg, null)?.Invoke(null, new object[] { group });

            var settingsType = FindType("UnityEngine.XR.OpenXR.OpenXRSettings");
            var getSettings = settingsType?.GetMethod("GetSettingsForBuildTargetGroup", BindingFlags.Public | BindingFlags.Static, null, groupArg, null);
            var settings = getSettings?.Invoke(null, new object[] { group }) as UnityEngine.Object;
            if (!settings) return "OpenXR settings not found (is com.unity.xr.openxr installed?)";
            // GetFeatures() and GetFeatures<T>() both take no arguments; use the non-generic one
            var getFeatures = settingsType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m => m.Name == "GetFeatures" && !m.IsGenericMethodDefinition && m.GetParameters().Length == 0);
            var features = getFeatures?.Invoke(settings, null) as Array;
            if (features == null) return "no features";

            var enabled = new System.Collections.Generic.List<string>();
            foreach (var f in features)
            {
                var name = f.GetType().Name;
                if (!typeNames.Contains(name)) continue;
                f.GetType().GetProperty("enabled")?.SetValue(f, true);
                EditorUtility.SetDirty((UnityEngine.Object)f);
                enabled.Add(name);
            }
            EditorUtility.SetDirty(settings);
            var missing = typeNames.Except(enabled).ToArray();
            return string.Join(", ", enabled) + (missing.Length > 0 ? $"  (not available: {string.Join(", ", missing)})" : "");
        }

        // Look the type up in the OpenXR runtime/editor assemblies by assembly-qualified name
        static Type FindType(string fullName) =>
            Type.GetType(fullName + ", Unity.XR.OpenXR") ?? Type.GetType(fullName + ", Unity.XR.OpenXR.Editor");
    }
}
