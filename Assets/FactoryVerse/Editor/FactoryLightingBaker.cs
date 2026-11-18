#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FactoryVerse
{
    /// <summary>
    /// Quest optimisation for the FactoryInterior scene: bakes each lighting preset (Normal, Emergency, Zero-G) into its own
    /// set of lightmaps, stores them on FactoryLightingPresets so presets swap lightmaps at runtime (no realtime lights),
    /// then bakes occlusion culling. Re-run after rebuilding the scene.
    /// Run: FactoryVerse > Bake Factory Lighting (Quest).
    /// </summary>
    public static class FactoryLightingBaker
    {
        const string ScenePath = "Assets/FactoryVerse/Scenes/FactoryInterior.unity";
        const string SettingsPath = "Assets/FactoryVerse/Scenes/FactoryInterior_Lighting.lighting";
        const string LightmapFolder = "Assets/FactoryVerse/Art/Lightmaps";

        [MenuItem("FactoryVerse/Bake Factory Lighting (Quest)")]
        public static void Bake()
        {
            if (SceneManager.GetActiveScene().path != ScenePath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                EditorSceneManager.OpenScene(ScenePath);
            }
            var presets = Object.FindAnyObjectByType<FactoryLightingPresets>();
            if (!presets) throw new System.Exception("FactoryLightingPresets not found — build the scene first (FactoryVerse > Build Factory Interior Scene).");

            Lightmapping.lightingSettings = Settings();

            var start = System.DateTime.Now;
            // Normal last, so the scene's own baked data (what the Editor shows) is the Normal preset
            foreach (var p in new[] { FactoryLightingPresets.Preset.Emergency, FactoryLightingPresets.Preset.ZeroGravity, FactoryLightingPresets.Preset.Normal })
            {
                presets.Set(p);
                if (!Lightmapping.Bake()) throw new System.Exception($"Lightmap bake failed for {p}.");
                var maps = CopyLightmaps(p.ToString());
                if (p == FactoryLightingPresets.Preset.Normal) presets.normalLightmaps = maps;
                else if (p == FactoryLightingPresets.Preset.Emergency) presets.emergencyLightmaps = maps;
                else presets.zeroGravityLightmaps = maps;
                Debug.Log($"FactoryVerse: baked {p} lighting ({maps.Length} lightmap(s)).");
            }
            presets.Set(FactoryLightingPresets.Preset.Normal);
            EditorUtility.SetDirty(presets);

            // Occlusion culling: rooms and the corridor hide each other
            StaticOcclusionCulling.smallestOccluder = 1.5f;
            StaticOcclusionCulling.smallestHole = 0.25f;
            StaticOcclusionCulling.backfaceThreshold = 100f;
            StaticOcclusionCulling.Compute();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Debug.Log($"FactoryVerse: factory lighting + occlusion baked in {(System.DateTime.Now - start).TotalMinutes:F1} min.");
        }

        static LightingSettings Settings()
        {
            var ls = AssetDatabase.LoadAssetAtPath<LightingSettings>(SettingsPath);
            if (!ls) { ls = new LightingSettings { name = "FactoryInterior_Lighting" }; AssetDatabase.CreateAsset(ls, SettingsPath); }
            ls.bakedGI = true;
            ls.realtimeGI = false;
            ls.lightmapper = LightingSettings.Lightmapper.ProgressiveGPU;   // falls back to CPU if no suitable GPU
            ls.lightmapResolution = 6f;           // texels per metre: enough for a blockout, small on Quest
            ls.lightmapPadding = 2;
            ls.lightmapMaxSize = 1024;
            ls.directionalityMode = LightmapsMode.NonDirectional;            // half the memory of directional maps
            ls.lightmapCompression = LightmapCompression.NormalQuality;
            ls.directSampleCount = 32;
            ls.indirectSampleCount = 128;
            ls.environmentSampleCount = 64;
            ls.maxBounces = 2;
            ls.ao = true;
            ls.aoMaxDistance = 1f;
            ls.filteringMode = LightingSettings.FilterMode.Auto;
            EditorUtility.SetDirty(ls);
            AssetDatabase.SaveAssets();
            return ls;
        }

        // The scene's lightmaps are overwritten by each bake, so keep a copy per preset.
        static Texture2D[] CopyLightmaps(string preset)
        {
            string folder = $"{LightmapFolder}/{preset}";
            if (AssetDatabase.IsValidFolder(folder))
                foreach (var old in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
                    AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(old));
            Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
            var lms = LightmapSettings.lightmaps;
            var result = new Texture2D[lms.Length];
            for (int i = 0; i < lms.Length; i++)
            {
                string src = AssetDatabase.GetAssetPath(lms[i].lightmapColor);
                string dst = $"{folder}/Lightmap-{i}{Path.GetExtension(src)}";
                if (!AssetDatabase.CopyAsset(src, dst)) throw new System.Exception($"Could not copy {src} to {dst}");
                result[i] = AssetDatabase.LoadAssetAtPath<Texture2D>(dst);
            }
            AssetDatabase.SaveAssets();
            return result;
        }
    }
}
#endif
