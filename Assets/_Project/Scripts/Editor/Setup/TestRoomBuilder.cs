using System.Collections.Generic;
using System.IO;
using BugReport.Anomaly;
using BugReport.Containment;
using BugReport.Core.Entities;
using BugReport.Core.GameFlow;
using BugReport.DebugTools;
using BugReport.Glitches;
using BugReport.Hero;
using BugReport.InputHandling;
using BugReport.Interaction;
using BugReport.Audio;
using BugReport.Level;
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
        public const string GlitchFolder = "Assets/_Project/ScriptableObjects/Glitches";
        public const string CollisionTransferPath = GlitchFolder + "/GL_CollisionTransfer.asset";
        public const string DuplicationPath = GlitchFolder + "/GL_Duplication.asset";
        public const string FreezePath = GlitchFolder + "/GL_Freeze.asset";
        public const string ProfileFolder = "Assets/_Project/ScriptableObjects/Duplication";
        public const string CrateProfilePath = ProfileFolder + "/DP_Crate.asset";
        public const string PrefabFolder = "Assets/_Project/Prefabs/Generated";
        public const string CratePrefabPath = PrefabFolder + "/PF_Crate.prefab";
        public const string ContainmentSettingsPath = "Assets/_Project/ScriptableObjects/Containment/CS_Default.asset";

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

            string error;
            bool built;
            try
            {
                built = Build(out error);
            }
            catch (System.Exception e)
            {
                built = false;
                error = e.Message;
                Debug.LogException(e);
            }

            if (built) Debug.Log("[BUG REPORT] Built " + ScenePath + ". Press Play.");
            else Debug.LogError("[BUG REPORT] Test room build failed (scene not saved): " + error);
        }

        /// <summary>Builds and saves the scene. Returns false with a reason instead of leaving a half-built scene saved.</summary>
        public static bool Build(out string error)
        {
            error = null;
            EnsureFolder("Assets/_Project/Scenes");

            // Open the new scene FIRST. NewScene(Single) unloads assets nothing references yet, which
            // destroys asset objects loaded before it (the input actions came through as null). So every
            // asset this scene references is created or loaded after this line.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var mats = new MaterialSet();
            if (!mats.Create(out error)) return false;

            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (actions == null)
            {
                error = "Input actions not found at " + InputActionsPath + ". Select it in the Project window and check the Console for import errors.";
                return false;
            }

            var collisionTransfer = LoadOrCreate<CollisionTransferDefinition>(CollisionTransferPath, d => d.ApplyDefaults());
            var duplication = LoadOrCreate<DuplicationDefinition>(DuplicationPath, d => d.ApplyDefaults());
            var freeze = LoadOrCreate<FreezeDefinition>(FreezePath, d => d.ApplyDefaults());
            var crateProfile = LoadOrCreate<DuplicationProfile>(CrateProfilePath, p => p.Configure(null, 2));
            var containmentSettings = LoadOrCreate<ContainmentSettingsAsset>(ContainmentSettingsPath, _ => { });
            var cratePrefab = CreateCratePrefab(mats, crateProfile);
            if (cratePrefab == null)
            {
                error = "Could not save " + CratePrefabPath;
                return false;
            }
            var presentation = PresentationBuilder.LoadOrCreateAssets(mats);

            BuildLighting();
            var environment = new GameObject("--- ENVIRONMENT ---").transform;
            BuildRoom(environment, mats);
            BuildProps(environment, mats, cratePrefab);

            var hero = BuildHero(mats);
            var anomaly = BuildAnomaly(mats, actions, out var motor);
            var cameraRig = BuildCamera(anomaly.transform, out var scanner);
            var systems = BuildSystems(mats, scanner, motor, hero, new GlitchDefinition[] { collisionTransfer, duplication, freeze },
                containmentSettings, objectives: null, debugOverlayVisible: true, levelTitle: "TEST ROOM");
            WirePlayer(anomaly, motor, cameraRig, systems);
            PresentationBuilder.Apply(presentation, mats, systems, anomaly, hero, cameraRig.GetComponent<Camera>());

            var missing = FindUnassignedReferences(scene);
            if (missing.Count > 0)
            {
                error = "Scene NOT saved; unassigned references:\n  " + string.Join("\n  ", missing);
                return false;
            }

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                error = "Could not save " + ScenePath;
                return false;
            }
            AddToBuildSettings(ScenePath);
            return true;
        }

        // ---------------------------------------------------------------- lighting

        internal static void BuildLighting()
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

        internal static void CreatePointLight(string name, Vector3 position, Color color, float intensity, float range)
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

        internal static void BuildRoom(Transform parent, MaterialSet mats)
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
            AddCollision(firewall, CollisionMode.Solid, mats);

            // Hologram across the hero's route: intangible now. Making it solid (the "wrong" transfer
            // target) blocks the hero again, which teaches what the transfer does.
            var holo = Block("HoloPanel", parent, new Vector3(6f, 1.25f, 0f), new Vector3(0.15f, 2.5f, 4f), mats.Ghost, GameLayers.Index(GameLayers.Intangible));
            AddEntity(holo, "HOLO_PANEL", EntityCategory.Decoration,
                EntityCapabilities.Inspectable | EntityCapabilities.CollisionTransferable,
                "A projected panel. Everything passes through it.");
            AddCollision(holo, CollisionMode.Intangible, mats);

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

        internal static void BuildProps(Transform parent, MaterialSet mats, GameObject cratePrefab)
        {
            int props = GameLayers.Index(GameLayers.Props);
            int world = GameLayers.Index(GameLayers.World);
            int intangible = GameLayers.Index(GameLayers.Intangible);

            // Crates are prefab instances: Duplication copies them from the prefab, never from the scene object.
            CrateInstance(cratePrefab, parent, "Crate_A", "CRATE_A", "Standard-issue crate.", new Vector3(-6f, 0.5f, 4f), Quaternion.identity);
            CrateInstance(cratePrefab, parent, "Crate_B", "CRATE_B", "Standard-issue crate. Suspiciously identical to CRATE_A.",
                new Vector3(-4.6f, 0.5f, 4.4f), Quaternion.Euler(0f, 20f, 0f));

            // Starts in the air so it can be frozen mid-fall right after Play.
            var ball = Primitive(PrimitiveType.Sphere, "Ball", parent, new Vector3(-5f, 3f, -4f), Vector3.one * 0.7f, mats.Ball, props);
            AddBody(ball, 2f);
            AddEntity(ball, "TEST_BALL", EntityCategory.Prop,
                EntityCapabilities.Inspectable | EntityCapabilities.Freezable | EntityCapabilities.PropertyMutable,
                "Physics test object.");
            AddFreeze(ball, mats);

            // Scripted mover: freezing it stops it mid-travel; it resumes from the same phase.
            var lift = Block("Lift", parent, new Vector3(-2.5f, 0.15f, -5.5f), new Vector3(2f, 0.3f, 2f), mats.Lift, world);
            var liftBody = lift.AddComponent<Rigidbody>();
            liftBody.isKinematic = true;
            liftBody.interpolation = RigidbodyInterpolation.Interpolate;
            AddEntity(lift, "LIFT_PLATFORM", EntityCategory.SystemDevice,
                EntityCapabilities.Inspectable | EntityCapabilities.Freezable,
                "Moves up and down on a fixed schedule. Very punctual.");
            lift.AddComponent<PingPongMover>().Configure(new Vector3(0f, 2.5f, 0f), 5f);
            AddFreeze(lift, mats);

            // Not inspectable on purpose: demonstrates invalid-target feedback.
            var pillar = Primitive(PrimitiveType.Cylinder, "Pillar", parent, new Vector3(5f, 1.5f, -5f), new Vector3(0.8f, 1.5f, 0.8f), mats.Wall, world);
            AddEntity(pillar, "DECOR_PILLAR", EntityCategory.Decoration, EntityCapabilities.None, "Decorative. Nothing to see here.");

            // Lives on the Intangible layer: selectable, but nothing collides with it.
            var shadow = Primitive(PrimitiveType.Quad, "Shadow", parent, new Vector3(4f, 0.02f, 3f), new Vector3(3f, 3f, 1f), mats.Shadow, intangible, keepCollider: false);
            shadow.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            // A thin slab (quad-local z = world up) works both as a targeting surface and as a solid platform.
            var slab = shadow.AddComponent<BoxCollider>();
            slab.center = Vector3.zero;
            slab.size = new Vector3(1f, 1f, 0.02f);
            AddEntity(shadow, "SHADOW_DECAL", EntityCategory.Decoration,
                EntityCapabilities.Inspectable | EntityCapabilities.CollisionTransferable,
                "A shadow. Harmless. Not solid. Yet.");
            AddCollision(shadow, CollisionMode.Intangible, mats);
        }

        // ---------------------------------------------------------------- actors

        internal static HeroController BuildHero(MaterialSet mats)
        {
            var hero = CreateHeroBody(mats, new Vector3(-9f, 1.05f, 0f), 90f, out var cc);

            // Route graph. The firewall is the only way east; the holo panel can be walked around (north/south),
            // so making it solid re-routes the hero instead of blocking it.
            var graphRoot = new GameObject("HeroNavGraph");
            var start = NavNode(graphRoot.transform, "N_Start", new Vector3(-9f, 0f, 0f));
            var west = NavNode(graphRoot.transform, "N_FirewallWest", new Vector3(-1.2f, 0f, 0f));
            var east = NavNode(graphRoot.transform, "N_FirewallEast", new Vector3(3f, 0f, 0f));
            var south = NavNode(graphRoot.transform, "N_HoloSouth", new Vector3(6f, 0f, -3.2f));
            var north = NavNode(graphRoot.transform, "N_HoloNorth", new Vector3(6f, 0f, 3.2f));
            var beforeExit = NavNode(graphRoot.transform, "N_BeforeExit", new Vector3(9f, 0f, 0f));
            var exit = NavNode(graphRoot.transform, "N_Exit", new Vector3(10.5f, 0f, 0f));
            start.Configure(west);
            west.Configure(east);
            east.Configure(beforeExit, south, north);
            south.Configure(beforeExit);
            north.Configure(beforeExit);
            beforeExit.Configure(exit);

            var navGraph = graphRoot.AddComponent<HeroNavGraph>();
            navGraph.Configure(new[] { start, west, east, south, north, beforeExit, exit }, cc.radius, cc.height);

            var controller = hero.AddComponent<HeroController>();
            controller.Configure(navGraph, exit, newStartDelay: 1.5f);
            return controller;
        }

        /// <summary>The hero's body: capsule + visor, CharacterController, GameEntity. Behaviour is added by the caller.</summary>
        internal static GameObject CreateHeroBody(MaterialSet mats, Vector3 position, float yaw, out CharacterController cc)
        {
            int heroLayer = GameLayers.Index(GameLayers.Hero);
            var hero = new GameObject("HERO");
            hero.transform.position = position;
            hero.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            hero.layer = heroLayer;

            var body = Primitive(PrimitiveType.Capsule, "Body", hero.transform, Vector3.zero, Vector3.one, mats.Hero, heroLayer, keepCollider: false);
            body.transform.localPosition = Vector3.zero;
            var visor = Primitive(PrimitiveType.Cube, "Visor", hero.transform, Vector3.zero, new Vector3(0.55f, 0.15f, 0.2f), mats.HeroVisor, heroLayer, keepCollider: false);
            visor.transform.localPosition = new Vector3(0f, 0.5f, 0.38f);

            cc = hero.AddComponent<CharacterController>();
            cc.height = 2f;
            cc.radius = 0.4f;
            cc.center = Vector3.zero;
            cc.stepOffset = 0.3f;
            cc.skinWidth = 0.04f;

            AddEntity(hero, "HERO", EntityCategory.Hero, EntityCapabilities.Inspectable | EntityCapabilities.AIControllable,
                "PROJECT HERO protagonist. Follows the script. Trusts the level geometry.");
            return hero;
        }

        internal static HeroNavNode NavNode(Transform parent, string name, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            return go.AddComponent<HeroNavNode>();
        }

        internal static GameObject BuildAnomaly(MaterialSet mats, InputActionAsset actions, out AnomalyMotor motor)
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

        internal static AnomalyCameraRig BuildCamera(Transform anomaly, out TargetScanner scanner)
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

        /// <summary>References the level builders need after the shared systems exist.</summary>
        internal struct Systems
        {
            public GameObject Root;
            public SelectionController Selection;
            public GlitchController Glitches;
            public ContainmentDirector Containment;
            public ObjectiveDirector Objectives;
            public string LevelTitle;
            /// <summary>Set by levels that teach (optional); wired into the UI by PresentationBuilder.</summary>
            public BugReport.Tutorial.GuideDirector Guide;
        }

        /// <summary>Bootstrap, state, selection, glitches, containment, presenters, audio, kill plane. The UI is added by PresentationBuilder.</summary>
        internal static Systems BuildSystems(MaterialSet mats, TargetScanner scanner, AnomalyMotor motor, HeroController hero,
            GlitchDefinition[] glitchDefinitions, ContainmentSettingsAsset containmentSettings, ObjectiveDirector objectives,
            bool debugOverlayVisible, string levelTitle)
        {
            var systems = new GameObject("--- SYSTEMS ---");
            var bootstrap = systems.AddComponent<GameBootstrap>();
            WireEnum(bootstrap, "initialState", (int)GameState.Playing);
            systems.AddComponent<GameStatePresenter>();

            var selection = systems.AddComponent<SelectionController>();
            Wire(selection, "scanner", scanner);

            var glitchController = systems.AddComponent<GlitchController>();
            WireArray(glitchController, "glitches", glitchDefinitions);
            Wire(glitchController, "selection", selection);

            var containment = systems.AddComponent<ContainmentDirector>();
            Wire(containment, "settings", containmentSettings);

            var highlight = systems.AddComponent<EntityHighlightPresenter>();
            Wire(highlight, "lineMaterial", mats.Line);
            Wire(highlight, "glitches", glitchController);
            Wire(highlight, "containment", containment);

            var pulses = systems.AddComponent<GlitchPulsePresenter>();
            Wire(pulses, "lineMaterial", mats.Line);
            Wire(pulses, "glitches", glitchController);

            var audio = systems.AddComponent<AudioFeedback>();
            Wire(audio, "selection", selection);
            Wire(audio, "glitches", glitchController);
            Wire(audio, "containment", containment);
            if (objectives != null) Wire(audio, "objectives", objectives);

            systems.AddComponent<WorldBoundsGuard>();

            var overlay = systems.AddComponent<DiagnosticsOverlay>();
            Wire(overlay, "anomaly", motor);
            Wire(overlay, "selection", selection);
            Wire(overlay, "hero", hero);
            Wire(overlay, "glitches", glitchController);
            var overlaySo = new SerializedObject(overlay);
            overlaySo.FindProperty("visible").boolValue = debugOverlayVisible;
            overlaySo.ApplyModifiedPropertiesWithoutUndo();

            return new Systems
            {
                Root = systems, Selection = selection, Glitches = glitchController, Containment = containment,
                Objectives = objectives, LevelTitle = levelTitle,
            };
        }

        /// <summary>Adds the player controller to the anomaly and connects it.</summary>
        internal static void WirePlayer(GameObject anomaly, AnomalyMotor motor, AnomalyCameraRig cameraRig, Systems systems)
        {
            var controller = anomaly.AddComponent<AnomalyPlayerController>();
            Wire(controller, "input", anomaly.GetComponent<InputReader>());
            Wire(controller, "motor", motor);
            Wire(controller, "cameraRig", cameraRig);
            Wire(controller, "selection", systems.Selection);
            Wire(controller, "glitches", systems.Glitches);
        }

        internal static void AddCollision(GameObject go, CollisionMode authoredMode, MaterialSet mats)
        {
            var capability = go.AddComponent<CollisionCapability>();
            capability.Configure(go.GetComponents<Collider>(), authoredMode, GameLayers.World);
            if (go.GetComponent<RendererOverrides>() == null) go.AddComponent<RendererOverrides>();
            go.AddComponent<CollisionModeVisual>().Configure(mats.Ghost, mats.Solidified);
        }

        internal static void AddFreeze(GameObject go, MaterialSet mats)
        {
            go.AddComponent<FreezeCapability>();
            if (go.GetComponent<RendererOverrides>() == null) go.AddComponent<RendererOverrides>();
            go.AddComponent<FreezeVisual>().Configure(mats.Frozen);
        }

        /// <summary>(Re)generates the crate prefab. The profile asset must already exist so the prefab can reference it.</summary>
        internal static GameObject CreateCratePrefab(MaterialSet mats, DuplicationProfile profile)
        {
            EnsureFolder(PrefabFolder);
            var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            temp.name = "PF_Crate";
            temp.layer = GameLayers.Index(GameLayers.Props);
            temp.GetComponent<MeshRenderer>().sharedMaterial = mats.Crate;
            AddBody(temp, 10f);
            AddEntity(temp, "CRATE", EntityCategory.Prop,
                EntityCapabilities.Inspectable | EntityCapabilities.Duplicable | EntityCapabilities.Freezable | EntityCapabilities.PropertyMutable,
                "Standard-issue crate.");
            temp.AddComponent<DuplicationCapability>().Configure(profile);
            AddFreeze(temp, mats);

            var prefab = PrefabUtility.SaveAsPrefabAsset(temp, CratePrefabPath);
            Object.DestroyImmediate(temp);
            if (prefab == null) return null;

            if (profile.Prefab != prefab)
            {
                profile.SetPrefab(prefab);
                EditorUtility.SetDirty(profile);
                AssetDatabase.SaveAssets();
            }
            return prefab;
        }

        internal static GameEntity CrateInstance(GameObject prefab, Transform parent, string objectName, string displayName, string description, Vector3 position, Quaternion rotation)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = objectName;
            go.transform.SetPositionAndRotation(position, rotation);
            var entity = go.GetComponent<GameEntity>();
            entity.Configure(displayName, entity.Category, entity.Capabilities, description);
            PrefabUtility.RecordPrefabInstancePropertyModifications(entity);
            PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
            return entity;
        }

        /// <summary>Loads a designer asset if it exists (never overwrites tuning), otherwise creates it with defaults.</summary>
        internal static T LoadOrCreate<T>(string path, System.Action<T> applyDefaults) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;

            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            var asset = ScriptableObject.CreateInstance<T>();
            applyDefaults(asset);
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        // ---------------------------------------------------------------- helpers

        /// <summary>Every null object-reference field on project components (same rule as TestRoomSceneTests).</summary>
        public static List<string> FindUnassignedReferences(UnityEngine.SceneManagement.Scene scene)
        {
            var problems = new List<string>();
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (behaviour == null) continue;
                    var ns = behaviour.GetType().Namespace ?? string.Empty;
                    if (!ns.StartsWith("BugReport")) continue;

                    var so = new SerializedObject(behaviour);
                    var prop = so.GetIterator();
                    while (prop.NextVisible(true))
                    {
                        if (prop.propertyType != SerializedPropertyType.ObjectReference || prop.name == "m_Script") continue;
                        if (prop.objectReferenceValue == null && !IsOptional(behaviour.GetType(), prop.propertyPath))
                            problems.Add(behaviour.GetType().Name + " on '" + behaviour.name + "': " + prop.propertyPath);
                    }
                }

                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer is LineRenderer) continue; // outline lines get their material at runtime
                    if (renderer.sharedMaterial == null)
                        problems.Add(renderer.GetType().Name + " on '" + renderer.name + "': material");
                }
            }
            return problems;
        }

        /// <summary>
        /// True when the field at <paramref name="propertyPath"/> is marked [OptionalReference]. Follows
        /// nested paths through structs and arrays ("steps.Array.data[2].target").
        /// </summary>
        internal static bool IsOptional(System.Type type, string propertyPath)
        {
            const System.Reflection.BindingFlags Flags = System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
            var segments = propertyPath.Split('.');
            var current = type;
            for (int i = 0; i < segments.Length; i++)
            {
                if (segments[i] == "Array" && i + 1 < segments.Length && segments[i + 1].StartsWith("data["))
                {
                    current = current.IsArray ? current.GetElementType()
                        : current.IsGenericType ? current.GetGenericArguments()[0]
                        : null;
                    if (current == null) return false;
                    i++;
                    continue;
                }

                System.Reflection.FieldInfo field = null;
                for (var t = current; t != null && field == null; t = t.BaseType) field = t.GetField(segments[i], Flags);
                if (field == null) return false;
                if (i == segments.Length - 1) return System.Attribute.IsDefined(field, typeof(OptionalReferenceAttribute));
                current = field.FieldType;
            }
            return false;
        }

        internal static void EnsureFolderFor(string assetPath) => EnsureFolder(Path.GetDirectoryName(assetPath).Replace('\\', '/'));

        internal static void InvisibleBox(string name, Transform parent, Vector3 position, Vector3 size, int layer)
        {
            var go = new GameObject(name, typeof(BoxCollider));
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = size;
            go.layer = layer;
        }

        internal static GameObject Block(string name, Transform parent, Vector3 position, Vector3 size, Material material, int layer)
            => Primitive(PrimitiveType.Cube, name, parent, position, size, material, layer);

        internal static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material material, int layer, bool keepCollider = true)
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

        internal static void AddBody(GameObject go, float mass)
        {
            var body = go.AddComponent<Rigidbody>();
            body.mass = mass;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        internal static void AddEntity(GameObject go, string displayName, EntityCategory category, EntityCapabilities caps, string description)
        {
            var entity = go.AddComponent<GameEntity>();
            entity.Configure(displayName, category, caps, description);
        }

        internal static SerializedObject Serialized(Object target, string property, out SerializedProperty prop)
        {
            var so = new SerializedObject(target);
            prop = so.FindProperty(property);
            if (prop == null)
                throw new System.InvalidOperationException("[BUG REPORT] " + target.GetType().Name + " has no serialized field '" + property + "'.");
            return so;
        }

        internal static void Wire(Object target, string property, Object value)
        {
            var so = Serialized(target, property, out var prop);
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();

            // SerializedProperty silently ignores assignments it rejects (destroyed object, wrong type).
            // Read back so a failure is reported here, with the reason, instead of as a null at Play time.
            so.Update();
            if (so.FindProperty(property).objectReferenceValue != value)
            {
                bool destroyed = value == null && !ReferenceEquals(value, null);
                throw new System.InvalidOperationException(
                    "[BUG REPORT] Could not assign " + target.GetType().Name + "." + property +
                    " = " + (ReferenceEquals(value, null) ? "null" : value.GetType().Name) +
                    (destroyed ? " (the object was already destroyed/unloaded)" : string.Empty) +
                    "; field type '" + prop.type + "', asset path '" + (destroyed ? "-" : AssetDatabase.GetAssetPath(value)) +
                    "', persistent=" + (!destroyed && !ReferenceEquals(value, null) && EditorUtility.IsPersistent(value)) + ".");
            }
        }

        internal static void WireArray(Object target, string property, Object[] values)
        {
            var so = Serialized(target, property, out var prop);
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void WireFloat(Object target, string property, float value)
        {
            var so = Serialized(target, property, out var prop);
            prop.floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void WireEnum(Object target, string property, int value)
        {
            var so = Serialized(target, property, out var prop);
            prop.intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Adds the scene to Build Settings (optionally as the first scene, i.e. what a build launches).</summary>
        internal static void AddToBuildSettings(string path, bool first = false)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            int existing = scenes.FindIndex(x => x.path == path);
            if (existing >= 0)
            {
                if (!first || existing == 0) return;
                scenes.RemoveAt(existing);
            }
            var entry = new EditorBuildSettingsScene(path, true);
            if (first) scenes.Insert(0, entry);
            else scenes.Add(entry);
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
        internal sealed class MaterialSet
        {
            public Material Floor, Wall, Firewall, Exit, Crate, Ball, Shadow, Hero, HeroVisor, AnomalyCore, AnomalyFragment, Line, Ghost, Solidified, Frozen, Lift,
                PlateIdle, PlatePressed, Gate, Piston;

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
                // Collision Transfer placeholder looks (electric blue): see-through ghost, glowing solid.
                Ghost = Transparent(lit, "M_CollisionGhost", new Color(0.2f, 0.55f, 1f, 0.28f), new Color(0.1f, 0.35f, 1f) * 0.4f);
                Solidified = Lit(lit, "M_CollisionSolidified", new Color(0.15f, 0.4f, 0.95f), 0.8f, new Color(0.15f, 0.5f, 1f) * 1.6f);
                Frozen = Lit(lit, "M_Frozen", new Color(0.75f, 0.95f, 1f), 0.95f, new Color(0.45f, 0.85f, 1f) * 0.9f);
                Lift = Lit(lit, "M_Lift", new Color(0.45f, 0.5f, 0.58f), 0.5f);
                PlateIdle = Lit(lit, "M_PlateIdle", new Color(0.55f, 0.2f, 0.2f), 0.4f, new Color(0.6f, 0.1f, 0.1f) * 0.5f);
                PlatePressed = Lit(lit, "M_PlatePressed", new Color(0.2f, 0.6f, 0.3f), 0.4f, new Color(0.1f, 0.9f, 0.3f) * 0.9f);
                Gate = Lit(lit, "M_Gate", new Color(0.7f, 0.55f, 0.2f), 0.6f, new Color(0.9f, 0.5f, 0.1f) * 0.25f);
                Piston = Lit(lit, "M_Piston", new Color(0.35f, 0.37f, 0.42f), 0.7f, new Color(1f, 0.3f, 0.1f) * 0.3f);
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

            /// <summary>URP Lit configured for alpha blending (what the material inspector does for Surface Type = Transparent).</summary>
            private static Material Transparent(Shader shader, string name, Color baseColor, Color emission)
            {
                var m = new Material(shader) { name = name };
                m.SetColor("_BaseColor", baseColor);
                m.SetFloat("_Smoothness", 0.9f);
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", 0f);
                m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite", 0f);
                m.SetOverrideTag("RenderType", "Transparent");
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission);
                m.renderQueue = (int)RenderQueue.Transparent;
                return Save(m, name);
            }

            internal static Material Save(Material material, string name)
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
                existing.renderQueue = material.renderQueue;
                existing.SetOverrideTag("RenderType", material.GetTag("RenderType", false));
                EditorUtility.SetDirty(existing);
                Object.DestroyImmediate(material);
                return existing;
            }
        }
    }
}
