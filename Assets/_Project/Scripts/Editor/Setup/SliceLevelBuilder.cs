using System.IO;
using BugReport.Anomaly;
using BugReport.Containment;
using BugReport.Core.Entities;
using BugReport.Glitches;
using BugReport.Hero;
using BugReport.Level;
using BugReport.Tutorial;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using static BugReport.EditorTools.Setup.TestRoomBuilder;

namespace BugReport.EditorTools.Setup
{
    /// <summary>
    /// Builds the vertical-slice level "TRAINING LAB" procedurally (reproducible, reviewable).
    ///
    /// Hall x −20…20, z −6…6, three rooms, one objective each:
    ///   A (spawn) | FIREWALL x=−8 | B: plates + crate | GATE x=6 | C: piston corridor x 10…14 | EXIT x=18
    /// 1. Firewall: Collision Transfer onto a shadow decal.
    /// 2. Gate opens when both plates have weight; one crate exists → Duplication (its copy lands on
    ///    plate 2). Alternative: transfer the gate's collision.
    /// 3. The hero refuses to walk into the moving piston → Freeze it while retracted.
    ///    Alternative: make it intangible.
    /// </summary>
    public static class SliceLevelBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Level_01_TrainingLab.unity";
        public const string LevelTitle = "TRAINING LAB";

        private const float HalfX = 20f;
        private const float HalfZ = 6f;
        private const float WallHeight = 4.5f;
        private const float Thickness = 0.4f;
        private const float Ceiling = 6f;

        [MenuItem(ProjectSetup.MenuRoot + "3. Build Vertical Slice Level", priority = 3)]
        public static void BuildMenu()
        {
            if (!GameLayers.AreConfigured(out var missing))
            {
                EditorUtility.DisplayDialog("BUG REPORT", "Missing layers: " + missing + ".\nRun 'Configure Project' first.", "OK");
                return;
            }
            if (File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog("BUG REPORT", ScenePath + " already exists.\nRebuild it? Manual edits to that scene will be lost.", "Rebuild", "Cancel"))
                return;
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
            else Debug.LogError("[BUG REPORT] Slice level build failed (scene not saved): " + error);
        }

