#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FactoryVerse
{
    /// <summary>
    /// Builds Assets/FactoryVerse/Scenes/ModelShowcase.unity: every Blender-exported model laid out in labelled rows
    /// on a floor, with the station and Earth in the background. Separate from LastShift; not added to Build Settings.
    /// Run: FactoryVerse > Build Model Showcase Scene (safe to re-run; it regenerates the scene).
    /// </summary>
    public static class ModelShowcaseBuilder
    {
        const string ScenePath = "Assets/FactoryVerse/Scenes/ModelShowcase.unity";
        const float Gap = 1.0f;       // space between models in a row (m)
        const float RowGap = 3.5f;    // space between rows (m), leaves room for the labels
        // Blender fronts (-Y) arrive facing +Z in Unity; the showcase camera looks toward +Z, so turn models round.
        static readonly Quaternion FaceCamera = Quaternion.Euler(0f, 180f, 0f);
        const float ItemLabelSize = 0.30f;

        // Rough width of a TextMesh label (LegacyRuntime font averages ~0.6 em per character).
        static float LabelWidth(string modelName, float size) => modelName.Replace("FV_", "").Length * size * 0.6f;

        // Front row first. Floating items hover above the floor like zero-G debris.
        static readonly (string title, string[] models, float lift)[] Rows =
        {
            ("Robot",        new[] { "FV_UnitR7", "FV_HeadUnit", "FV_ArmModule_L", "FV_ArmModule_R", "FV_PowerCore", "FV_PowerCell" }, 0f),
            ("Floating tools", new[] { "FV_Tool_Drill", "FV_Tool_Wrench", "FV_Tool_Tablet", "FV_Tool_Crate" }, 0.9f),
            ("Props",        new[] { "FV_Prop_Crate", "FV_Prop_Canister", "FV_Prop_StorageBox", "FV_Prop_Barrier" }, 0f),
            ("UI",           new[] { "FV_ARIA_HoloDisplay", "FV_MissionHUD" }, 0f),
            ("Stations",     new[] { "FV_AssemblyWorkstation", "FV_ControlConsole", "FV_EscapePod" }, 0f),
            ("Corridor kit", new[] { "FV_Corridor_Module", "FV_Corridor_Floor_2m", "FV_Corridor_Wall_2m", "FV_Corridor_Ceiling_2m", "FV_Corridor_Frame" }, 0f),
            ("Window",       new[] { "FV_ObservationWindow" }, 0f),
        };

        [MenuItem("FactoryVerse/Build Model Showcase Scene")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            ModelMaterialSetup.Setup();   // make sure FV_ materials exist and are mapped

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var root = new GameObject("Model Showcase").transform;
            var missing = new List<string>();

            float z = 0f, minX = 0f, maxX = 0f;
            foreach (var row in Rows)
            {
                var rowRoot = new GameObject(row.title).transform;
                rowRoot.SetParent(root, false);
                float x = 0f, rowDepth = 0f;
                var placed = new List<(Transform t, Bounds b)>();

                foreach (var name in row.models)
                {
                    var go = Spawn(name, rowRoot);
                    if (!go) { missing.Add(name); continue; }
                    go.transform.rotation = FaceCamera;
                    var b = BoundsOf(go);
                    // each model gets a slot wide enough for itself and its label; centred in it,
                    // front edge at z, bottom on the floor (+ lift)
                    float slot = Mathf.Max(b.size.x, LabelWidth(go.name, ItemLabelSize));
                    go.transform.position += new Vector3(x + slot / 2f - b.center.x, row.lift - b.min.y, z - b.min.z);
                    b = BoundsOf(go);
                    placed.Add((go.transform, b));
                    x += slot + Gap;
                    rowDepth = Mathf.Max(rowDepth, b.size.z);
                }
                if (placed.Count == 0) { Object.DestroyImmediate(rowRoot.gameObject); continue; }

                // centre the row on x = 0 and label each model
                float shift = -(x - Gap) / 2f;
                foreach (var (t, b) in placed)
                {
                    t.position += Vector3.right * shift;
                    Label(t.name, new Vector3(b.center.x + shift, 0.02f, z - 0.6f), rowRoot, ItemLabelSize);
                }
                Label(row.title.ToUpperInvariant(), new Vector3(shift - 0.8f, 0.02f, z + rowDepth * 0.5f), rowRoot, 0.55f, TextAnchor.MiddleRight);
                minX = Mathf.Min(minX, shift - 6f);
                maxX = Mathf.Max(maxX, -shift);
                z += rowDepth + RowGap;
            }

            // Floor
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.SetParent(root, false);
            float width = Mathf.Max(maxX - minX, 10f) + 6f, depth = z + 6f;
            floor.transform.position = new Vector3((minX + maxX) / 2f, 0f, z / 2f - 1f);
            floor.transform.localScale = new Vector3(width / 10f, 1f, depth / 10f);   // Plane is 10 x 10 m
            var floorMat = AssetDatabase.LoadAssetAtPath<Material>($"{ModelMaterialSetup.MaterialsFolder}/FV_Greeble_Dark.mat");
            if (floorMat) floor.GetComponent<Renderer>().sharedMaterial = floorMat;

            // Background: station high and far, Earth below the horizon
            var bg = new GameObject("Background").transform; bg.SetParent(root, false);
            var station = Spawn("FV_Station_Exterior", bg);
            if (station) { station.transform.position = new Vector3(-25f, 30f, z + 90f); station.transform.rotation = Quaternion.Euler(-30f, 20f, 10f); }
            else missing.Add("FV_Station_Exterior");
            var earth = Spawn("FV_Earth_Backdrop", bg);
            if (earth) earth.transform.position = new Vector3(0f, -20f, z + 140f);
            else missing.Add("FV_Earth_Backdrop");

            // Camera + light
            var cam = Camera.main;
            if (cam)
            {
                cam.transform.position = new Vector3(0f, 8f, -11f);
                cam.transform.LookAt(new Vector3(0f, 1f, z * 0.35f));
                cam.farClipPlane = 500f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.01f, 0.012f, 0.02f);
            }
            var sun = Object.FindAnyObjectByType<Light>();
            if (sun) { sun.transform.rotation = Quaternion.Euler(45f, -30f, 0f); sun.intensity = 1.2f; }
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.35f, 0.40f, 0.50f);
            RenderSettings.ambientEquatorColor = new Color(0.20f, 0.22f, 0.26f);
            RenderSettings.ambientGroundColor = new Color(0.08f, 0.08f, 0.10f);

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);

            var sv = SceneView.lastActiveSceneView;
            if (sv) sv.Frame(new Bounds(new Vector3(0f, 1.5f, z / 2f), new Vector3(width, 4f, depth)), false);

            Debug.Log(missing.Count == 0
                ? $"FactoryVerse: model showcase saved to {ScenePath}."
                : $"FactoryVerse: model showcase saved to {ScenePath}. Missing models: {string.Join(", ", missing)}");
        }

        static GameObject Spawn(string model, Transform parent)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelMaterialSetup.ModelsFolder}/{model}.fbx");
            if (!asset) return null;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            go.transform.localPosition = Vector3.zero;
            return go;
        }

        static Bounds BoundsOf(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one);
            var b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return b;
        }

        static void Label(string text, Vector3 pos, Transform parent, float size, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            var go = new GameObject("Label " + text);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(55f, 0f, 0f);   // tilted up off the floor toward the camera
            var tm = go.AddComponent<TextMesh>();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tm.font = font;
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            tm.text = text.Replace("FV_", "");
            tm.anchor = anchor;
            tm.alignment = TextAlignment.Center;
            tm.fontSize = 100;
            tm.characterSize = size / 10f;   // ~'size' metres tall at fontSize 100
            tm.color = new Color(0.75f, 0.85f, 1f);
        }
    }
}
#endif
