#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FactoryVerse
{
    /// <summary>
    /// Builds Assets/FactoryVerse/Scenes/FactoryInterior.unity — a blockout of the FV-01 orbital factory from the
    /// "Factory Interior – Environment Reference" board, assembled from the Blender models in Art/Models plus simple
    /// room shells (floor, walls with openings, ceiling, pillars, light strips).
    ///
    /// Layout (metres; X = east, Z = north, Y = up; origin = centre of the main bay floor):
    ///   A  Main assembly bay      28 x 18 x 8   x -14..14,  z  -9..9
    ///   F  Assembly workstation   inside A, west half
    ///   G  Control / emergency    alcove north of A,  x -4..4,   z 9..12
    ///   H  Storage area           east of A,          x 14..20,  z -5..5
    ///   B  Secondary work area    south of A,         x -6..6,   z -17..-9
    ///   E  Observation deck       south of B,         x -9.5..9.5, z -23..-17 (windows face Earth to the south)
    ///   C  Maintenance corridor   west of A, 24 m long, built from the corridor kit (x -38..-14)
    ///   D  Escape pod bay         end of C,           x -50..-38, z -5..5
    /// Lighting: Normal / Emergency / Zero-G presets on the "Lighting" object (keys 1/2/3 in Play mode).
    /// Run: FactoryVerse > Build Factory Interior Scene (safe to re-run; regenerates the scene).
    /// </summary>
    public static class FactoryInteriorBuilder
    {
        const string ScenePath = "Assets/FactoryVerse/Scenes/FactoryInterior.unity";
        const float T = 0.3f;    // wall thickness

        static Transform _root;
        static readonly Dictionary<string, Material> _mats = new Dictionary<string, Material>();
        static readonly List<string> _missing = new List<string>();

        struct Opening
        {
            public char Side; public float Center, Width, Height;
            public Opening(char side, float center, float width, float height) { Side = side; Center = center; Width = width; Height = height; }
        }

        [MenuItem("FactoryVerse/Build Factory Interior Scene")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            ModelMaterialSetup.Setup();
            _mats.Clear(); _missing.Clear(); _demoRobot = null; _demoCell = null;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            _root = new GameObject("FV-01 Orbital Factory").transform;

            BuildRooms();
            BuildCorridor();
            FurnishAssemblyBay();
            FillOutAssemblyBay();
            FurnishControl();
            FurnishSecondaryArea();
            FurnishObservationDeck();
            FurnishStorage();
            FurnishPodBay();
            FloatingDebris();
            PlaceCharacters();
            BuildLightProbes();
            BuildLighting();
            BuildCameras();
            BuildPlayer();

            // Voice dialogue + subtitles (Mission 01 intro on start; lighting presets trigger the emergency beats)
            var dialogue = new GameObject("Dialogue");
            dialogue.transform.SetParent(_root, false);
            dialogue.AddComponent<DialogueManager>();
            // FactoryDialogueDemo is not added: FactoryDemoDirector already drives the story beats and its own subtitles;
            // the two would talk over each other. Voiced lines will be routed through DialogueManager from the director.
            BuildDemoDirector();

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            var sv = SceneView.lastActiveSceneView;
            if (sv) sv.Frame(new Bounds(new Vector3(-15f, 4f, -5f), new Vector3(75f, 10f, 40f)), false);
            Debug.Log(_missing.Count == 0
                ? $"FactoryVerse: factory interior saved to {ScenePath}."
                : $"FactoryVerse: factory interior saved to {ScenePath}. Missing models: {string.Join(", ", _missing)}");
        }

        // ------------------------------------------------------------------ rooms
        static void BuildRooms()
        {
            var rooms = Group("Rooms", _root);
            Room("A Main Assembly Bay", rooms, -14, -9, 14, 9, 8f, "",
                new Opening('W', 0f, 4f, 4f),     // to corridor C
                new Opening('S', 0f, 12f, 5f),    // to B
                new Opening('N', 0f, 8f, 5f),     // to G
                new Opening('E', 0f, 10f, 4f));   // to H
            Room("G Control Alcove", rooms, -4, 9, 4, 12, 5f, "S");
            Room("H Storage Area", rooms, 14, -5, 20, 5, 4f, "W");
            Room("B Secondary Work Area", rooms, -6, -17, 6, -9, 5f, "NS");
            Room("E Observation Deck", rooms, -9.5f, -23, 9.5f, -17, 5f, "S",
                new Opening('N', 0f, 12f, 5f));   // open to B
            ObservationWindowWall(rooms);
            Room("D Escape Pod Bay", rooms, -50, -5, -38, 5, 6f, "",
                new Opening('E', 0f, 4f, 4f));    // from corridor C

            // Floor markings + signage
            var marks = Group("Markings", _root);
            HazardFrame(marks, -12.5f, -7.5f, 12.5f, 7.5f);
            HazardFrame(marks, -43.5f, -2.5f, -39.0f, 2.5f);
            Text(marks, "ASSEMBLY BAY A", new Vector3(0f, 0.02f, -6.2f), new Vector3(90, 0, 0), 1.1f, new Color(0.75f, 0.8f, 0.85f, 0.8f));
            Text(marks, "FV-01\nORBITAL FACTORY", new Vector3(-13.8f, 5.6f, 5.6f), new Vector3(0, -90, 0), 1.0f, new Color(0.85f, 0.88f, 0.92f));
            Text(marks, "SECONDARY WORK AREA B", new Vector3(0f, 0.02f, -10.2f), new Vector3(90, 0, 0), 0.55f, new Color(0.75f, 0.8f, 0.85f, 0.8f));
            Text(marks, "OBSERVATION DECK E", new Vector3(0f, 0.02f, -17.8f), new Vector3(90, 0, 0), 0.55f, new Color(0.75f, 0.8f, 0.85f, 0.8f));
            Text(marks, "STORAGE H", new Vector3(15.2f, 0.02f, 0f), new Vector3(90, 90, 0), 0.55f, new Color(0.75f, 0.8f, 0.85f, 0.8f));
            Text(marks, "ESCAPE POD BAY D", new Vector3(-39.2f, 0.02f, -3.6f), new Vector3(90, -90, 0), 0.55f, new Color(1f, 0.6f, 0.2f, 0.9f));
            Text(marks, "MAINTENANCE C", new Vector3(-13.8f, 4.6f, 0f), new Vector3(0, -90, 0), 0.4f, new Color(0.85f, 0.88f, 0.92f));
            Text(marks, "CONTROL G", new Vector3(0f, 5.45f, 8.8f), new Vector3(0, 0, 0), 0.45f, new Color(1f, 0.35f, 0.3f));
        }

        static void Room(string name, Transform parent, float x0, float z0, float x1, float z1, float h, string openSides, params Opening[] openings)
        {
            var g = Group(name, parent);
            float w = x1 - x0, d = z1 - z0, cx = (x0 + x1) / 2f, cz = (z0 + z1) / 2f;
            Box("Floor", g, new Vector3(cx, -0.1f, cz), new Vector3(w + 2 * T, 0.2f, d + 2 * T), "FV_Greeble_Dark");
            Box("Ceiling", g, new Vector3(cx, h + 0.1f, cz), new Vector3(w + 2 * T, 0.2f, d + 2 * T), "FV_Greeble_Dark");
            // floor plates (lighter metal tiles with gaps), every 2 m
            var plates = Group("FloorPlates", g);
            for (float x = x0 + 1f; x < x1; x += 2f)
                for (float z = z0 + 1f; z < z1; z += 2f)
                    Box("Plate", plates, new Vector3(x, 0.005f, z), new Vector3(1.9f, 0.01f, 1.9f), "FV_Hull_Grey", collider: false);
            // ceiling light bars (along X, every 4 m) and beams (along Z, every 4 m)
            for (float z = z0 + 2f; z < z1 - 0.5f; z += 4f)
                Box("CeilingLight", g, new Vector3(cx, h - 0.03f, z), new Vector3(Mathf.Max(w - 2.5f, 1f), 0.05f, 0.35f), "FV_Emissive_White", collider: false);
            for (float x = x0 + 4f; x < x1 - 0.5f; x += 4f)
                Box("CeilingBeam", g, new Vector3(x, h - 0.3f, cz), new Vector3(0.4f, 0.5f, d), "FV_Greeble_Dark", collider: false);

            foreach (char side in "NSEW")
            {
                if (openSides.IndexOf(side) >= 0) continue;
                var ops = new List<Opening>();
                foreach (var o in openings) if (o.Side == side) ops.Add(o);
                Wall(g, side, x0, z0, x1, z1, h, ops);
            }
        }

        static void Wall(Transform parent, char side, float x0, float z0, float x1, float z1, float h, List<Opening> ops)
        {
            var g = Group("Wall " + side, parent);
            bool alongX = side == 'N' || side == 'S';
            float a0 = alongX ? x0 - T : z0 - T, a1 = alongX ? x1 + T : z1 + T;
            float fixedC = side == 'N' ? z1 + T / 2 : side == 'S' ? z0 - T / 2 : side == 'E' ? x1 + T / 2 : x0 - T / 2;
            float inward = (side == 'N' || side == 'E') ? -1f : 1f;   // direction into the room along the wall normal

            Vector3 P(float along, float y, float off) => alongX ? new Vector3(along, y, fixedC + inward * off) : new Vector3(fixedC + inward * off, y, along);
            Vector3 S(float len, float hgt, float thick) => alongX ? new Vector3(len, hgt, thick) : new Vector3(thick, hgt, len);

            // solid spans = wall minus openings
            var spans = new List<Vector2> { new Vector2(a0, a1) };
            foreach (var o in ops)
            {
                float c0 = o.Center - o.Width / 2, c1 = o.Center + o.Width / 2;
                var next = new List<Vector2>();
                foreach (var s in spans)
                {
                    if (c1 <= s.x || c0 >= s.y) { next.Add(s); continue; }
                    if (c0 > s.x) next.Add(new Vector2(s.x, c0));
                    if (c1 < s.y) next.Add(new Vector2(c1, s.y));
                }
                spans = next;
                if (o.Height < h)   // lintel above the opening
                    Box("Lintel", g, P(o.Center, (o.Height + h) / 2, 0), S(o.Width, h - o.Height, T), "FV_Hull_Grey");
                // door frame trim
                Box("DoorTrim", g, P(o.Center, o.Height + 0.1f, T / 2 + 0.05f), S(o.Width + 0.4f, 0.2f, 0.1f), "FV_Industrial_Yellow", collider: false);
            }

            float strip = Mathf.Min(2.6f, h - 1.2f);
            foreach (var s in spans)
            {
                float len = s.y - s.x, mid = (s.x + s.y) / 2;
                if (len < 0.05f) continue;
                Box("Panel", g, P(mid, h / 2, 0), S(len, h, T), "FV_Hull_Grey");
                Box("Kick", g, P(mid, 0.15f, T / 2 + 0.05f), S(len, 0.3f, 0.1f), "FV_Greeble_Dark", collider: false);
                if (len > 0.8f)
                    Box("LightStrip", g, P(mid, strip, T / 2 + 0.03f), S(len - 0.4f, 0.06f, 0.05f), "FV_Emissive_Blue", collider: false);
                if (h >= 5f && len > 0.8f)
                    Box("UpperBand", g, P(mid, h - 1.2f, T / 2 + 0.04f), S(len, 0.25f, 0.08f), "FV_Greeble_Dark", collider: false);
                // pillars every 4 m inside this span
                for (float p = Mathf.Ceil((s.x + 0.6f) / 4f) * 4f; p < s.y - 0.6f; p += 4f)
                {
                    Box("Pillar", g, P(p, h / 2, T / 2 + 0.17f), S(0.5f, h, 0.35f), "FV_Greeble_Dark");
                    Box("PillarLight", g, P(p, strip + 0.9f, T / 2 + 0.36f), S(0.12f, 0.8f, 0.04f), "FV_Emissive_Red", collider: false);
                }
            }
        }

        static void ObservationWindowWall(Transform rooms)
        {
            var g = Group("E South Wall (Windows)", rooms);
            float z = -23f - T / 2;
            Box("Below", g, new Vector3(0f, 0.5f, z), new Vector3(19f + 2 * T, 1f, T), "FV_Hull_Grey");
            Box("Above", g, new Vector3(0f, 4.5f, z), new Vector3(19f + 2 * T, 1f, T), "FV_Hull_Grey");
            foreach (float sx in new[] { -1f, 1f })
                Box("SideFill", g, new Vector3(sx * 9.4f, 2.5f, z), new Vector3(0.8f, 3f, T), "FV_Hull_Grey");
            foreach (float x in new[] { -6.12f, 0f, 6.12f })
                Model("FV_ObservationWindow", g, new Vector3(x, 1f, -23f), 0f);
            // fill the triangular gaps left by the frames' chamfered corners where windows meet the wall
            foreach (float x in new[] { -9.18f, -3.06f, 3.06f, 9.18f })
                foreach (float y in new[] { 1.32f, 3.68f })
                    Box("CornerFill", g, new Vector3(x, y, z), new Vector3(1.3f, 0.66f, T), "FV_Hull_Grey");
        }

        // ------------------------------------------------------------------ corridor C (kit pieces)
        static void BuildCorridor()
        {
            // Kit pieces span local z -2..0 (wall piece on -X). Build along local +Z, then turn the corridor to run west.
            var c = Group("C Maintenance Corridor", _root);
            c.position = new Vector3(-14f, 0f, 0f);
            c.rotation = Quaternion.Euler(0f, -90f, 0f);
            c.localScale = new Vector3(1f, 4f / 3f, 1f);     // kit is 3 m tall; reference corridor is 4 m
            for (int i = 0; i < 12; i++)
            {
                var seg = Group($"Segment {i:00}", c);
                seg.localPosition = new Vector3(0f, 0f, 2f * (i + 1));
                Model("FV_Corridor_Floor_2m", seg, Vector3.zero, 0f, local: true);
                Model("FV_Corridor_Ceiling_2m", seg, Vector3.zero, 0f, local: true);
                Model("FV_Corridor_Wall_2m", seg, Vector3.zero, 0f, local: true);
                Model("FV_Corridor_Wall_2m", seg, new Vector3(0f, 0f, -2f), 180f, local: true);  // mirrored wall
            }
            for (int k = 0; k <= 6; k++)
                Model("FV_Corridor_Frame", c, new Vector3(0f, 0f, 4f * k), 0f, local: true);
        }

        // ------------------------------------------------------------------ furnishing
        static void FurnishAssemblyBay()
        {
            var g = Group("A Contents", _root);
            // F — robot assembly workstation (west), operator side faces south
            var f = Group("F Assembly Workstation", g);
            Model("FV_AssemblyWorkstation", f, new Vector3(-8f, 0f, 1f), 180f);
            // the demo's install target: this R-7 and the loose power cell move at runtime, so they are not static
            _demoRobot = Model("FV_UnitR7", f, new Vector3(-8f, 0f, 2.4f), 180f);
            _demoCell = Model("FV_PowerCell", f, new Vector3(-8.9f, 0.89f, 0.85f), 0f);
            MakeDynamic(_demoRobot); MakeDynamic(_demoCell);
            Model("FV_HeadUnit", f, new Vector3(-7.1f, 0.89f, 0.8f), 200f);
            foreach (float z in new[] { -0.6f, 0.65f, 1.9f, 3.15f })   // safety railing on the west side of the cell
                Model("FV_Prop_Barrier", f, new Vector3(-11.7f, 0f, z), 90f);

            // Main assembly line (centre) with ARIA hologram
            var m = Group("Main Assembly Line", g);
            Model("FV_AssemblyWorkstation", m, new Vector3(3f, 0f, 1f), 180f);
            Model("FV_PowerCore", m, new Vector3(2.2f, 0.89f, 0.85f), 0f);
            Model("FV_ArmModule_L", m, new Vector3(3.6f, 0.95f, 0.75f), 180f);
            Model("FV_ARIA_HoloDisplay", m, new Vector3(3f, 0f, 2.6f), 180f);
            Model("FV_UnitR7", m, new Vector3(8.5f, 0f, 3.5f), 215f);

            // Perimeter props (crate stacks, canisters, boxes)
            var p = Group("Perimeter Props", g);
            CrateStack(p, new Vector3(12.2f, 0f, 7.6f), 2);
            CrateStack(p, new Vector3(11.2f, 0f, 7.6f), 1);
            CrateStack(p, new Vector3(-12.2f, 0f, -7.4f), 2);
            Model("FV_Prop_StorageBox", p, new Vector3(-12.3f, 0f, 7.6f), 90f);
            Model("FV_Prop_StorageBox", p, new Vector3(-12.3f, 0.415f, 7.6f), 80f);
            foreach (var v in new[] { new Vector3(12.6f, 0f, -7.6f), new Vector3(12.1f, 0f, -7.9f), new Vector3(12.7f, 0f, -7.0f) })
                Model("FV_Prop_Canister", p, v, Random.Range(0f, 360f));
            Model("FV_Tool_Crate", p, new Vector3(-10.8f, 0.18f, -7.6f), 10f);
            Model("FV_Prop_Crate", p, new Vector3(6.5f, 0f, -7.5f), 0f);
        }

        // Denser main bay, after the reference board's primary view: industrial robot arms at both cells,
        // an assembly conveyor, overhead gantry crane, holo screens, wall monitors and crate rows.
        static void FillOutAssemblyBay()
        {
            var g = Group("A Detail", _root);
            Random.InitState(42);

            // Industrial robot arms (yellow), two per work cell, reaching in toward the table
            var arms = Group("Robot Arms", g);
            IndustrialArm(arms, new Vector3(-10.3f, 0f, 1.9f), 0f, 1.25f);     // F cell, west side, reaching east
            IndustrialArm(arms, new Vector3(-5.7f, 0f, 1.9f), 180f, 1.25f);    // F cell, east side, reaching west
            IndustrialArm(arms, new Vector3(0.7f, 0f, 1.9f), 0f, 1.35f);       // main cell
            IndustrialArm(arms, new Vector3(5.3f, 0f, 1.9f), 180f, 1.35f);
            HazardFrame(g, -11.3f, -1.4f, -4.7f, 3.8f);
            HazardFrame(g, -0.3f, -1.4f, 6.3f, 3.8f);

            // Holo screens above both workstations (robot schematic)
            HoloScreen(g, new Vector3(3f, 3.9f, 3.1f), 3.2f, 1.8f);
            HoloScreen(g, new Vector3(-8f, 3.5f, 3.3f), 2.4f, 1.4f);

            // Assembly conveyor along the back of the cells, carrying robot parts; split in the middle so the
            // walkway to the control alcove (G) stays open
            var conv = Group("Assembly Conveyor", g);
            float cz = 6.2f;
            foreach (var (cx0, cx1) in new[] { (-11f, -1.8f), (1.8f, 11f) })
            {
                float len = cx1 - cx0, mid = (cx0 + cx1) / 2f;
                Box("Belt", conv, new Vector3(mid, 0.85f, cz), new Vector3(len, 0.12f, 0.9f), "FV_Greeble_Dark");
                Box("Frame", conv, new Vector3(mid, 0.72f, cz), new Vector3(len, 0.14f, 1.0f), "FV_Hull_Grey");
                for (float x = cx0 + 0.25f; x < cx1; x += 0.5f)
                    Box("Roller", conv, new Vector3(x, 0.915f, cz), new Vector3(0.06f, 0.02f, 0.84f), "FV_Metal_Clean", collider: false);
                for (float x = cx0 + 0.3f; x < cx1; x += 2.2f)
                    foreach (float sz in new[] { -0.42f, 0.42f })
                        Box("Leg", conv, new Vector3(x, 0.33f, cz + sz), new Vector3(0.1f, 0.66f, 0.1f), "FV_Industrial_Yellow");
                Box("GuideLightN", conv, new Vector3(mid, 0.8f, cz + 0.51f), new Vector3(len, 0.04f, 0.02f), "FV_Emissive_Blue", collider: false);
                Box("GuideLightS", conv, new Vector3(mid, 0.8f, cz - 0.51f), new Vector3(len, 0.04f, 0.02f), "FV_Emissive_Blue", collider: false);
                Box("EndCap", conv, new Vector3(cx0 < 0 ? cx1 : cx0, 0.78f, cz), new Vector3(0.12f, 0.3f, 1.05f), "FV_Industrial_Yellow");
            }
            string[] parts = { "FV_PowerCore", "FV_HeadUnit", "FV_PowerCell", "FV_PowerCore", "FV_HeadUnit", "FV_PowerCell" };
            float[] partX = { -10f, -8.4f, -3.2f, 3.2f, 6.2f, 10f };
            for (int i = 0; i < parts.Length; i++)
                Model(parts[i], conv, new Vector3(partX[i], 0.91f, cz), Random.Range(-20f, 20f));
            Model("FV_ArmModule_L", conv, new Vector3(-4.6f, 1.0f, cz), 180f);
            Model("FV_ArmModule_R", conv, new Vector3(7.4f, 1.0f, cz), 0f);
            // scanner arch over the west belt
            Box("ScannerL", conv, new Vector3(-6.5f, 1.25f, cz - 0.6f), new Vector3(0.15f, 1.1f, 0.15f), "FV_Greeble_Dark");
            Box("ScannerR", conv, new Vector3(-6.5f, 1.25f, cz + 0.6f), new Vector3(0.15f, 1.1f, 0.15f), "FV_Greeble_Dark");
            Box("ScannerTop", conv, new Vector3(-6.5f, 1.85f, cz), new Vector3(0.3f, 0.15f, 1.35f), "FV_Greeble_Dark");
            Box("ScannerBeam", conv, new Vector3(-6.5f, 1.35f, cz), new Vector3(0.02f, 0.9f, 1.05f), "FV_Hologram", collider: false);

            // Overhead gantry crane
            var gantry = Group("Gantry Crane", g);
            foreach (float z in new[] { -7.5f, 7.5f })
                Box("Rail", gantry, new Vector3(0f, 6.6f, z), new Vector3(27.4f, 0.4f, 0.35f), "FV_Industrial_Yellow");
            Box("Bridge", gantry, new Vector3(-2f, 6.25f, 0f), new Vector3(0.5f, 0.5f, 15.4f), "FV_Industrial_Yellow");
            Box("Trolley", gantry, new Vector3(-2f, 5.85f, 1.5f), new Vector3(0.9f, 0.45f, 0.9f), "FV_Greeble_Dark");
            Box("TrolleyLight", gantry, new Vector3(-2f, 5.6f, 1.5f), new Vector3(0.5f, 0.04f, 0.5f), "FV_Emissive_Orange", collider: false);
            Box("Cable", gantry, new Vector3(-2f, 4.6f, 1.5f), new Vector3(0.04f, 2.1f, 0.04f), "FV_Metal_Clean", collider: false);
            Box("Hook", gantry, new Vector3(-2f, 3.5f, 1.5f), new Vector3(0.3f, 0.15f, 0.15f), "FV_Industrial_Yellow");
            Model("FV_Prop_Crate", gantry, new Vector3(-2f, 2.82f, 1.5f), 15f);   // crate hanging from the hook

            // Wall monitors between the pillars (north wall faces south, east faces west, west faces east)
            var screens = Group("Wall Monitors", g);
            foreach (float x in new[] { -10.5f, -6f, 6f, 10.5f })
                WallScreen(screens, new Vector3(x, 3.4f, 8.9f), 0f, 2.2f, 1.2f);
            foreach (float z in new[] { -6.6f, 6.6f })
                WallScreen(screens, new Vector3(13.9f, 3.4f, z), 90f, 1.6f, 1.0f);
            WallScreen(screens, new Vector3(-13.9f, 3.0f, -6.1f), -90f, 2.6f, 1.4f);

            // Crate rows along the walls
            var crates = Group("Crate Rows", g);
            for (float x = -12.6f; x <= -8.5f; x += 0.95f)
                CrateStack(crates, new Vector3(x, 0f, 8.3f), Random.Range(1, 4));
            for (float x = 8.6f; x <= 12.6f; x += 0.95f)
                CrateStack(crates, new Vector3(x, 0f, 8.3f), Random.Range(1, 3));
            for (float x = 7.8f; x <= 11.6f; x += 0.85f)
                Model("FV_Prop_StorageBox", crates, new Vector3(x, 0f, -8.35f), 90f + Random.Range(-6f, 6f));
            for (float x = 7.8f; x <= 11.6f; x += 1.7f)
                Model("FV_Tool_Crate", crates, new Vector3(x, 0.415f + 0.18f, -8.35f), Random.Range(-15f, 15f));
            for (float z = -6.6f; z <= -3.2f; z += 0.95f)
                CrateStack(crates, new Vector3(-12.9f, 0f, z), Random.Range(1, 3));

            // Power-cell charging rack (east side)
            var rack = Group("Power Cell Rack", g);
            Box("RackBase", rack, new Vector3(12.9f, 0.3f, 6.0f), new Vector3(0.9f, 0.6f, 2.6f), "FV_Greeble_Dark");
            Box("RackGlow", rack, new Vector3(12.43f, 0.45f, 6.0f), new Vector3(0.02f, 0.05f, 2.4f), "FV_Emissive_Blue", collider: false);
            for (int i = 0; i < 4; i++)
                Model("FV_PowerCell", rack, new Vector3(12.9f, 0.6f, 5.05f + i * 0.63f), 0f);

            // Walkway from B into the bay, plus a railing at the west end of the conveyor
            Box("WalkwayL", g, new Vector3(-6.2f, 0.012f, -5.2f), new Vector3(0.15f, 0.01f, 7.6f), "FV_Industrial_Yellow", collider: false);
            Box("WalkwayR", g, new Vector3(6.2f, 0.012f, -5.2f), new Vector3(0.15f, 0.01f, 7.6f), "FV_Industrial_Yellow", collider: false);
            Model("FV_Prop_Barrier", g, new Vector3(-12.2f, 0f, cz), 90f);
        }

        static void IndustrialArm(Transform parent, Vector3 pos, float yaw, float scale)
        {
            // Built in local space reaching toward local +X; parent group carries position/yaw/scale.
            var a = Group("Industrial Arm", parent);
            a.position = pos; a.rotation = Quaternion.Euler(0f, yaw, 0f); a.localScale = Vector3.one * scale;
            LCyl(a, "Base", new Vector3(0f, 0.15f, 0f), 0.45f, 0.3f, "FV_Industrial_Yellow", Vector3.zero);
            LCyl(a, "Turret", new Vector3(0f, 0.55f, 0f), 0.32f, 0.5f, "FV_Greeble_Dark", Vector3.zero);
            LBox(a, "StatusLight", new Vector3(-0.33f, 0.6f, 0f), new Vector3(0.02f, 0.1f, 0.2f), "FV_Emissive_Blue", Vector3.zero);
            Vector3 sh = new Vector3(0f, 0.95f, 0f), el = new Vector3(0.5f, 2.35f, 0f), wr = new Vector3(1.45f, 1.55f, 0f);
            LCyl(a, "Shoulder", sh, 0.25f, 0.55f, "FV_Greeble_Dark", new Vector3(90f, 0f, 0f));
            LSeg(a, "LowerArm", sh, el, 0.32f, "FV_Industrial_Yellow");
            LCyl(a, "Elbow", el, 0.2f, 0.45f, "FV_Greeble_Dark", new Vector3(90f, 0f, 0f));
            LCyl(a, "ElbowGlow", el, 0.11f, 0.47f, "FV_Emissive_Blue", new Vector3(90f, 0f, 0f));
            LSeg(a, "UpperArm", el, wr, 0.24f, "FV_Industrial_Yellow");
            LSeg(a, "Piston", new Vector3(0.12f, 1.05f, 0.2f), new Vector3(0.42f, 2.05f, 0.2f), 0.06f, "FV_Metal_Clean");
            LCyl(a, "Wrist", wr, 0.13f, 0.3f, "FV_Greeble_Dark", new Vector3(90f, 0f, 0f));
            LBox(a, "GripBase", wr + new Vector3(0.05f, -0.2f, 0f), new Vector3(0.18f, 0.12f, 0.22f), "FV_Metal_Clean", Vector3.zero);
            LBox(a, "FingerA", wr + new Vector3(0.05f, -0.38f, 0.07f), new Vector3(0.05f, 0.24f, 0.04f), "FV_Greeble_Dark", Vector3.zero);
            LBox(a, "FingerB", wr + new Vector3(0.05f, -0.38f, -0.07f), new Vector3(0.05f, 0.24f, 0.04f), "FV_Greeble_Dark", Vector3.zero);
        }

        static void HoloScreen(Transform parent, Vector3 center, float w, float h)
        {
            var s = Group("Holo Screen", parent);
            Box("Panel", s, center, new Vector3(w, h, 0.02f), "FV_Hologram", collider: false);
            Box("Top", s, center + new Vector3(0f, h / 2, 0f), new Vector3(w, 0.04f, 0.03f), "FV_Emissive_Blue", collider: false);
            Box("Bottom", s, center - new Vector3(0f, h / 2, 0f), new Vector3(w, 0.04f, 0.03f), "FV_Emissive_Blue", collider: false);
            Box("Left", s, center - new Vector3(w / 2, 0f, 0f), new Vector3(0.04f, h, 0.03f), "FV_Emissive_Blue", collider: false);
            Box("Right", s, center + new Vector3(w / 2, 0f, 0f), new Vector3(0.04f, h, 0.03f), "FV_Emissive_Blue", collider: false);
            // robot schematic, scaled to the screen height
            float u = h / 1.8f; Vector3 c = center + new Vector3(0f, 0f, -0.02f);
            Box("Head", s, c + new Vector3(0f, 0.55f, 0f) * u, new Vector3(0.22f, 0.22f, 0.01f) * u, "FV_Emissive_White", collider: false);
            Box("Torso", s, c + new Vector3(0f, 0.15f, 0f) * u, new Vector3(0.42f, 0.5f, 0.01f) * u, "FV_Emissive_White", collider: false);
            Box("ArmL", s, c + new Vector3(-0.32f, 0.12f, 0f) * u, new Vector3(0.1f, 0.5f, 0.01f) * u, "FV_Emissive_White", collider: false);
            Box("ArmR", s, c + new Vector3(0.32f, 0.12f, 0f) * u, new Vector3(0.1f, 0.5f, 0.01f) * u, "FV_Emissive_White", collider: false);
            Box("LegL", s, c + new Vector3(-0.11f, -0.4f, 0f) * u, new Vector3(0.12f, 0.55f, 0.01f) * u, "FV_Emissive_White", collider: false);
            Box("LegR", s, c + new Vector3(0.11f, -0.4f, 0f) * u, new Vector3(0.12f, 0.55f, 0.01f) * u, "FV_Emissive_White", collider: false);
            for (int i = 0; i < 4; i++)
                Box("DataBar", s, c + new Vector3(w * 0.3f / u, 0.4f - i * 0.15f, 0f) * u, new Vector3(0.5f * (1f - i * 0.15f), 0.05f, 0.01f) * u, "FV_Emissive_Blue", collider: false);
            // support poles down to the floor
            Box("Pole", s, new Vector3(center.x, (center.y - h / 2) / 2, center.z + 0.05f), new Vector3(0.08f, center.y - h / 2, 0.08f), "FV_Greeble_Dark");
        }

        static void WallScreen(Transform parent, Vector3 center, float yaw, float w, float h)
        {
            var s = Group("Wall Monitor", parent);
            s.position = center; s.rotation = Quaternion.Euler(0f, yaw, 0f);
            LBox(s, "Bezel", Vector3.zero, new Vector3(w + 0.16f, h + 0.16f, 0.1f), "FV_Greeble_Dark", Vector3.zero);
            LBox(s, "Screen", new Vector3(0f, 0f, -0.055f), new Vector3(w, h, 0.01f), "FV_Hologram", Vector3.zero);
            for (int i = 0; i < 3; i++)
                LBox(s, "Line", new Vector3(-w * 0.15f, h * 0.25f - i * h * 0.2f, -0.065f), new Vector3(w * 0.55f * (1f - i * 0.2f), 0.05f, 0.01f), "FV_Emissive_White", Vector3.zero);
            LBox(s, "Graph", new Vector3(w * 0.3f, -h * 0.15f, -0.065f), new Vector3(w * 0.25f, h * 0.4f, 0.01f), "FV_Emissive_Blue", Vector3.zero);
        }

        // local-space primitives for grouped assemblies (parent may be rotated/scaled)
        static GameObject LBox(Transform parent, string name, Vector3 localPos, Vector3 size, string mat, Vector3 euler)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos; go.transform.localRotation = Quaternion.Euler(euler); go.transform.localScale = size;
            var m = Mat(mat); if (m) go.GetComponent<Renderer>().sharedMaterial = m;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            MarkStatic(go, mat);
            return go;
        }

        static GameObject LCyl(Transform parent, string name, Vector3 localPos, float radius, float height, string mat, Vector3 euler)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);   // unit cylinder: 1 wide, 2 tall
            go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos; go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = new Vector3(radius * 2f, height / 2f, radius * 2f);
            var m = Mat(mat); if (m) go.GetComponent<Renderer>().sharedMaterial = m;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            MarkStatic(go, mat);
            return go;
        }

        // square beam from a to b in the parent's local XY plane
        static void LSeg(Transform parent, string name, Vector3 a, Vector3 b, float thick, string mat)
        {
            Vector3 d = b - a;
            float angle = -Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg;   // rotate local Y onto d (about Z)
            LBox(parent, name, (a + b) / 2f, new Vector3(thick, d.magnitude, thick), mat, new Vector3(0f, 0f, angle));
        }

        static void FurnishControl()
        {
            var g = Group("G Control / Emergency", _root);
            foreach (float x in new[] { -2.2f, 0f, 2.2f })
                Model("FV_ControlConsole", g, new Vector3(x, 0f, 11.35f), 180f);
        }

        static void FurnishSecondaryArea()
        {
            var g = Group("B Contents", _root);
            Model("FV_Prop_StorageBox", g, new Vector3(-3f, 0f, -13f), 0f);
            Model("FV_ArmModule_R", g, new Vector3(-3f, 0.5f, -13f), 180f);
            Model("FV_Prop_StorageBox", g, new Vector3(-1.9f, 0f, -13f), 0f);
            Model("FV_Tool_Tablet", g, new Vector3(-1.9f, 0.43f, -13f), 0f);
            Model("FV_UnitR7", g, new Vector3(3f, 0f, -14f), 160f);
            Model("FV_PowerCell", g, new Vector3(4.5f, 0f, -15.8f), 0f);
            Model("FV_PowerCell", g, new Vector3(5.1f, 0f, -15.8f), 0f);
            CrateStack(g, new Vector3(-5.2f, 0f, -16.3f), 2);
        }

        static void FurnishObservationDeck()
        {
            var g = Group("E Contents", _root);
            foreach (float x in new[] { -4.5f, -3.3f, 3.3f, 4.5f })
                Model("FV_ControlConsole", g, new Vector3(x, 0f, -21.3f), 0f);   // operators face south, toward the windows
            // Earth + station far outside, seen through the windows
            var sky = Group("Space Backdrop", _root);
            var earth = Model("FV_Earth_Backdrop", sky, new Vector3(0f, -85f, -150f), 0f);
            if (earth) earth.transform.localScale = Vector3.one * 3f;
            var st = Model("FV_Station_Exterior", sky, new Vector3(-45f, 22f, -170f), 0f);
            if (st) st.transform.rotation = Quaternion.Euler(-25f, 30f, 12f);
        }

        static void FurnishStorage()
        {
            var g = Group("H Contents", _root);
            foreach (float z in new[] { -3.3f, 0f, 3.3f })
                Rack(g, new Vector3(19.3f, 0f, z));
            CrateStack(g, new Vector3(16f, 0f, 4.2f), 2);
            CrateStack(g, new Vector3(16.9f, 0f, 4.2f), 1);
            Model("FV_Prop_Barrier", g, new Vector3(15.3f, 0f, -4.4f), 0f);
            Model("FV_Prop_Canister", g, new Vector3(16.2f, 0f, -4.3f), 0f);
        }

        static void FurnishPodBay()
        {
            var g = Group("D Contents", _root);
            Model("FV_EscapePod", g, new Vector3(-45.5f, 0f, 0f), 90f);   // door + ramp face the corridor (east)
            foreach (float sz in new[] { -1f, 1f })
                for (int i = 0; i < 3; i++)
                    Model("FV_Prop_Barrier", g, new Vector3(-41.9f + i * 1.25f, 0f, sz * 2.9f), 0f);
            Model("FV_ControlConsole", g, new Vector3(-39.2f, 0f, -4.3f), 0f);
            CrateStack(g, new Vector3(-49.2f, 0f, 4.3f), 2);
            CrateStack(g, new Vector3(-48.3f, 0f, 4.3f), 1);
            Model("FV_Prop_Canister", g, new Vector3(-49.3f, 0f, -4.3f), 0f);
            Model("FV_Prop_Canister", g, new Vector3(-48.8f, 0f, -4.5f), 0f);
        }

        static void FloatingDebris()
        {
            // Zero-G emergency debris drifting in the maintenance corridor and storage area
            var g = Group("Zero-G Debris", _root);
            Random.InitState(11);
            (string m, Vector3 p)[] items =
            {
                ("FV_Tool_Drill",   new Vector3(-19f, 2.4f,  0.8f)),
                ("FV_Tool_Wrench",  new Vector3(-22.5f, 1.6f, -0.6f)),
                ("FV_Tool_Tablet",  new Vector3(-27f, 2.9f,  0.4f)),
                ("FV_Tool_Crate",   new Vector3(-31f, 1.3f, -0.5f)),
                ("FV_Tool_Wrench",  new Vector3(-34f, 3.0f,  0.9f)),
                ("FV_Tool_Drill",   new Vector3(17f, 2.6f, -2.0f)),
                ("FV_Prop_Canister",new Vector3(16.5f, 1.8f, 1.6f)),
            };
            foreach (var (m, p) in items)
            {
                var go = Model(m, g, p, 0f);
                if (go) { go.transform.rotation = Random.rotation; MakeDynamic(go); go.AddComponent<FactoryZeroGDrift>(); }
            }
        }

        static void CrateStack(Transform parent, Vector3 basePos, int height)
        {
            for (int i = 0; i < height; i++)
                Model("FV_Prop_Crate", parent, basePos + new Vector3(0f, 0.6f * i, 0f), i * 8f);
        }

        static void Rack(Transform parent, Vector3 pos)
        {
            // Yellow shelving rack, long axis along Z, against the east wall of H
            var g = Group("Rack", parent);
            g.position = pos;
            float L = 2.8f, D = 0.9f, H = 3.0f;
            foreach (float sx in new[] { -1f, 1f })
                foreach (float sz in new[] { -1f, 1f })
                    Box("Post", g, pos + new Vector3(sx * D / 2, H / 2, sz * L / 2), new Vector3(0.08f, H, 0.08f), "FV_Industrial_Yellow");
            float[] levels = { 0.12f, 1.12f, 2.12f };
            foreach (float y in levels)
                Box("Shelf", g, pos + new Vector3(0f, y, 0f), new Vector3(D, 0.05f, L), "FV_Greeble_Dark");
            Model("FV_Prop_Crate", g, pos + new Vector3(0f, 0.145f, -0.7f), 90f);
            Model("FV_Prop_StorageBox", g, pos + new Vector3(0f, 0.145f, 0.7f), 90f);
            Model("FV_Prop_StorageBox", g, pos + new Vector3(0f, 1.145f, -0.6f), 90f);
            Model("FV_Prop_Canister", g, pos + new Vector3(0f, 1.145f, 0.5f), 0f);
            Model("FV_Prop_Canister", g, pos + new Vector3(0f, 1.145f, 1.0f), 0f);
            Model("FV_Tool_Crate", g, pos + new Vector3(0f, 2.33f, 0f), 90f);
        }

        // ------------------------------------------------------------------ lighting presets
        static void BuildLighting()
        {
            var lighting = new GameObject("Lighting");
            lighting.transform.SetParent(_root, false);
            var normal = Group("Normal (Operations)", lighting.transform);
            var emergency = Group("Emergency (Alarm)", lighting.transform);
            var zeroG = Group("Zero-G (Power Loss)", lighting.transform);

            Color cyan = new Color(0.70f, 0.90f, 1.00f), red = new Color(1f, 0.08f, 0.04f);
            Color dimBlue = new Color(0.25f, 0.40f, 0.90f), amber = new Color(1f, 0.55f, 0.10f), warm = new Color(1f, 0.6f, 0.3f);

            // Normal: cool cyan everywhere, warm glow in the pod bay
            foreach (float x in new[] { -9f, 0f, 9f })
                foreach (float z in new[] { -4.5f, 4.5f })
                    Point(normal, "Bay", new Vector3(x, 7f, z), cyan, 1.4f, 11f);
            Point(normal, "B", new Vector3(0f, 4.5f, -13f), cyan, 1.6f, 10f);
            Point(normal, "E", new Vector3(-5f, 4.5f, -20f), cyan, 1.4f, 9f);
            Point(normal, "E", new Vector3(5f, 4.5f, -20f), cyan, 1.4f, 9f);
            Point(normal, "G", new Vector3(0f, 4.5f, 10.5f), cyan, 1.2f, 7f);
            Point(normal, "H", new Vector3(17f, 3.6f, 0f), cyan, 1.4f, 9f);
            foreach (float x in new[] { -18f, -26f, -34f })
                Point(normal, "C", new Vector3(x, 3.5f, 0f), cyan, 1.3f, 8f);
            Point(normal, "D", new Vector3(-44f, 5.5f, -2.5f), cyan, 1.4f, 10f);
            Point(normal, "D Pod Glow", new Vector3(-42.5f, 1.6f, 0f), warm, 1.8f, 6f);

            // Emergency: pulsing red (the controller animates these)
            foreach (var p in new[] { new Vector3(-10f, 6f, -6f), new Vector3(10f, 6f, -6f), new Vector3(-10f, 6f, 6f), new Vector3(10f, 6f, 6f),
                                      new Vector3(0f, 4.5f, -13f), new Vector3(0f, 4.5f, -20f), new Vector3(0f, 4.5f, 10.5f), new Vector3(17f, 3.5f, 0f),
                                      new Vector3(-18f, 3.5f, 0f), new Vector3(-26f, 3.5f, 0f), new Vector3(-34f, 3.5f, 0f), new Vector3(-44f, 5f, 0f) })
                Point(emergency, "Alarm", p, red, 1.7f, 13f);

            // Zero-G: dim blue + blinking amber warnings ("Warning" lights blink)
            Point(zeroG, "Dim", new Vector3(-6f, 7f, 0f), dimBlue, 0.8f, 16f);
            Point(zeroG, "Dim", new Vector3(6f, 7f, 0f), dimBlue, 0.8f, 16f);
            Point(zeroG, "Dim", new Vector3(0f, 4.5f, -19f), dimBlue, 0.7f, 12f);
            Point(zeroG, "Dim", new Vector3(-26f, 3.5f, 0f), dimBlue, 0.6f, 12f);
            Point(zeroG, "Dim", new Vector3(-44f, 5f, 0f), dimBlue, 0.6f, 12f);
            foreach (var p in new[] { new Vector3(-13.4f, 4.6f, 3f), new Vector3(13.4f, 4.6f, -3f), new Vector3(-20f, 3.3f, 1.5f),
                                      new Vector3(-32f, 3.3f, -1.5f), new Vector3(-39f, 4.5f, 3f), new Vector3(0f, 4.5f, 8.8f) })
                Point(zeroG, "Warning", p, amber, 2.2f, 7f);

            var presets = lighting.AddComponent<FactoryLightingPresets>();
            presets.normalLights = normal.gameObject;
            presets.emergencyLights = emergency.gameObject;
            presets.zeroGravityLights = zeroG.gameObject;
            // darker, moodier ambient than the component defaults, closer to the reference board
            presets.normalAmbient = new Color(0.07f, 0.085f, 0.11f);
            presets.emergencyAmbient = new Color(0.07f, 0.012f, 0.012f);
            presets.zeroGravityAmbient = new Color(0.02f, 0.03f, 0.055f);
            presets.Set(FactoryLightingPresets.Preset.Normal);

            // Faint fill from the default directional light (sunlight through the windows)
            Light sun = null;
            foreach (var l in Object.FindObjectsByType<Light>())
                if (l.type == LightType.Directional) { sun = l; break; }
            if (sun)
            {
                sun.name = "Fill Light (Earth glow)";
                sun.transform.SetParent(lighting.transform, true);
                sun.transform.rotation = Quaternion.Euler(30f, 10f, 0f);
                sun.color = new Color(0.55f, 0.7f, 1f);
                sun.intensity = 0.25f;
                sun.shadows = LightShadows.None;
                sun.lightmapBakeType = LightmapBakeType.Baked;
            }
        }

        static void Point(Transform parent, string name, Vector3 pos, Color color, float intensity, float range)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point; l.color = color; l.intensity = intensity; l.range = range;
            l.shadows = LightShadows.Soft;                     // baked shadows cost nothing at runtime
            l.lightmapBakeType = LightmapBakeType.Baked;       // Quest: baked into per-preset lightmaps (FactoryVerse > Bake Factory Lighting)
        }

        // ------------------------------------------------------------------ cameras (cinematic reference angles)
        static void BuildCameras()
        {
            var g = Group("Camera Points", _root);
            var main = Camera.main;
            if (main)
            {
                main.name = "Cam A - Main Assembly Bay (Primary)";
                main.transform.SetParent(g, true);
                Aim(main, new Vector3(-11.5f, 4.5f, -7.8f), new Vector3(2f, 1.8f, 5f));
            }
            AddCam(g, "Cam B - Assembly Bay Opposite", new Vector3(12f, 4.5f, 7.5f), new Vector3(-3f, 1.5f, -3f));
            AddCam(g, "Cam C - Maintenance Corridor", new Vector3(-15f, 1.8f, 0.6f), new Vector3(-38f, 1.7f, 0f));
            AddCam(g, "Cam D - Escape Pod Bay", new Vector3(-39f, 3.2f, 4.2f), new Vector3(-46f, 1.2f, -0.5f));
            AddCam(g, "Cam E - Observation Deck", new Vector3(0f, 1.8f, -17.6f), new Vector3(0f, 1.2f, -40f));
            AddCam(g, "Cam G - Control Console", new Vector3(0f, 1.9f, 7.5f), new Vector3(0f, 1.0f, 11.5f));
            // Top-down layout plan (like the board's floor plan): orthographic, near plane clips ceilings and walls above 4 m
            var plan = new GameObject("Cam Top - Layout Plan");
            plan.transform.SetParent(g, false);
            var pc = plan.AddComponent<Camera>();
            Aim(pc, new Vector3(-15f, 30f, -5.5f), new Vector3(-15f, 0f, -5.5f));
            plan.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            pc.orthographic = true;
            pc.orthographicSize = 21f;
            pc.nearClipPlane = 26.2f;   // renders y < ~3.8 m only
            pc.farClipPlane = 40f;
            pc.enabled = false;
        }

        static void AddCam(Transform parent, string name, Vector3 pos, Vector3 target)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var cam = go.AddComponent<Camera>();
            Aim(cam, pos, target);
            cam.enabled = false;   // reference viewpoints; enable one (and disable the primary) to look through it
        }

        static void Aim(Camera cam, Vector3 pos, Vector3 target)
        {
            cam.transform.position = pos;
            cam.transform.LookAt(target);
            cam.fieldOfView = 62f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 600f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.005f, 0.006f, 0.012f);
        }

        // ------------------------------------------------------------------ helpers
        static Transform Group(string name, Transform parent)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return t;
        }

        static Material Mat(string name)
        {
            if (!_mats.TryGetValue(name, out var m))
                _mats[name] = m = AssetDatabase.LoadAssetAtPath<Material>($"{ModelMaterialSetup.MaterialsFolder}/{name}.mat");
            return m;
        }

        static GameObject Box(string name, Transform parent, Vector3 center, Vector3 size, string mat, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, true);
            go.transform.position = center;
            go.transform.rotation = Quaternion.identity;
            var ls = parent.lossyScale;
            go.transform.localScale = new Vector3(size.x / ls.x, size.y / ls.y, size.z / ls.z);
            var m = Mat(mat);
            if (m) go.GetComponent<Renderer>().sharedMaterial = m;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            MarkStatic(go, mat);
            return go;
        }

        static GameObject Model(string model, Transform parent, Vector3 pos, float yRot, bool local = false)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelMaterialSetup.ModelsFolder}/{model}.fbx");
            if (!asset) { if (!_missing.Contains(model)) _missing.Add(model); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            if (local) { go.transform.localPosition = pos; go.transform.localRotation = Quaternion.Euler(0f, yRot, 0f); }
            else { go.transform.position = pos; go.transform.rotation = Quaternion.Euler(0f, yRot, 0f); }
            // solid to the player: exact mesh colliders (static scenery), except see-through / far-away models
            if (!NoCollide.Contains(model))
                foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
                    if (!mf.GetComponent<Collider>()) mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
            // Quest: everything here is static scenery -> static batching, baked lightmaps, occlusion culling.
            // See-through models must not occlude; the far-away backdrop is batched but not lightmapped.
            bool backdrop = model == "FV_Earth_Backdrop" || model == "FV_Station_Exterior";
            var flags = backdrop ? StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic
                                 : StaticFlags(occluder: !SeeThrough.Contains(model));
            foreach (var t in go.GetComponentsInChildren<Transform>())
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, flags);
            return go;
        }

        static readonly HashSet<string> NoCollide = new HashSet<string> { "FV_ARIA_HoloDisplay", "FV_Earth_Backdrop", "FV_Station_Exterior", "FV_MissionHUD" };
        static readonly HashSet<string> SeeThrough = new HashSet<string> { "FV_ARIA_HoloDisplay", "FV_ObservationWindow", "FV_MissionHUD" };

        static StaticEditorFlags StaticFlags(bool occluder)
        {
            var f = StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ReflectionProbeStatic;
            return occluder ? f | StaticEditorFlags.OccluderStatic : f;
        }

        static void MarkStatic(GameObject go, string mat)
        {
            bool seeThrough = mat == "FV_Hologram" || mat == "FV_Glass" || mat == "FV_HUD_BG";
            GameObjectUtility.SetStaticEditorFlags(go, StaticFlags(occluder: !seeThrough));
        }

        // ------------------------------------------------------------------ player
        static void BuildPlayer()
        {
            var player = new GameObject("Player");
            player.transform.SetParent(_root, false);
            player.transform.position = new Vector3(0f, 0.05f, -7.2f);   // south side of the main bay, facing north
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = 0.3f; cc.center = new Vector3(0f, 0.9f, 0f);
            cc.stepOffset = 0.35f; cc.slopeLimit = 45f;

            var viewGo = new GameObject("Player View");
            viewGo.transform.SetParent(player.transform, false);
            viewGo.transform.localPosition = new Vector3(0f, 1.65f, 0f);
            viewGo.tag = "MainCamera";
            var cam = viewGo.AddComponent<Camera>();
            cam.fieldOfView = 70f; cam.nearClipPlane = 0.03f; cam.farClipPlane = 600f;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.005f, 0.006f, 0.012f);
            viewGo.AddComponent<AudioListener>();

            var lampGo = new GameObject("Headlamp");
            lampGo.transform.SetParent(viewGo.transform, false);
            var lamp = lampGo.AddComponent<Light>();
            lamp.type = LightType.Spot; lamp.range = 14f; lamp.spotAngle = 55f; lamp.intensity = 1.6f;
            lamp.color = new Color(0.9f, 0.95f, 1f); lamp.shadows = LightShadows.None;

            // the old primary camera becomes a reference viewpoint only
            foreach (var c in Object.FindObjectsByType<Camera>())
                if (c != cam && c.CompareTag("MainCamera"))
                {
                    c.tag = "Untagged"; c.enabled = false;
                    var al = c.GetComponent<AudioListener>(); if (al) Object.DestroyImmediate(al);
                }

            // teleport points (Tab) — one per zone, facing into it
            var tp = Group("Teleport Points", _root);
            (string n, Vector3 p, float yaw)[] pts =
            {
                ("A  Main Assembly Bay", new Vector3(0f, 0.05f, -7.2f), 0f),
                ("G  Control / Emergency", new Vector3(0f, 0.05f, 8.2f), 0f),
                ("H  Storage Area", new Vector3(15f, 0.05f, 0f), 90f),
                ("C  Maintenance Corridor", new Vector3(-15.5f, 0.05f, 0f), -90f),
                ("D  Escape Pod Bay", new Vector3(-39.5f, 0.05f, 1.5f), -90f),
                ("B  Secondary Work Area", new Vector3(0f, 0.05f, -10f), 180f),
                ("E  Observation Deck", new Vector3(0f, 0.05f, -18f), 180f),
            };
            var points = new Transform[pts.Length];
            for (int i = 0; i < pts.Length; i++)
            {
                var t = Group(pts[i].n, tp);
                t.position = pts[i].p; t.rotation = Quaternion.Euler(0f, pts[i].yaw, 0f);
                points[i] = t;
            }

            var rig = player.AddComponent<FactoryPlayerRig>();
            rig.view = cam; rig.headlamp = lamp; rig.teleportPoints = points;
            rig.lighting = Object.FindAnyObjectByType<FactoryLightingPresets>();

            BuildXRRig(player, points, rig.lighting);
        }

        // OpenXR rig (Quest / Quest Link). At runtime FactoryVRRig keeps whichever rig fits: XR if a headset is
        // active, otherwise it switches itself off and leaves the desktop Player running.
        static void BuildXRRig(GameObject desktopPlayer, Transform[] points, FactoryLightingPresets lighting)
        {
            var xr = new GameObject("XR Rig");
            xr.transform.SetParent(_root, false);
            xr.transform.SetPositionAndRotation(desktopPlayer.transform.position, desktopPlayer.transform.rotation);
            var cc = xr.AddComponent<CharacterController>();
            cc.height = 1.7f; cc.radius = 0.25f; cc.center = new Vector3(0f, 0.85f, 0f);
            cc.stepOffset = 0.3f; cc.slopeLimit = 45f;

            var offset = Group("Camera Offset", xr.transform);
            var camGo = new GameObject("XR Camera");
            camGo.transform.SetParent(offset, false);
            camGo.transform.localPosition = new Vector3(0f, 1.65f, 0f);   // replaced by head tracking at runtime
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.03f; cam.farClipPlane = 600f;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.005f, 0.006f, 0.012f);
            camGo.AddComponent<AudioListener>();

            var lampGo = new GameObject("Headlamp");
            lampGo.transform.SetParent(camGo.transform, false);
            var lamp = lampGo.AddComponent<Light>();
            lamp.type = LightType.Spot; lamp.range = 14f; lamp.spotAngle = 55f; lamp.intensity = 1.6f;
            lamp.color = new Color(0.9f, 0.95f, 1f); lamp.shadows = LightShadows.None;
            lamp.lightmapBakeType = LightmapBakeType.Realtime;

            Transform Hand(string name, float side)
            {
                var h = Group(name, offset);
                h.localPosition = new Vector3(0.2f * side, 1.1f, 0.3f);
                // Engineer VR hand model (Blender: wrist pivot, fingers +Z, palm -Y). On a controller the palm faces
                // inward, so roll it 90° and pull it back so the palm sits on the grip point.
                var handAsset = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelMaterialSetup.ModelsFolder}/{(side < 0 ? "FV_VRHand_L" : "FV_VRHand_R")}.fbx");
                if (handAsset)
                {
                    var model = (GameObject)PrefabUtility.InstantiatePrefab(handAsset, h);
                    model.transform.localPosition = new Vector3(0f, 0f, -0.075f);
                    model.transform.localRotation = Quaternion.Euler(0f, 0f, side < 0 ? 90f : -90f);
                    foreach (var t in model.GetComponentsInChildren<Transform>()) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
                    return h;
                }
                // fallback if the hand FBX is missing: simple glove from boxes
                var parts = new[]
                {
                    LBox(h, "Palm", new Vector3(0f, 0f, 0.02f), new Vector3(0.085f, 0.03f, 0.1f), "FV_Greeble_Dark", Vector3.zero),
                    LBox(h, "Fingers", new Vector3(0f, -0.005f, 0.095f), new Vector3(0.08f, 0.025f, 0.07f), "FV_Hull_Grey", new Vector3(12f, 0f, 0f)),
                    LBox(h, "Thumb", new Vector3(-0.05f * side, 0f, 0.04f), new Vector3(0.025f, 0.022f, 0.06f), "FV_Hull_Grey", new Vector3(0f, -30f * side, 0f)),
                    LBox(h, "Knuckles", new Vector3(0f, 0.017f, 0.06f), new Vector3(0.07f, 0.006f, 0.012f), "FV_Emissive_Blue", Vector3.zero),
                    LBox(h, "Cuff", new Vector3(0f, 0f, -0.07f), new Vector3(0.075f, 0.06f, 0.09f), "FV_Industrial_Yellow", Vector3.zero),
                };
                foreach (var p in parts) GameObjectUtility.SetStaticEditorFlags(p, 0);   // hands move: never static
                return h;
            }
            var left = Hand("Left Hand", -1f);
            var right = Hand("Right Hand", 1f);

            var vr = xr.AddComponent<FactoryVRRig>();
            vr.cameraOffset = offset; vr.headCamera = cam; vr.leftHand = left; vr.rightHand = right;
            vr.headlamp = lamp; vr.lighting = lighting; vr.teleportPoints = points; vr.desktopRig = desktopPlayer;

            // Hands-first: tracked hands + point-and-pinch teleport pads along the whole mission route
            xr.layer = 2;   // "Ignore Raycast": hand aim rays pass through the player's own body capsule
            var hands = xr.AddComponent<FactoryHandRig>();
            hands.rig = vr; hands.cameraOffset = offset; hands.head = cam;
            hands.controllerHands = new[] { left.gameObject, right.gameObject };
            hands.jointMaterial = Mat("FV_Hull_Grey"); hands.tipMaterial = Mat("FV_Emissive_Blue");
            hands.pads = BuildTeleportPads();
        }

        static FactoryTeleportPad[] BuildTeleportPads()
        {
            // (label, position, facing yaw) — every mission beat is reachable seated: point at the next pad and pinch
            (string label, Vector3 p, float yaw)[] defs =
            {
                ("Bay entrance", new Vector3(0f, 0f, -7.2f), 0f),
                ("R-7 workstation", new Vector3(-8f, 0f, -0.3f), 0f),
                ("Main assembly line", new Vector3(3f, 0f, -0.6f), 0f),
                ("Control alcove", new Vector3(0f, 0f, 8.2f), 0f),
                ("Storage", new Vector3(15.5f, 0f, 0f), 90f),
                ("Secondary work area", new Vector3(0f, 0f, -12f), 180f),
                ("Observation deck", new Vector3(0f, 0f, -19.5f), 180f),
                ("West exit", new Vector3(-12f, 0f, 0f), -90f),
                ("Corridor entry", new Vector3(-16.5f, 0f, 0f), -90f),
                ("Corridor mid", new Vector3(-26f, 0f, 0f), -90f),
                ("Corridor end", new Vector3(-35f, 0f, 0f), -90f),
                ("Pod bay", new Vector3(-40.5f, 0f, 1.5f), -90f),
                ("Inside the pod", new Vector3(-45.2f, 0.47f, 0f), 90f),
            };
            var root = Group("Teleport Pads (hands)", _root);
            var idle = Mat("FV_Hologram"); var hover = Mat("FV_Emissive_Green");
            var list = new List<FactoryTeleportPad>();
            foreach (var (label, p, yaw) in defs)
            {
                var g = Group("Pad " + label, root);
                g.position = p; g.rotation = Quaternion.Euler(0f, yaw, 0f);
                var ring = LCyl(g, "Ring", new Vector3(0f, 0.03f, 0f), 0.45f, 0.02f, "FV_Hologram", Vector3.zero);
                var arrow = LBox(g, "Facing", new Vector3(0f, 0.04f, 0.32f), new Vector3(0.12f, 0.01f, 0.22f), "FV_Hologram", new Vector3(0f, 45f, 0f));
                foreach (var go in new[] { ring, arrow }) GameObjectUtility.SetStaticEditorFlags(go, 0);   // shown/hidden at runtime
                var pad = g.gameObject.AddComponent<FactoryTeleportPad>();
                pad.label = label; pad.idleMaterial = idle; pad.hoverMaterial = hover;
                pad.visuals = new[] { ring.GetComponent<Renderer>(), arrow.GetComponent<Renderer>() };
                list.Add(pad);
            }
            return list.ToArray();
        }

        // ------------------------------------------------------------------ characters
        static void PlaceCharacters()
        {
            var g = Group("Characters", _root);
            // Commander Hale at the control alcove (G), turned toward the bay; looks at the player when near.
            // Paces between the outer consoles, stopping at each to face the bay.
            var hale = Character("FV_Char_Hale", g, new Vector3(3.2f, 0f, 10.3f), 180f, solid: true);
            Idle(hale, FactoryIdleMotion.Style.Human, 1f, 0f, true);
            Walker(hale, 1.0f,
                (new Vector3(3.2f, 0f, 10.3f), 6f, 180f),
                (new Vector3(-3.0f, 0f, 10.2f), 6f, 180f));
            // ARIA, the station AI hologram, on her projector in the main bay; hovers and follows the player with her gaze
            Idle(Character("FV_Char_ARIA", g, new Vector3(9.5f, 0f, -2.0f), 240f, solid: false), FactoryIdleMotion.Style.Hologram, 1f, 0f, true);
            // An engineer crew member: works at the arm-module bench in B (head down), then walks into the main bay
            // to check the F cell, and back.
            var eng = Character("FV_Char_Engineer", g, new Vector3(-3.0f, 0f, -14.05f), 0f, solid: true);
            Idle(eng, FactoryIdleMotion.Style.Human, 0.8f, 18f, false);
            Walker(eng, 1.15f,
                (new Vector3(-3.0f, 0f, -14.05f), 7f, 0f),
                (new Vector3(-0.6f, 0f, -14.6f), 0f, -1f),
                (new Vector3(0.0f, 0f, -10.5f), 0f, -1f),
                (new Vector3(-2.5f, 0f, -4.0f), 6f, 312f));
        }

        static void Walker(GameObject character, float speed, params (Vector3 pos, float wait, float faceYaw)[] stops)
        {
            if (!character) return;
            var w = character.AddComponent<FactoryNPCWalker>();
            w.walkSpeed = speed;
            w.route = new FactoryNPCWalker.Stop[stops.Length];
            for (int i = 0; i < stops.Length; i++)
                w.route[i] = new FactoryNPCWalker.Stop { position = stops[i].pos, wait = stops[i].wait, faceYaw = stops[i].faceYaw };
        }

        static void Idle(GameObject character, FactoryIdleMotion.Style style, float amount, float headDown, bool lookAtPlayer)
        {
            if (!character) return;
            var idle = character.AddComponent<FactoryIdleMotion>();
            idle.style = style; idle.amount = amount; idle.headDown = headDown; idle.lookAtPlayer = lookAtPlayer;
        }

        static GameObject Character(string model, Transform parent, Vector3 pos, float yaw, bool solid)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelMaterialSetup.ModelsFolder}/{model}.fbx");
            if (!asset) { if (!_missing.Contains(model)) _missing.Add(model); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            foreach (var t in go.GetComponentsInChildren<Transform>())
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);   // skinned characters: dynamic, lit by light probes
            // Start from the exact rest (bind) pose: FBX files with a Walk take can carry its first frame as the
            // default pose, so rebuild every bone from the skin's bind poses (parents first).
            var smr = go.GetComponentInChildren<SkinnedMeshRenderer>();
            if (smr && smr.sharedMesh && smr.sharedMesh.bindposes.Length == smr.bones.Length)
            {
                var bp = smr.sharedMesh.bindposes; var bones = smr.bones;
                var order = new List<int>();
                for (int i = 0; i < bones.Length; i++) if (bones[i]) order.Add(i);
                order.Sort((a, b) => Depth(bones[a]).CompareTo(Depth(bones[b])));
                foreach (int i in order)
                {
                    var m = smr.transform.localToWorldMatrix * bp[i].inverse;
                    bones[i].SetPositionAndRotation(m.GetPosition(), m.rotation);
                }
            }
            // relaxed standing pose: bring the T-pose arms down to the sides, elbows slightly bent
            foreach (var side in new[] { "Left", "Right" })
            {
                var upper = FindDeep(go.transform, side + "UpperArm");
                var lower = FindDeep(go.transform, side + "LowerArm");
                var hand = FindDeep(go.transform, side + "Hand");
                if (!upper || !lower) continue;
                Vector3 dir = (lower.position - upper.position).normalized;
                Vector3 outward = Vector3.ProjectOnPlane(dir, Vector3.up).normalized;
                Vector3 target = (Vector3.down + outward * 0.18f).normalized;
                upper.rotation = Quaternion.FromToRotation(dir, target) * upper.rotation;
                if (hand)
                {
                    Vector3 fdir = (hand.position - lower.position).normalized;
                    Vector3 ftarget = (fdir + go.transform.forward * 0.25f).normalized;   // forearm bends a little forward
                    lower.rotation = Quaternion.FromToRotation(fdir, ftarget) * lower.rotation;
                }
            }
            if (solid)
            {
                var cap = go.AddComponent<CapsuleCollider>();
                cap.center = new Vector3(0f, 0.88f, 0f); cap.height = 1.76f; cap.radius = 0.25f;
            }
            return go;
        }

        // ------------------------------------------------------------------ guided demo
        static GameObject _demoRobot, _demoCell;

        static void MakeDynamic(GameObject go)
        {
            if (!go) return;
            foreach (var t in go.GetComponentsInChildren<Transform>()) GameObjectUtility.SetStaticEditorFlags(t.gameObject, 0);
        }

        static void BuildDemoDirector()
        {
            var go = new GameObject("Demo Director");
            go.transform.SetParent(_root, false);
            var d = go.AddComponent<FactoryDemoDirector>();
            d.lighting = Object.FindAnyObjectByType<FactoryLightingPresets>();
            if (_demoRobot)
            {
                d.robot = _demoRobot.transform;
                var core = FindDeep(_demoRobot.transform, "R7_PowerCore");
                d.robotCore = core ? core.gameObject : null;
                d.robotHead = FindDeep(_demoRobot.transform, "R7_Head");
            }
            if (_demoCell) d.loosePart = _demoCell.transform;
            d.beaconMaterial = Mat("FV_Emissive_Cyan");
            d.beamMaterial = Mat("FV_Hologram");
            d.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            // keep LastShift first; add this scene so the demo can restart itself
            var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!list.Exists(s => s.path == ScenePath)) { list.Add(new EditorBuildSettingsScene(ScenePath, true)); EditorBuildSettings.scenes = list.ToArray(); }
        }

        static int Depth(Transform t) { int d = 0; while (t.parent) { d++; t = t.parent; } return d; }

        static Transform FindDeep(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }

        // Light probes so the (non-lightmapped) characters and VR hands pick up the baked lighting
        static void BuildLightProbes()
        {
            var go = new GameObject("Light Probes");
            go.transform.SetParent(_root, false);
            var pts = new List<Vector3>();
            void Grid(float x0, float x1, float z0, float z1, float step, params float[] ys)
            {
                for (float x = x0; x <= x1 + 0.01f; x += step)
                    for (float z = z0; z <= z1 + 0.01f; z += step)
                        foreach (float y in ys) pts.Add(new Vector3(x, y, z));
            }
            Grid(-12f, 12f, -8f, 8f, 4f, 0.4f, 1.7f, 4f);   // A
            Grid(-3f, 3f, 11f, 11f, 3f, 0.4f, 1.7f);         // G
            Grid(15f, 19f, -4f, 4f, 4f, 0.4f, 1.7f);         // H
            Grid(-5f, 5f, -16f, -10f, 3f, 0.4f, 1.7f);       // B
            Grid(-8f, 8f, -22f, -18f, 4f, 0.4f, 1.7f);       // E
            Grid(-36f, -16f, 0f, 0f, 4f, 0.4f, 1.7f, 3f);    // C
            Grid(-48f, -40f, -4f, 4f, 4f, 0.4f, 1.7f, 4f);   // D
            go.AddComponent<LightProbeGroup>().probePositions = pts.ToArray();
        }

        static void HazardFrame(Transform parent, float x0, float z0, float x1, float z1)
        {
            const float w = 0.15f, y = 0.012f;
            Box("Hazard", parent, new Vector3((x0 + x1) / 2, y, z0), new Vector3(x1 - x0, 0.01f, w), "FV_Industrial_Yellow", false);
            Box("Hazard", parent, new Vector3((x0 + x1) / 2, y, z1), new Vector3(x1 - x0, 0.01f, w), "FV_Industrial_Yellow", false);
            Box("Hazard", parent, new Vector3(x0, y, (z0 + z1) / 2), new Vector3(w, 0.01f, z1 - z0), "FV_Industrial_Yellow", false);
            Box("Hazard", parent, new Vector3(x1, y, (z0 + z1) / 2), new Vector3(w, 0.01f, z1 - z0), "FV_Industrial_Yellow", false);
        }

        static void Text(Transform parent, string text, Vector3 pos, Vector3 euler, float size, Color color)
        {
            var go = new GameObject("Sign " + text.Replace("\n", " "));
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(euler);
            var tm = go.AddComponent<TextMesh>();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tm.font = font;
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            tm.text = text;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontSize = 100;
            tm.characterSize = size / 10f;
            tm.color = color;
        }
    }
}
#endif