        public static bool Build(out string error)
        {
            error = null;
            EnsureFolder("Assets/_Project/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var mats = new MaterialSet();
            if (!mats.Create(out error)) return false;
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (actions == null)
            {
                error = "Input actions not found at " + InputActionsPath;
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
            ChapterTheme.Apply(1, RenderSettings.sun);
            var env = new GameObject("--- ENVIRONMENT ---").transform;
            BuildHall(env, mats);
            env.Find("Floor").GetComponent<MeshRenderer>().sharedMaterial = PresentationBuilder.GridFloor(HalfX * 2f, HalfZ * 2f);

            int world = GameLayers.Index(GameLayers.World);
            int intangible = GameLayers.Index(GameLayers.Intangible);

            // ---- Room A → B: firewall + shadow target
            var firewall = Block("Firewall", env, new Vector3(-8f, 1.5f, 0f), new Vector3(0.5f, 3f, HalfZ * 2f - Thickness), mats.Firewall, world);
            AddEntity(firewall, "FIREWALL_01", EntityCategory.Structure, EntityCapabilities.Inspectable | EntityCapabilities.CollisionTransferable,
                "Blocks the way to the lab. Its collision looks... negotiable.");
            AddCollision(firewall, CollisionMode.Solid, mats);
            var shadowA = ShadowDecal(env, mats, "ShadowA", "SHADOW_DECAL_A", new Vector3(-13f, 0.02f, 3.5f), intangible);

            // ---- Room B: plates, crate, gate
            var plate1 = Plate(env, mats, "Plate_1", new Vector3(-1.5f, 0f, -3f));
            var plate2 = Plate(env, mats, "Plate_2", new Vector3(-0.4f, 0f, -3f));
            var crate = CrateInstance(cratePrefab, env, "Crate_Lab", "CRATE_LAB", "Heavy enough for one pressure plate. Only one of it. Officially.",
                new Vector3(-1.5f, 0.62f, -3f), Quaternion.identity);
            ShadowDecal(env, mats, "ShadowB", "SHADOW_DECAL_B", new Vector3(-4.5f, 0.02f, 3.5f), intangible);

            float gapHalf = 1.6f;
            float sideLength = HalfZ - gapHalf;
            Block("GateWall_N", env, new Vector3(6f, WallHeight * 0.5f, gapHalf + sideLength * 0.5f), new Vector3(0.5f, WallHeight, sideLength), mats.Wall, world);
            Block("GateWall_S", env, new Vector3(6f, WallHeight * 0.5f, -gapHalf - sideLength * 0.5f), new Vector3(0.5f, WallHeight, sideLength), mats.Wall, world);
            var gate = Block("Gate", env, new Vector3(6f, 1.5f, 0f), new Vector3(0.45f, 3f, gapHalf * 2f), mats.Gate, world);
            var gateBody = gate.AddComponent<Rigidbody>();
            gateBody.isKinematic = true;
            gateBody.interpolation = RigidbodyInterpolation.Interpolate;
            AddEntity(gate, "SECURITY_GATE", EntityCategory.Structure, EntityCapabilities.Inspectable | EntityCapabilities.CollisionTransferable,
                "Opens when both pressure plates are weighed down.");
            AddCollision(gate, CollisionMode.Solid, mats);
            gate.AddComponent<GateController>().Configure(new[] { plate1, plate2 }, new Vector3(0f, -3.2f, 0f), 1.2f);

            // ---- Room C: corridor with piston
            Block("Divider10_N", env, new Vector3(10f, WallHeight * 0.5f, gapHalf + sideLength * 0.5f), new Vector3(0.4f, WallHeight, sideLength), mats.Wall, world);
            Block("Divider10_S", env, new Vector3(10f, WallHeight * 0.5f, -gapHalf - sideLength * 0.5f), new Vector3(0.4f, WallHeight, sideLength), mats.Wall, world);
            Block("Divider14_N", env, new Vector3(14f, WallHeight * 0.5f, gapHalf + sideLength * 0.5f), new Vector3(0.4f, WallHeight, sideLength), mats.Wall, world);
            Block("Divider14_S", env, new Vector3(14f, WallHeight * 0.5f, -gapHalf - sideLength * 0.5f), new Vector3(0.4f, WallHeight, sideLength), mats.Wall, world);
            Block("Corridor_N", env, new Vector3(12f, WallHeight * 0.5f, gapHalf + 0.2f), new Vector3(4f, WallHeight, 0.4f), mats.Wall, world);
            Block("Corridor_S_W", env, new Vector3(10.7f, WallHeight * 0.5f, -gapHalf - 0.2f), new Vector3(1.4f, WallHeight, 0.4f), mats.Wall, world);
            Block("Corridor_S_E", env, new Vector3(13.3f, WallHeight * 0.5f, -gapHalf - 0.2f), new Vector3(1.4f, WallHeight, 0.4f), mats.Wall, world);

            var piston = Block("Piston", env, new Vector3(12f, 1.5f, -3.4f), new Vector3(1f, 3f, 3.2f), mats.Piston, world);
            var pistonBody = piston.AddComponent<Rigidbody>();
            pistonBody.isKinematic = true;
            pistonBody.interpolation = RigidbodyInterpolation.Interpolate;
            AddEntity(piston, "CRUSHER_PISTON", EntityCategory.SystemDevice,
                EntityCapabilities.Inspectable | EntityCapabilities.Freezable | EntityCapabilities.CollisionTransferable,
                "Slams across the corridor on a timer. The hero will not walk into it while it moves.");
            piston.AddComponent<PingPongMover>().Configure(new Vector3(0f, 0f, 3.4f), 3f);
            AddFreeze(piston, mats);
            AddCollision(piston, CollisionMode.Solid, mats);
            var hazardVolume = new GameObject("PistonDangerZone", typeof(BoxCollider));
            hazardVolume.transform.SetParent(env, false);
            hazardVolume.transform.position = new Vector3(12f, 1.5f, 0f);
            var hv = hazardVolume.GetComponent<BoxCollider>();
            hv.isTrigger = true;
            hv.size = new Vector3(1.6f, 3f, gapHalf * 2f);
            piston.AddComponent<HeroHazard>().Configure(hv);

            // ---- Exit
            var exitRoot = new GameObject("ExitDoor");
            exitRoot.transform.SetParent(env, false);
            exitRoot.transform.position = new Vector3(18f, 0f, 0f);
            Block("Pad", exitRoot.transform, new Vector3(18f, 0.05f, 0f), new Vector3(1.6f, 0.1f, 2.6f), mats.Exit, world);
            Block("Post_L", exitRoot.transform, new Vector3(19.2f, 1.4f, -1.4f), new Vector3(0.25f, 2.8f, 0.25f), mats.Exit, world);
            Block("Post_R", exitRoot.transform, new Vector3(19.2f, 1.4f, 1.4f), new Vector3(0.25f, 2.8f, 0.25f), mats.Exit, world);
            Block("Lintel", exitRoot.transform, new Vector3(19.2f, 2.9f, 0f), new Vector3(0.25f, 0.25f, 3.05f), mats.Exit, world);
            AddEntity(exitRoot, "EXIT_DOOR", EntityCategory.Objective, EntityCapabilities.Inspectable | EntityCapabilities.ObjectiveRelevant,
                "Level objective. The hero wants to be here.");

            Decorate(env);

            CreatePointLight("Light_RoomB", new Vector3(-1f, 3.5f, 0f), new Color(1f, 0.85f, 0.6f), 2.5f, 12f);
            CreatePointLight("Light_RoomC", new Vector3(12f, 3.5f, 0f), new Color(1f, 0.45f, 0.3f), 2.5f, 10f);

            // ---- Hero + route graph
            var heroGo = CreateHeroBody(mats, new Vector3(-17f, 1.05f, 0f), 90f, out var cc);
            var graphRoot = new GameObject("HeroNavGraph");
            var nStart = NavNode(graphRoot.transform, "N_Start", new Vector3(-17f, 0f, 0f));
            var nFirewall = NavNode(graphRoot.transform, "N_BeforeFirewall", new Vector3(-10f, 0f, 0f));
            var nLabWest = NavNode(graphRoot.transform, "N_LabWest", new Vector3(-6f, 0f, 0f));
            var nGateWest = NavNode(graphRoot.transform, "N_BeforeGate", new Vector3(4f, 0f, 0f));
            var nGateEast = NavNode(graphRoot.transform, "N_AfterGate", new Vector3(8f, 0f, 0f));
            var nCorridorWest = NavNode(graphRoot.transform, "N_CorridorWest", new Vector3(9.2f, 0f, 0f));
            var nCorridorEast = NavNode(graphRoot.transform, "N_CorridorEast", new Vector3(14.8f, 0f, 0f));
            var nExit = NavNode(graphRoot.transform, "N_Exit", new Vector3(18f, 0f, 0f));
            nStart.Configure(nFirewall);
            nFirewall.Configure(nLabWest);
            nLabWest.Configure(nGateWest);
            nGateWest.Configure(nGateEast);
            nGateEast.Configure(nCorridorWest);
            nCorridorWest.Configure(nCorridorEast);
            nCorridorEast.Configure(nExit);
            var graph = graphRoot.AddComponent<HeroNavGraph>();
            graph.Configure(new[] { nStart, nFirewall, nLabWest, nGateWest, nGateEast, nCorridorWest, nCorridorEast, nExit }, cc.radius, cc.height);
            var hero = heroGo.AddComponent<HeroController>();
            hero.Configure(graph, nExit, newStartDelay: 2f);

            // ---- Objectives
            var objectivesGo = new GameObject("--- OBJECTIVES ---");
            var zoneLab = Zone(objectivesGo.transform, "Zone_Lab", new Vector3(-0.75f, 1.5f, 0f), new Vector3(13.5f, 3f, HalfZ * 2f), hero);
            var zoneCorridorRoom = Zone(objectivesGo.transform, "Zone_MachineRoom", new Vector3(13.25f, 1.5f, 0f), new Vector3(13.5f, 3f, HalfZ * 2f), hero);
            var zoneExit = Zone(objectivesGo.transform, "Zone_Exit", new Vector3(18f, 1.5f, 0f), new Vector3(2.4f, 3f, 3f), hero);
            var objectives = objectivesGo.AddComponent<ObjectiveDirector>();
            objectives.Configure(new[]
            {
                new ObjectiveDirector.Entry("firewall", "GET THE HERO PAST THE FIREWALL",
                    "Give the firewall's solidity to something harmless.", zoneLab),
                new ObjectiveDirector.Entry("gate", "OPEN THE SECURITY GATE",
                    "Both plates need weight. There is only one crate. Officially.", zoneCorridorRoom),
                new ObjectiveDirector.Entry("piston", "GET THE HERO PAST THE PISTON",
                    "The hero won't walk into moving machinery.", zoneExit),
            });

            // ---- Player, camera, systems
            var anomaly = BuildAnomaly(mats, actions, out var motor);
            anomaly.transform.position = new Vector3(-18f, 2.2f, -4f);
            var cameraRig = BuildCamera(anomaly.transform, out var scanner);
            var systems = BuildSystems(mats, scanner, motor, hero, new GlitchDefinition[] { collisionTransfer, duplication, freeze },
                containmentSettings, objectives, debugOverlayVisible: false, levelTitle: LevelTitle);
            WirePlayer(anomaly, motor, cameraRig, systems);

            // ---- Guide: this level is the tutorial. One glitch per objective, taught step by step.
            var guide = new GameObject("--- GUIDE ---").AddComponent<GuideDirector>();
            var firewallEntity = firewall.GetComponent<GameEntity>();
            var pistonEntity = piston.GetComponent<GameEntity>();
            guide.Configure(new[]
            {
                new GuideDirector.Step(0, "Fly with [W][A][S][D] and the mouse. [SPACE] and [CTRL] move up and down.", GuideCondition.Move),
                new GuideDirector.Step(0, "The hero is stuck behind the blue FIREWALL. Look at it.", GuideCondition.LookAt, firewallEntity),
                new GuideDirector.Step(0, "Press [E] to select the firewall.", GuideCondition.Select, firewallEntity),
                new GuideDirector.Step(0, "Now look at the dark SHADOW on the floor.", GuideCondition.LookAt, shadowA, requireSelected: firewallEntity),
                new GuideDirector.Step(0, "Click [LMB]: the wall's solidity moves into the shadow.", GuideCondition.Apply, shadowA, collisionTransfer),
                new GuideDirector.Step(0, "The firewall is a ghost now. Watch the hero walk through.", GuideCondition.WaitForObjective),

                new GuideDirector.Step(1, "New glitch: DUPLICATION. Press [Q] to switch to it.", GuideCondition.Equip, glitch: duplication),
                new GuideDirector.Step(1, "Both red plates need weight, and there is one crate. Look at the CRATE.", GuideCondition.LookAt, crate),
                new GuideDirector.Step(1, "Click [LMB] to copy it. The copy lands on the empty plate.", GuideCondition.Apply, crate, duplication),
                new GuideDirector.Step(1, "The gate opens when both plates are pressed. Made a mistake? [R] undoes your last glitch.", GuideCondition.WaitForObjective),

                new GuideDirector.Step(2, "New glitch: FREEZE. Press [Q] until FREEZE is selected.", GuideCondition.Equip, glitch: freeze),
                new GuideDirector.Step(2, "The hero won't walk into the moving PISTON. Look at it.", GuideCondition.LookAt, pistonEntity),
                new GuideDirector.Step(2, "Wait until it pulls back out of the corridor, then click [LMB].", GuideCondition.Apply, pistonEntity, freeze),
                new GuideDirector.Step(2, "Frozen. The hero can pass safely.", GuideCondition.WaitForObjective),
            }, new[]
            {
                new GuideDirector.Unlock(1, duplication),
                new GuideDirector.Unlock(2, freeze),
            }, systems.Glitches, objectives, anomaly.transform);
            var guideProblem = guide.Validate();
            if (guideProblem != null)
            {
                error = "Guide: " + guideProblem;
                return false;
            }
            systems.Guide = guide;
            Wire(systems.Root.GetComponent<BugReport.Audio.AudioFeedback>(), "guide", guide);
            PresentationBuilder.Apply(presentation, mats, systems, anomaly, hero, cameraRig.GetComponent<Camera>());

            var missingRefs = FindUnassignedReferences(scene);
            if (missingRefs.Count > 0)
            {
                error = "Scene NOT saved; unassigned references:\n  " + string.Join("\n  ", missingRefs);
                return false;
            }
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                error = "Could not save " + ScenePath;
                return false;
            }
            AddToBuildSettings(ScenePath);
            LevelCatalogBuilder.SyncBuildSettings();
            return true;
        }

        /// <summary>
        /// PROJECT HERO set dressing from the KayKit dungeon kit (skipped if the CC0 art isn't fetched).
        /// Everything stands against the side walls (|z| ≥ 5), clear of the hero's route along z = 0,
        /// the plates, the shadows and the piston corridor. The chest is what the hero walks towards.
        /// </summary>
        private static void Decorate(Transform env)
        {
            if (!ArtBuilder.DungeonAvailable) return;
            var material = ArtBuilder.AtlasMaterial("M_Dungeon", ArtBuilder.DungeonTexturePath);
            int world = GameLayers.Index(GameLayers.World);
            var root = new GameObject("Decor").transform;
            root.SetParent(env, false);
            (string model, Vector3 position, float yaw, float height)[] props =
            {
                ("barrel_large", new Vector3(-18.6f, 0f, 5.1f), 0f, 1.1f),
                ("barrel_small_stack", new Vector3(-17.2f, 0f, 5.2f), 30f, 1.0f),
                ("crates_stacked", new Vector3(-18.4f, 0f, -5.0f), 90f, 1.3f),
                ("keg", new Vector3(-9.8f, 0f, -5.1f), 0f, 0.9f),
                ("box_stacked", new Vector3(-6.4f, 0f, -5.0f), 15f, 1.2f),
                ("barrel_large", new Vector3(2.5f, 0f, 5.1f), 0f, 1.1f),
                ("shelf_small_candles", new Vector3(4.6f, 0f, 5.3f), 180f, 1.6f),
                ("barrel_small_stack", new Vector3(8.2f, 0f, 5.1f), 0f, 1.0f),
                ("crates_stacked", new Vector3(8.2f, 0f, -5.0f), 0f, 1.3f),
                ("table_small_decorated_A", new Vector3(16.5f, 0f, 5.0f), 0f, 0.9f),
                ("chest_gold", new Vector3(19.0f, 0f, -2.4f), -90f, 0.8f),
                ("barrel_large", new Vector3(18.8f, 0f, 4.8f), 0f, 1.1f),
            };
            foreach (var p in props) ArtBuilder.Decor(p.model, root, p.position, p.yaw, p.height, material, world);
        }

        private static void BuildHall(Transform parent, MaterialSet mats)
        {
            int world = GameLayers.Index(GameLayers.World);
            Block("Floor", parent, new Vector3(0f, -0.1f, 0f), new Vector3(HalfX * 2f, 0.2f, HalfZ * 2f), mats.Floor, world);
            float y = WallHeight * 0.5f;
            Block("Wall_North", parent, new Vector3(0f, y, HalfZ), new Vector3(HalfX * 2f, WallHeight, Thickness), mats.Wall, world);
            Block("Wall_South", parent, new Vector3(0f, y, -HalfZ), new Vector3(HalfX * 2f, WallHeight, Thickness), mats.Wall, world);
            Block("Wall_West", parent, new Vector3(-HalfX, y, 0f), new Vector3(Thickness, WallHeight, HalfZ * 2f), mats.Wall, world);
            Block("Wall_East", parent, new Vector3(HalfX, y, 0f), new Vector3(Thickness, WallHeight, HalfZ * 2f), mats.Wall, world);

            float lid = Thickness * 2f;
            InvisibleBox("Bounds_Ceiling", parent, new Vector3(0f, Ceiling, 0f), new Vector3(HalfX * 2f + lid, 0.2f, HalfZ * 2f + lid), world);
            float extHeight = Ceiling - WallHeight + 0.2f;
            float extY = WallHeight + extHeight * 0.5f - 0.1f;
            InvisibleBox("Bounds_North", parent, new Vector3(0f, extY, HalfZ), new Vector3(HalfX * 2f, extHeight, Thickness), world);
            InvisibleBox("Bounds_South", parent, new Vector3(0f, extY, -HalfZ), new Vector3(HalfX * 2f, extHeight, Thickness), world);
            InvisibleBox("Bounds_West", parent, new Vector3(-HalfX, extY, 0f), new Vector3(Thickness, extHeight, HalfZ * 2f), world);
            InvisibleBox("Bounds_East", parent, new Vector3(HalfX, extY, 0f), new Vector3(Thickness, extHeight, HalfZ * 2f), world);
        }

        internal static GameEntity ShadowDecal(Transform parent, MaterialSet mats, string objectName, string displayName, Vector3 position, int intangible)
        {
            var shadow = Primitive(PrimitiveType.Quad, objectName, parent, position, new Vector3(3f, 3f, 1f), mats.Shadow, intangible, keepCollider: false);
            shadow.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var slab = shadow.AddComponent<BoxCollider>();
            slab.center = Vector3.zero;
            slab.size = new Vector3(1f, 1f, 0.02f);
            AddEntity(shadow, displayName, EntityCategory.Decoration, EntityCapabilities.Inspectable | EntityCapabilities.CollisionTransferable,
                "A shadow. Harmless. Not solid. Yet.");
            AddCollision(shadow, CollisionMode.Intangible, mats);
            return shadow.GetComponent<GameEntity>();
        }

        internal static PressurePlate Plate(Transform parent, MaterialSet mats, string name, Vector3 position, float minMass = 5f)
        {
            int world = GameLayers.Index(GameLayers.World);
            var visual = Block(name, parent, position + new Vector3(0f, 0.05f, 0f), new Vector3(1f, 0.1f, 1f), mats.PlateIdle, world);
            // The plate is a sensor, not a step: a solid top would leave only ~1 cm of clearance when a
            // copy spawns next to a crate standing on it.
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            var sensor = new GameObject(name + "_Sensor", typeof(BoxCollider));
            sensor.transform.SetParent(parent, false);
            sensor.transform.position = position + new Vector3(0f, 0.5f, 0f);
            var box = sensor.GetComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(0.9f, 0.8f, 0.9f);
            sensor.layer = GameLayers.IgnoreRaycastLayer;
            var plate = sensor.AddComponent<PressurePlate>();
            plate.Configure(minMass, visual.GetComponent<Renderer>(), mats.PlateIdle, mats.PlatePressed);
            return plate;
        }

        internal static ObjectiveCondition Zone(Transform parent, string name, Vector3 center, Vector3 size, HeroController hero)
        {
            var go = new GameObject(name, typeof(BoxCollider));
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            go.layer = GameLayers.IgnoreRaycastLayer;
            var box = go.GetComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = size;
            var condition = go.AddComponent<HeroReachedZone>();
            condition.Configure(hero);
            return condition;
        }
    }
}
