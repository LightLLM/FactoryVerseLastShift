#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

namespace FactoryVerse
{
    /// <summary>
    /// Meta Quest player settings for Android (safe to run without the Android module; they apply once you build for Android).
    /// Run: FactoryVerse > XR > Apply Quest Player Settings.
    /// </summary>
    public static class QuestProjectSettings
    {
        [MenuItem("FactoryVerse/XR/Apply Quest Player Settings")]
        public static void Apply()
        {
            var android = NamedBuildTarget.Android;
            PlayerSettings.SetApplicationIdentifier(android, "com.factoryverse.lastshift");
            if (string.IsNullOrEmpty(PlayerSettings.bundleVersion) || PlayerSettings.bundleVersion == "0.1") PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.Android.bundleVersionCode = Mathf.Max(1, PlayerSettings.Android.bundleVersionCode);
            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);   // required for ARM64
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel32;   // Quest minimum
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan, GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.SetMobileMTRendering(android, true);
            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            SetStaticBatching(BuildTarget.Android);
            AssetDatabase.SaveAssets();
            Debug.Log("FactoryVerse: Quest player settings applied (Android: IL2CPP, ARM64, API 32+, Vulkan/GLES3, ASTC, Linear, static batching).");
        }

        const string ApkPath = "Builds/Quest/FactoryVerse_LastShift.apk";
        const string QuestScene = "Assets/FactoryVerse/Scenes/FactoryInterior.unity";

        /// <summary>
        /// Builds the hands-first Quest APK of the FactoryInterior demo to Builds/Quest/. Needs Android Build Support.
        /// Each build bumps the Android versionCode (the Meta Developer Dashboard rejects a re-upload with the same code).
        /// </summary>
        [MenuItem("FactoryVerse/XR/Build Quest APK")]
        public static void BuildQuestApk()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
                throw new System.Exception("Android Build Support is not installed for this Editor.");
            Apply();
            PlayerSettings.Android.bundleVersionCode += 1;
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(ApkPath));
            EditorUserBuildSettings.buildAppBundle = false;
            ApplyReleaseSigning();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { QuestScene },
                locationPathName = ApkPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None,
            });
            var s = report.summary;
            Debug.Log($"FactoryVerse: Quest APK build {s.result} — {ApkPath}, {s.totalSize / (1024f * 1024f):F1} MB, versionCode {PlayerSettings.Android.bundleVersionCode}, " +
                      $"{s.totalErrors} error(s), {s.totalWarnings} warning(s), {s.totalTime.TotalMinutes:F1} min.");
            if (s.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new System.Exception("Quest APK build failed — see the Console.");
        }

        /// <summary>
        /// Release-sign with the FactoryVerse key (the Meta store rejects debug-signed APKs). The keystore and its
        /// passwords live in Keystore/ at the project root, outside Assets, and are never written to ProjectSettings.
        /// Without that folder the build falls back to Unity's debug key and says so.
        /// </summary>
        static void ApplyReleaseSigning()
        {
            var dir = System.IO.Path.GetFullPath("Keystore");
            var props = System.IO.Path.Combine(dir, "keystore-credentials.properties");
            if (!System.IO.File.Exists(props))
            {
                PlayerSettings.Android.useCustomKeystore = false;
                Debug.LogWarning("FactoryVerse: Keystore/keystore-credentials.properties not found, so this APK is DEBUG-signed and cannot be uploaded to the Meta store.");
                return;
            }
            var kv = new System.Collections.Generic.Dictionary<string, string>();
            foreach (var line in System.IO.File.ReadAllLines(props))
            {
                var l = line.Trim();
                if (l.Length == 0 || l.StartsWith("#")) continue;
                int eq = l.IndexOf('=');
                if (eq > 0) kv[l.Substring(0, eq).Trim()] = l.Substring(eq + 1).Trim();
            }
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = System.IO.Path.Combine(dir, kv["keystore"]);
            PlayerSettings.Android.keystorePass = kv["storepass"];
            PlayerSettings.Android.keyaliasName = kv["alias"];
            PlayerSettings.Android.keyaliasPass = kv["keypass"];
            Debug.Log("FactoryVerse: release signing with Keystore/" + kv["keystore"] + " (alias " + kv["alias"] + ").");
        }

        // Static batching per platform has no public setter; the Player Settings window uses this internal API.
        static void SetStaticBatching(BuildTarget target)
        {
            var t = typeof(PlayerSettings);
            var get = t.GetMethod("GetBatchingForPlatform", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            var set = t.GetMethod("SetBatchingForPlatform", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            if (get == null || set == null) { Debug.LogWarning("FactoryVerse: couldn't set static batching for Android; enable it in Player Settings > Other Settings."); return; }
            var args = new object[] { target, 0, 0 };
            get.Invoke(null, args);
            set.Invoke(null, new object[] { target, 1, args[2] });
        }
    }
}
#endif
