using System.Collections.Generic;
using System.IO;
using BugReport.Anomaly;
using BugReport.Core.Entities;
using BugReport.Core.GameFlow;
using BugReport.DebugTools;
using BugReport.Hero;
using BugReport.InputHandling;
using BugReport.Interaction;
using BugReport.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace BugReport.EditorTools.Setup
{
    /// <summary>
    /// Builds the Milestone 1 test room procedurally so the scene is reproducible
    /// from code and reviewable in diffs. Re-running asks before overwriting.
    ///
    /// Layout (top view, +x right/east, +z up/north):
    ///   hero spawn (-9,0)  →  firewall at x=1 (blocks the hero; anomaly can fly over)  →  exit (10.5,0)
    /// </summary>
    public static class TestRoomBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/TestRoom.unity";
        public const string InputActionsPath = "Assets/_Project/Input/BugReportControls.inputactions";
        public const string MaterialFolder = "Assets/_Project/Art/Materials/Generated";

        private const float RoomHalfX = 12f;
        private const float RoomHalfZ = 8f;
        private const float WallHeight = 4.5f;
        private const float WallThickness = 0.4f;
        private const float CeilingHeight = 6f;

        [MenuItem(ProjectSetup.MenuRoot + "2. Build Test Room Scene", priority = 2)]
        public static void BuildMenu()
        {
            if (!GameLayers.AreConfigured(out var missing))
            {
                if (!EditorUtility.DisplayDialog("BUG REPORT", "Missing layers: " + missing + ".\nRun 'Configure Project' now?", "Configure", "Cancel")) return;
                if (!ProjectSetup.ConfigureProject(out var report))
                {
                    Debug.LogError(report);
                    return;
                }
                Debug.Log(report);
            }

            if (File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog("BUG REPORT", ScenePath + " already exists.\nRebuild it? Manual edits to that scene will be lost.", "Rebuild", "Cancel"))
            {
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            if (Build(out var error)) Debug.Log("[BUG REPORT] Built " + ScenePath + ". Press Play.");
            else Debug.LogError("[BUG REPORT] Test room build failed: " + error);
        }

        /// <summary>Builds and saves the scene. Returns false with a reason instead of leaving a half-built scene saved.</summary>
        public static bool Build(out string error)
        {
            error = null;
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (actions == null)
            {
                error = "Input actions not found at " + InputActionsPath;
                return false;
            }

            var mats = new MaterialSet();
            if (!mats.Create(out error)) return false;

            EnsureFolder("Assets/_Project/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildLighting();
            var environment = new GameObject("--- ENVIRONMENT ---").transform;
            BuildRoom(environment, mats);
            BuildProps(environment, mats);

            var hero = BuildHero(mats);
            var anomaly = BuildAnomaly(mats, actions, out var motor);
            var cameraRig = BuildCamera(anomaly.transform, out var scanner);
            BuildSystems(mats, scanner, motor, hero, out var selection);

            var controller = anomaly.AddComponent<AnomalyPlayerController>();
            Wire(controller, "input", anomaly.GetComponent<InputReader>());
            Wire(controller, "motor", motor);
            Wire(controller, "cameraRig", cameraRig);
            Wire(controller, "selection", selection);

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                error = "Could not save " + ScenePath;
                return false;
            }
            AddToBuildSettings(ScenePath);
            return true;
        }

        // ---------------------------------------------------------------- lighting

        private static void BuildLighting()
        {
            var sun = new GameObject("Sun", typeof(Light));
            var light = sun.GetComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.95f, 0.86f);
            light.intensity = 1.3f;
            light.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.62f, 0.75f);
            RenderSettings.ambientEquatorColor = new Color(0.42f, 0.40f, 0.38f);
            RenderSettings.ambientGroundColor = new Color(0.20f, 0.18f, 0.17f);
            RenderSettings.sun = light;

            CreatePointLight("Light_Cyan", new Vector3(6f, 3.2f, 4f), new Color(0.35f, 0.9f, 1f), 3f, 9f);
            CreatePointLight("Light_Warm", new Vector3(-6f, 3.2f, -3f), new Color(1f, 0.7f, 0.45f), 2.5f, 9f);
        }

        private static void CreatePointLight(string name, Vector3 position, Color color, float intensity, float range)
        {
            var go = new GameObject(name, typeof(Light));
            go.transform.position = position;
            var light = go.GetComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
        }

        // ---------------------------------------------------------------- room

        private static void BuildRoom(Transform parent, MaterialSet mats)
        {
            int world = GameLayers.Index(GameLayers.World);

            // Floor and outer walls are plain geometry, not entities: outlining them would be noise.
            Block("Floor", parent, new Vector3(0f, -0.1f, 0f), new Vector3(RoomHalfX * 2f, 0.2f, RoomHalfZ * 2f), mats.Floor, world);

            float y = WallHeight * 0.5f;
            Block("Wall_North", parent, new Vector3(0f, y, RoomHalfZ), new Vector3(RoomHalfX * 2f, WallHeight, WallThickness), mats.Wall, world);
            Block("Wall_South", parent, new Vector3(0f, y, -RoomHalfZ), new Vector3(RoomHalfX * 2f, WallHeight, WallThickness), mats.Wall, world);
            Block("Wall_West", parent, new Vector3(-RoomHalfX, y, 0f), new Vector3(WallThickness, WallHeight, RoomHalfZ * 2f), mats.Wall, world);
            Block("Wall_East", parent, new Vector3(RoomHalfX, y, 0f), new Vector3(WallThickness, WallHeight, RoomHalfZ * 2f), mats.Wall, world);

            // Invisible lid plus wall extensions up to it, so the anomaly cannot leave the level
            // while the visible walls stay low enough to keep the room open and well lit.
            float lidSpan = WallThickness * 2f;
            InvisibleBox("Bounds_Ceiling", parent, new Vector3(0f, CeilingHeight, 0f), new Vector3(RoomHalfX * 2f + lidSpan, 0.2f, RoomHalfZ * 2f + lidSpan), world);
            float extHeight = CeilingHeight - WallHeight + 0.2f;
            float extY = WallHeight + extHeight * 0.5f - 0.1f;
            InvisibleBox("Bounds_North", parent, new Vector3(0f, extY, RoomHalfZ), new Vector3(RoomHalfX * 2f, extHeight, WallThickness), world);
            InvisibleBox("Bounds_South", parent, new Vector3(0f, extY, -RoomHalfZ), new Vector3(RoomHalfX * 2f, extHeight, WallThickness), world);
            InvisibleBox("Bounds_West", parent, new Vector3(-RoomHalfX, extY, 0f), new Vector3(WallThickness, extHeight, RoomHalfZ * 2f), world);
            InvisibleBox("Bounds_East", parent, new Vector3(RoomHalfX, extY, 0f), new Vector3(WallThickness, extHeight, RoomHalfZ * 2f), world);

            // The obstacle Milestone 2 (Collision Transfer) is designed around.
            float innerZ = RoomHalfZ * 2f - WallThickness;
            var firewall = Block("Firewall", parent, new Vector3(1f, 1.5f, 0f), new Vector3(0.5f, 3f, innerZ), mats.Firewall, world);
            AddEntity(firewall, "FIREWALL_01", EntityCategory.Structure,
                EntityCapabilities.Inspectable | EntityCapabilities.CollisionTransferable,
                "Blocks the hero's route to the exit. Its collision looks... negotiable.");

            // Exit objective: pad plus a glowing frame against the east wall.
            var exitRoot = new GameObject("ExitDoor");
            exitRoot.transform.SetParent(parent, false);
            exitRoot.transform.position = new Vector3(10.5f, 0f, 0f);
            Block("Pad", exitRoot.transform, new Vector3(10.5f, 0.05f, 0f), new Vector3(1.6f, 0.1f, 2.6f), mats.Exit, world);
            Block("Post_L", exitRoot.transform, new Vector3(11.4f, 1.4f, -1.4f), new Vector3(0.25f, 2.8f, 0.25f), mats.Exit, world);
            Block("Post_R", exitRoot.transform, new Vector3(11.4f, 1.4f, 1.4f), new Vector3(0.25f, 2.8f, 0.25f), mats.Exit, world);
            Block("Lintel", exitRoot.transform, new Vector3(11.4f, 2.9f, 0f), new Vector3(0.25f, 0.25f, 3.05f), mats.Exit, world);
            AddEntity(exitRoot, "EXIT_DOOR", EntityCategory.Objective,
                EntityCapabilities.Inspectable | EntityCapabilities.ObjectiveRelevant,
                "Level objective. The hero wants to be here.");
        }

        private static void BuildProps(Transform parent, MaterialSet mats)
        {
            int props = GameLayers.Index(GameLayers.Props);
            int world = GameLayers.Index(GameLayers.World);
            int intangible = GameLayers.Index(GameLayers.Intangible);

            var propCaps = EntityCapabilities.Inspectable | EntityCapabilities.Duplicable | EntityCapabilities.Freezable | EntityCapabilities.PropertyMutable;
            var crateA = Block("Crate_A", parent, new Vector3(-6f, 0.5f, 4f), Vector3.one, mats.Crate, props);
            AddBody(crateA, 10f);
            AddEntity(crateA, "CRATE_A", EntityCategory.Prop, propCaps, "Standard-issue crate.");

            var crateB = Block("Crate_B", parent, new Vector3(-4.6f, 0.5f, 4.4f), Vector3.one, mats.Crate, props);
            crateB.transform.rotation = Quaternion.Euler(0f, 20f, 0f);
            AddBody(crateB, 10f);
            AddEntity(crateB, "CRATE_B", EntityCategory.Prop, propCaps, "Standard-issue crate. Suspiciously identical to CRATE_A.");

            var ball = Primitive(PrimitiveType.Sphere, "Ball", parent, new Vector3(-5f, 0.35f, -4f), Vector3.one * 0.7f, mats.Ball, props);
            AddBody(ball, 2f);
            AddEntity(ball, "TEST_BALL", EntityCategory.Prop,
                EntityCapabilities.Inspectable | EntityCapabilities.Freezable | EntityCapabilities.PropertyMutable,
                "Physics test object.");

            // Not inspectable on purpose: demonstrates invalid-target feedback.
            var pillar = Primitive(PrimitiveType.Cylinder, "Pillar", parent, new Vector3(5f, 1.5f, -5f), new Vector3(0.8f, 1.5f, 0.8f), mats.Wall, world);
            AddEntity(pillar, "DECOR_PILLAR", EntityCategory.Decoration, EntityCapabilities.None, "Decorative. Nothing to see here.");

            // Lives on the Intangible layer: selectable, but nothing collides with it.
            var shadow = Primitive(PrimitiveType.Quad, "Shadow", parent, new Vector3(4f, 0.02f, 3f), new Vector3(3f, 3f, 1f), mats.Shadow, intangible);
            shadow.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            AddEntity(shadow, "SHADOW_DECAL", EntityCategory.Decoration,
                EntityCapabilities.Inspectable | EntityCapabilities.CollisionTransferable,
                "A shadow. Harmless. Not solid. Yet.");
        }

        // ---------------------------------------------------------------- actors

        private static PlaceholderHero BuildHero(MaterialSet mats)
        {
            int heroLayer = GameLayers.Index(GameLayers.Hero);
            var hero = new GameObject("HERO");
            hero.transform.position = new Vector3(-9f, 1.05f, 0f);
            hero.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            hero.layer = heroLayer;

            var body = Primitive(PrimitiveType.Capsule, "Body", hero.transform, Vector3.zero, Vector3.one, mats.Hero, heroLayer, keepCollider: false);
            body.transform.localPosition = Vector3.zero;
            var visor = Primitive(PrimitiveType.Cube, "Visor", hero.transform, Vector3.zero, new Vector3(0.55f, 0.15f, 0.2f), mats.HeroVisor, heroLayer, keepCollider: false);
            visor.transform.localPosition = new Vector3(0f, 0.5f, 0.38f);

            var cc = hero.AddComponent<CharacterController>();
            cc.height = 2f;
            cc.radius = 0.4f;
            cc.center = Vector3.zero;
            cc.stepOffset = 0.3f;
            cc.skinWidth = 0.04f;

            AddEntity(hero, "HERO", EntityCategory.Hero, EntityCapabilities.Inspectable | EntityCapabilities.AIControllable,
                "PROJECT HERO protagonist. Follows the script. Trusts the level geometry.");

            var routeRoot = new GameObject("HeroRoute").transform;
            var route = new[]
            {
                Waypoint(routeRoot, "WP_0", new Vector3(-5f, 0f, 0f)),
                Waypoint(routeRoot, "WP_1_Exit", new Vector3(10.5f, 0f, 0f)),
            };

            var placeholder = hero.AddComponent<PlaceholderHero>();
            placeholder.Configure(route, newStartDelay: 1.5f);
            return placeholder;
        }

        private static Transform Waypoint(Transform parent, string name, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            return go.transform;
        }

        private static GameObject BuildAnomaly(MaterialSet mats, InputActionAsset actions, out AnomalyMotor motor)
        {
            int layer = GameLayers.Index(GameLayers.Anomaly);
            var anomaly = new GameObject("ANOMALY");
            anomaly.transform.position = new Vector3(-10f, 2.2f, -5f);
            anomaly.layer = layer;

            var cc = anomaly.AddComponent<CharacterController>();
            cc.radius = 0.3f;
            cc.height = 0.6f;
            cc.center = Vector3.zero;
            cc.stepOffset = 0.1f;
            cc.skinWidth = 0.03f;
            cc.slopeLimit = 90f;

            motor = anomaly.AddComponent<AnomalyMotor>();
            var reader = anomaly.AddComponent<InputReader>();
            Wire(reader, "actions", actions);

            AddEntity(anomaly, "ANOMALY", EntityCategory.Anomaly, EntityCapabilities.None, "You.");

            var visualRoot = new GameObject("Visual");
            visualRoot.transform.SetParent(anomaly.transform, false);
            visualRoot.layer = layer;
            var core = Primitive(PrimitiveType.Sphere, "Core", visualRoot.transform, Vector3.zero, Vector3.one * 0.32f, mats.AnomalyCore, layer, keepCollider: false);
            core.transform.localPosition = Vector3.zero;
            var fragments = new Transform[4];
            for (int i = 0; i < fragments.Length; i++)
            {
                var f = Primitive(PrimitiveType.Cube, "Fragment_" + i, visualRoot.transform, Vector3.zero, Vector3.one * 0.09f, mats.AnomalyFragment, layer, keepCollider: false);
                f.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                fragments[i] = f.transform;
            }
            var visual = visualRoot.AddComponent<AnomalyVisual>();
            Wire(visual, "core", core.transform);
            WireArray(visual, "fragments", fragments);

            var glow = new GameObject("Glow", typeof(Light));
            glow.transform.SetParent(anomaly.transform, false);
            var light = glow.GetComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.4f, 0.95f, 1f);
            light.intensity = 1.5f;
            light.range = 3f;
            light.shadows = LightShadows.None;
            return anomaly;
        }

        private static AnomalyCameraRig BuildCamera(Transform anomaly, out TargetScanner scanner)
        {
            var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            go.tag = "MainCamera";
            var cam = go.GetComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 200f;
            cam.fieldOfView = 65f;
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.backgroundColor = new Color(0.08f, 0.09f, 0.12f);

            var rig = go.AddComponent<AnomalyCameraRig>();
            Wire(rig, "target", anomaly);
            WireFloat(rig, "initialYaw", 60f);

            scanner = go.AddComponent<TargetScanner>();
            Wire(scanner, "viewCamera", cam);
            Wire(scanner, "ignoreRoot", anomaly);
            return rig;
        }

        private static void BuildSystems(MaterialSet mats, TargetScanner scanner, AnomalyMotor motor, PlaceholderHero hero, out SelectionController selection)
        {
            var systems = new GameObject("--- SYSTEMS ---");
            var bootstrap = systems.AddComponent<GameBootstrap>();
            WireEnum(bootstrap, "initialState", (int)GameState.Playing);
            systems.AddComponent<GameStatePresenter>();

            selection = systems.AddComponent<SelectionController>();
            Wire(selection, "scanner", scanner);

            var highlight = systems.AddComponent<EntityHighlightPresenter>();
            Wire(highlight, "lineMaterial", mats.Line);

            systems.AddComponent<ReticlePresenter>();

            var overlay = systems.AddComponent<DebugOverlay>();
            Wire(overlay, "anomaly", motor);
            Wire(overlay, "selection", selection);
            Wire(overlay, "hero", hero);
        }

        // ---------------------------------------------------------------- helpers

        private static void InvisibleBox(string name, Transform parent, Vector3 position, Vector3 size, int layer)
        {
            var go = new GameObject(name, typeof(BoxCollider));
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = size;
            go.layer = layer;
        }

        private static GameObject Block(string name, Transform parent, Vector3 position, Vector3 size, Material material, int layer)
            => Primitive(PrimitiveType.Cube, name, parent, position, size, material, layer);

        private static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material material, int layer, bool keepCollider = true)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            go.layer = layer;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        private static void AddBody(GameObject go, float mass)
        {
            var body = go.AddComponent<Rigidbody>();
            body.mass = mass;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        private static void AddEntity(GameObject go, string displayName, EntityCategory category, EntityCapabilities caps, string description)
        {
            var entity = go.AddComponent<GameEntity>();
            entity.Configure(displayName, category, caps, description);
        }

        private static SerializedObject Serialized(Object target, string property, out SerializedProperty prop)
        {
            var so = new SerializedObject(target);
            prop = so.FindProperty(property);
            if (prop == null)
                throw new System.InvalidOperationException("[BUG REPORT] " + target.GetType().Name + " has no serialized field '" + property + "'.");
            return so;
        }

        private static void Wire(Object target, string property, Object value)
        {
            var so = Serialized(target, property, out var prop);
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireArray(Object target, string property, Object[] values)
        {
            var so = Serialized(target, property, out var prop);
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireFloat(Object target, string property, float value)
        {
            var so = Serialized(target, property, out var prop);
            prop.floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireEnum(Object target, string property, int value)
        {
            var so = Serialized(target, property, out var prop);
            prop.intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddToBuildSettings(string path)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (var s in scenes)
            {
                if (s.path == path) return;
            }
            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        internal static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        /// <summary>Generated placeholder materials. Updated in place if they already exist.</summary>
        private sealed class MaterialSet
        {
            public Material Floor, Wall, Firewall, Exit, Crate, Ball, Shadow, Hero, HeroVisor, AnomalyCore, AnomalyFragment, Line;

            public bool Create(out string error)
            {
                error = null;
                var lit = Shader.Find("Universal Render Pipeline/Lit");
                if (lit == null)
                {
                    error = "Shader 'Universal Render Pipeline/Lit' not found. Is URP installed and active?";
                    return false;
                }
                // Needs vertex colour + alpha so each outline can be tinted per LineRenderer.
                var lineShader = Shader.Find("Sprites/Default");
                if (lineShader == null) lineShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (lineShader == null)
                {
                    error = "No shader found for outline lines (tried Sprites/Default, URP Particles/Unlit).";
                    return false;
                }

                EnsureFolder(MaterialFolder);
                Floor = Lit(lit, "M_Floor", new Color(0.78f, 0.74f, 0.68f), 0.25f);
                Wall = Lit(lit, "M_Wall", new Color(0.88f, 0.86f, 0.82f), 0.2f);
                Firewall = Lit(lit, "M_Firewall", new Color(0.25f, 0.35f, 0.6f), 0.6f, new Color(0.1f, 0.35f, 1f) * 0.6f);
                Exit = Lit(lit, "M_Exit", new Color(0.3f, 0.8f, 0.45f), 0.5f, new Color(0.2f, 1f, 0.45f) * 1.2f);
                Crate = Lit(lit, "M_Crate", new Color(0.85f, 0.55f, 0.25f), 0.3f);
                Ball = Lit(lit, "M_Ball", new Color(0.9f, 0.3f, 0.35f), 0.7f);
                Shadow = Lit(lit, "M_Shadow", new Color(0.12f, 0.12f, 0.15f), 0f);
                Hero = Lit(lit, "M_Hero", new Color(0.95f, 0.75f, 0.2f), 0.45f);
                HeroVisor = Lit(lit, "M_HeroVisor", new Color(0.15f, 0.2f, 0.3f), 0.9f);
                AnomalyCore = Lit(lit, "M_AnomalyCore", new Color(0.8f, 1f, 1f), 0.9f, new Color(0.3f, 0.95f, 1f) * 3f);
                AnomalyFragment = Lit(lit, "M_AnomalyFragment", new Color(0.9f, 0.3f, 1f), 0.9f, new Color(0.9f, 0.2f, 1f) * 1.5f);
                Line = Save(new Material(lineShader) { name = "M_DebugLine" }, "M_DebugLine");
                AssetDatabase.SaveAssets();
                return true;
            }

            private static Material Lit(Shader shader, string name, Color baseColor, float smoothness, Color? emission = null)
            {
                var m = new Material(shader) { name = name };
                m.SetColor("_BaseColor", baseColor);
                m.SetFloat("_Smoothness", smoothness);
                if (emission.HasValue)
                {
                    m.EnableKeyword("_EMISSION");
                    m.SetColor("_EmissionColor", emission.Value);
                    m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                }
                return Save(m, name);
            }

            private static Material Save(Material material, string name)
            {
                string path = MaterialFolder + "/" + name + ".mat";
                var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (existing == null)
                {
                    AssetDatabase.CreateAsset(material, path);
                    return material;
                }
                existing.shader = material.shader;
                existing.CopyPropertiesFromMaterial(material);
                existing.shaderKeywords = material.shaderKeywords;
                existing.globalIlluminationFlags = material.globalIlluminationFlags;
                EditorUtility.SetDirty(existing);
                Object.DestroyImmediate(material);
                return existing;
            }
        }
    }
}
