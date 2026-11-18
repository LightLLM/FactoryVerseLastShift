#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace FactoryVerse
{
    /// <summary>
    /// Creates the shared FV_ materials (Built-in "Standard" shader) used by the Blender-exported
    /// models in Assets/FactoryVerse/Art/Models, and remaps each FBX's embedded materials to them by name.
    /// Values mirror the Blender materials (colours are linear, as authored in Blender).
    /// Run: FactoryVerse > Setup Model Materials. New FBX files dropped into the models folder are remapped automatically.
    /// </summary>
    public static class ModelMaterialSetup
    {
        public const string MaterialsFolder = "Assets/FactoryVerse/Art/Materials";
        public const string ModelsFolder = "Assets/FactoryVerse/Art/Models";

        enum Surface { Opaque, Glass, Fade }

        struct Spec
        {
            public string Name; public Color Linear; public float Metallic; public float Roughness;
            public Color EmissionLinear; public float EmissionIntensity; public Surface Surface; public float Alpha;
        }

        static Spec Opaque(string n, float r, float g, float b, float metallic, float rough) =>
            new Spec { Name = n, Linear = new Color(r, g, b), Metallic = metallic, Roughness = rough, Surface = Surface.Opaque, Alpha = 1f };

        static Spec Glow(string n, float r, float g, float b, float er, float eg, float eb, float intensity) =>
            new Spec { Name = n, Linear = new Color(r, g, b), Roughness = 0.4f, EmissionLinear = new Color(er, eg, eb), EmissionIntensity = intensity, Surface = Surface.Opaque, Alpha = 1f };

        static readonly Spec[] Specs =
        {
            Opaque("FV_Metal_Clean",       0.62f, 0.64f, 0.67f, 1.0f, 0.28f),
            Opaque("FV_Painted_Panel",     0.80f, 0.81f, 0.82f, 0.0f, 0.45f),
            Opaque("FV_Industrial_Yellow", 0.85f, 0.55f, 0.05f, 0.0f, 0.45f),
            Opaque("FV_Warning_Red",       0.60f, 0.05f, 0.04f, 0.0f, 0.45f),
            Opaque("FV_Greeble_Dark",      0.06f, 0.065f, 0.075f, 0.6f, 0.55f),
            Opaque("FV_Copper",            0.72f, 0.38f, 0.18f, 1.0f, 0.35f),
            Opaque("FV_Hull_Grey",         0.33f, 0.35f, 0.38f, 0.4f, 0.50f),
            Opaque("FV_SolarPanel",        0.02f, 0.05f, 0.18f, 0.8f, 0.25f),
            Glow("FV_Emissive_Blue",   0.00f, 0.20f, 0.80f,  0.00f, 0.30f, 1.00f, 2.0f),
            Glow("FV_Emissive_Orange", 1.00f, 0.30f, 0.03f,  1.00f, 0.35f, 0.05f, 3.0f),
            Glow("FV_Emissive_Red",    0.60f, 0.02f, 0.01f,  0.80f, 0.02f, 0.01f, 1.2f),
            Glow("FV_Emissive_Green",  0.15f, 1.00f, 0.30f,  0.15f, 1.00f, 0.30f, 2.0f),
            Glow("FV_Emissive_White",  0.90f, 0.95f, 1.00f,  0.90f, 0.95f, 1.00f, 1.5f),
            Glow("FV_Earth",           0.02f, 0.12f, 0.45f,  0.02f, 0.12f, 0.45f, 0.6f),
            // Player suit / characters (character sheet palette)
            Opaque("FV_Fabric_Orange",     1.00f, 0.195f, 0.0f, 0.0f, 0.70f),
            Opaque("FV_Fabric_Graphite",   0.032f, 0.035f, 0.040f, 0.0f, 0.85f),
            Opaque("FV_Metal_Brushed",     0.42f, 0.43f, 0.45f, 1.0f, 0.38f),
            Opaque("FV_Plastic_Matte",     0.018f, 0.019f, 0.022f, 0.0f, 0.60f),
            Opaque("FV_Skin_Light",        0.60f, 0.36f, 0.25f, 0.0f, 0.55f),
            Opaque("FV_Hair_Dark",         0.020f, 0.013f, 0.010f, 0.0f, 0.70f),
            Opaque("FV_Eye_Dark",          0.010f, 0.010f, 0.012f, 0.0f, 0.20f),
            Opaque("FV_Stubble",           0.30f, 0.19f, 0.14f, 0.0f, 0.80f),
            Opaque("FV_Lip",               0.45f, 0.20f, 0.16f, 0.0f, 0.50f),
            Glow("FV_Emissive_Cyan",   0.00f, 0.66f, 1.00f,  0.00f, 0.66f, 1.00f, 2.5f),
            // Commander Hale
            Opaque("FV_Fabric_Dark",       0.022f, 0.024f, 0.030f, 0.0f, 0.85f),
            Opaque("FV_Accent_Red",        0.72f, 0.015f, 0.035f, 0.0f, 0.55f),
            Opaque("FV_Gunmetal",          0.17f, 0.18f, 0.20f, 1.0f, 0.40f),
            Opaque("FV_Leather_Black",     0.012f, 0.012f, 0.014f, 0.0f, 0.42f),
            Opaque("FV_Hair_Auburn",       0.24f, 0.045f, 0.018f, 0.0f, 0.65f),
            Opaque("FV_Skin_Fair",         0.70f, 0.45f, 0.35f, 0.0f, 0.55f),
            Glow("FV_Emissive_RedGlow", 0.80f, 0.02f, 0.03f,  1.00f, 0.04f, 0.06f, 2.5f),
            // ARIA hologram: cyan, ~70% transparent, glowing (character sheet: additive/emissive)
            new Spec { Name = "FV_ARIA_Hologram", Linear = new Color(0.00f, 0.45f, 1.00f), Roughness = 0.3f,
                       EmissionLinear = new Color(0.00f, 0.66f, 1.00f), EmissionIntensity = 2.0f, Surface = Surface.Fade, Alpha = 0.3f },
            new Spec { Name = "FV_Glass", Linear = new Color(0.30f, 0.60f, 0.90f), Roughness = 0.05f, Surface = Surface.Glass, Alpha = 0.12f },
            new Spec { Name = "FV_Hologram", Linear = new Color(0.00f, 0.40f, 0.90f), Roughness = 0.3f,
                       EmissionLinear = new Color(0.00f, 0.55f, 1.00f), EmissionIntensity = 2.5f, Surface = Surface.Fade, Alpha = 0.35f },
            new Spec { Name = "FV_HUD_BG", Linear = new Color(0.005f, 0.02f, 0.05f), Roughness = 0.5f,
                       EmissionLinear = new Color(0.00f, 0.06f, 0.15f), EmissionIntensity = 1.0f, Surface = Surface.Fade, Alpha = 0.75f },
        };

        [MenuItem("FactoryVerse/Setup Model Materials")]
        public static void Setup()
        {
            var created = CreateOrUpdateMaterials();
            int chars = CreateCharacterMaterials();
            int remapped = RemapAllModels();
            Debug.Log($"FactoryVerse: {created} FV_ materials + {chars} character material(s) ready in {MaterialsFolder}; {remapped} model(s) remapped in {ModelsFolder}.");
        }

        public const string CharacterTexturesFolder = "Assets/FactoryVerse/Art/Textures/Characters";

        /// <summary>
        /// For every baked character texture set T_{Char}_Albedo/Emission/AO.png, make M_{Char}.mat (Standard, albedo +
        /// occlusion + emission). FBX character models export a material named M_{Char}, which AddRemaps points at it.
        /// </summary>
        public static int CreateCharacterMaterials()
        {
            if (!AssetDatabase.IsValidFolder(CharacterTexturesFolder)) return 0;
            int count = 0;
            foreach (var guid in AssetDatabase.FindAssets("_Albedo t:Texture2D", new[] { CharacterTexturesFolder }))
            {
                string albedoPath = AssetDatabase.GUIDToAssetPath(guid);
                string file = Path.GetFileNameWithoutExtension(albedoPath);           // T_Char_Engineer_Albedo
                if (!file.StartsWith("T_") || !file.EndsWith("_Albedo")) continue;
                string ch = file.Substring(2, file.Length - 2 - "_Albedo".Length);    // Char_Engineer
                string dir = Path.GetDirectoryName(albedoPath).Replace('\\', '/');
                var albedo = LoadTex(albedoPath, srgb: true);
                var ao = LoadTex($"{dir}/T_{ch}_AO.png", srgb: false);
                var emission = LoadTex($"{dir}/T_{ch}_Emission.png", srgb: true);

                string path = $"{MaterialsFolder}/M_{ch}.mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                bool isNew = mat == null;
                if (isNew) mat = new Material(Shader.Find("Standard")) { name = "M_" + ch };
                mat.SetTexture("_MainTex", albedo);
                mat.SetColor("_Color", Color.white);
                mat.SetFloat("_Metallic", 0f);
                mat.SetFloat("_Glossiness", 0.4f);
                if (ao) { mat.SetTexture("_OcclusionMap", ao); mat.SetFloat("_OcclusionStrength", 1f); }
                if (emission)
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetTexture("_EmissionMap", emission);
                    mat.SetColor("_EmissionColor", Color.white * 2f);
                    mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
                }
                ToRenderPipeline(mat);
                if (isNew) AssetDatabase.CreateAsset(mat, path); else EditorUtility.SetDirty(mat);
                count++;
            }
            AssetDatabase.SaveAssets();
            return count;
        }

        static Texture2D LoadTex(string path, bool srgb)
        {
            if (AssetImporter.GetAtPath(path) is TextureImporter ti)
            {
                bool changed = ti.sRGBTexture != srgb || ti.maxTextureSize != 2048;
                ti.sRGBTexture = srgb; ti.maxTextureSize = 2048;
                if (changed) ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        public static int CreateOrUpdateMaterials()
        {
            var shader = Shader.Find("Standard");
            if (!shader) throw new System.Exception("Built-in 'Standard' shader not found.");
            Directory.CreateDirectory(MaterialsFolder);

            foreach (var s in Specs)
            {
                string path = $"{MaterialsFolder}/{s.Name}.mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                bool isNew = mat == null;
                if (isNew) mat = new Material(shader) { name = s.Name };
                else mat.shader = shader;

                var albedo = s.Linear.gamma;           // Material colours are set in gamma space; Unity converts in Linear projects
                albedo.a = s.Alpha;
                mat.SetColor("_Color", albedo);
                mat.SetFloat("_Metallic", s.Metallic);
                mat.SetFloat("_Glossiness", 1f - s.Roughness);

                if (s.EmissionIntensity > 0f)
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", s.EmissionLinear.gamma * s.EmissionIntensity);
                    mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;   // glow strips light their surroundings in baked lightmaps
                }
                else
                {
                    mat.DisableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", Color.black);
                    mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                }

                ApplySurface(mat, s.Surface);
                ToRenderPipeline(mat);

                if (isNew) AssetDatabase.CreateAsset(mat, path);
                else EditorUtility.SetDirty(mat);
            }
            AssetDatabase.SaveAssets();
            return Specs.Length;
        }

        // Mirrors what the Standard shader's inspector does when "Rendering Mode" changes.
        /// <summary>
        /// The Meta VR project renders with URP: convert a material authored for the built-in Standard shader to URP Lit,
        /// keeping colour, metal/smoothness, occlusion, emission and transparency. No-op in a built-in pipeline project.
        /// </summary>
        public static void ToRenderPipeline(Material mat)
        {
            if (GraphicsSettings.currentRenderPipeline == null) return;
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            if (!lit) { Debug.LogWarning("FactoryVerse: URP Lit shader not found."); return; }

            var color = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
            var mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
            float metallic = mat.HasProperty("_Metallic") ? mat.GetFloat("_Metallic") : 0f;
            float smooth = mat.HasProperty("_Glossiness") ? mat.GetFloat("_Glossiness") : 0.5f;
            var occ = mat.HasProperty("_OcclusionMap") ? mat.GetTexture("_OcclusionMap") : null;
            var emitColor = mat.HasProperty("_EmissionColor") ? mat.GetColor("_EmissionColor") : Color.black;
            var emitTex = mat.HasProperty("_EmissionMap") ? mat.GetTexture("_EmissionMap") : null;
            bool emissive = mat.IsKeywordEnabled("_EMISSION");
            float mode = mat.HasProperty("_Mode") ? mat.GetFloat("_Mode") : 0f;   // 0 opaque, 2 fade, 3 transparent
            var gi = mat.globalIlluminationFlags;

            mat.shader = lit;
            mat.SetColor("_BaseColor", color);
            if (mainTex) mat.SetTexture("_BaseMap", mainTex);
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smooth);
            if (occ) mat.SetTexture("_OcclusionMap", occ);
            mat.SetColor("_EmissionColor", emitColor);
            if (emitTex) mat.SetTexture("_EmissionMap", emitTex);
            if (emissive) mat.EnableKeyword("_EMISSION"); else mat.DisableKeyword("_EMISSION");
            mat.globalIlluminationFlags = gi;

            bool transparent = mode >= 2f;
            mat.SetFloat("_Surface", transparent ? 1f : 0f);
            mat.SetFloat("_Blend", mode >= 3f ? 1f : 0f);   // 0 alpha (fade), 1 premultiply (glass)
            mat.SetFloat("_ZWrite", transparent ? 0f : 1f);
            mat.SetFloat("_SrcBlend", transparent ? (mode >= 3f ? (float)BlendMode.One : (float)BlendMode.SrcAlpha) : (float)BlendMode.One);
            mat.SetFloat("_DstBlend", transparent ? (float)BlendMode.OneMinusSrcAlpha : (float)BlendMode.Zero);
            if (transparent) { mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); mat.SetOverrideTag("RenderType", "Transparent"); mat.renderQueue = (int)RenderQueue.Transparent; }
            else { mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT"); mat.SetOverrideTag("RenderType", "Opaque"); mat.renderQueue = (int)RenderQueue.Geometry; }
            if (mode >= 3f) mat.EnableKeyword("_ALPHAPREMULTIPLY_ON"); else mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        }

        static void ApplySurface(Material mat, Surface surface)
        {
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.DisableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            switch (surface)
            {
                case Surface.Opaque:
                    mat.SetFloat("_Mode", 0f);
                    mat.SetOverrideTag("RenderType", "");
                    mat.SetInt("_SrcBlend", (int)BlendMode.One);
                    mat.SetInt("_DstBlend", (int)BlendMode.Zero);
                    mat.SetInt("_ZWrite", 1);
                    mat.renderQueue = -1;
                    break;
                case Surface.Fade:      // whole surface fades (holograms, HUD backgrounds)
                    mat.SetFloat("_Mode", 2f);
                    mat.SetOverrideTag("RenderType", "Transparent");
                    mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.EnableKeyword("_ALPHABLEND_ON");
                    mat.renderQueue = (int)RenderQueue.Transparent;
                    break;
                case Surface.Glass:     // keeps reflections/highlights while see-through
                    mat.SetFloat("_Mode", 3f);
                    mat.SetOverrideTag("RenderType", "Transparent");
                    mat.SetInt("_SrcBlend", (int)BlendMode.One);
                    mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                    mat.renderQueue = (int)RenderQueue.Transparent;
                    break;
            }
        }

        public static int RemapAllModels()
        {
            if (!AssetDatabase.IsValidFolder(ModelsFolder)) return 0;
            int count = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { ModelsFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase)) continue;
                if (AssetImporter.GetAtPath(path) is ModelImporter importer && (AddRemaps(importer) | EnsureLightmapUVs(importer)))
                {
                    importer.SaveAndReimport();
                    count++;
                }
            }
            return count;
        }

        /// <summary>Lightmap UVs (UV2) are needed to bake lighting on Quest. Returns true if the setting changed.</summary>
        internal static bool EnsureLightmapUVs(ModelImporter importer)
        {
            if (importer.generateSecondaryUV) return false;
            importer.generateSecondaryUV = true;
            return true;
        }

        /// <summary>Points every FV_ material slot in the model at the shared material asset. Returns true if anything changed.</summary>
        internal static bool AddRemaps(ModelImporter importer)
        {
            bool changed = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            var existing = importer.GetExternalObjectMap();
            // baked character material: FV_Char_X.fbx -> M_Char_X
            string fbxName = Path.GetFileNameWithoutExtension(importer.assetPath);
            if (fbxName.StartsWith("FV_Char_"))
            {
                string ch = fbxName.Substring(3);
                var cm = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/M_{ch}.mat");
                var cid = new AssetImporter.SourceAssetIdentifier(typeof(Material), "M_" + ch);
                if (cm && !(existing.TryGetValue(cid, out var cur) && cur == cm)) { importer.AddRemap(cid, cm); changed = true; }
            }
            foreach (var s in Specs)
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/{s.Name}.mat");
                if (!mat) continue;
                var id = new AssetImporter.SourceAssetIdentifier(typeof(Material), s.Name);
                if (existing.TryGetValue(id, out var current) && current == mat) continue;
                importer.AddRemap(id, mat);
                changed = true;
            }
            return changed;
        }
    }

    /// <summary>Remaps FV_ materials on any FBX imported into the models folder (once the materials exist).</summary>
    class ModelMaterialPostprocessor : AssetPostprocessor
    {
        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(ModelMaterialSetup.ModelsFolder + "/") &&
                !assetPath.StartsWith("Assets/FactoryVerse/Art/Blender/")) return;
            if (assetImporter is ModelImporter importer)
            {
                importer.importCameras = false;
                importer.importLights = false;
                // Characters carry their Walk take: import it as a legacy clip (played by FactoryNPCWalker).
                bool character = System.IO.Path.GetFileNameWithoutExtension(assetPath).StartsWith("FV_Char_");
                importer.importAnimation = character;
                if (character) importer.animationType = ModelImporterAnimationType.Legacy;
                ModelMaterialSetup.EnsureLightmapUVs(importer);
                ModelMaterialSetup.AddRemaps(importer);
            }
        }
    }
}
#endif
