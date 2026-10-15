using System.Collections.Generic;
using BugReport.Containment;
using BugReport.Core.Entities;
using BugReport.EditorTools.Setup;
using BugReport.Glitches;
using BugReport.Hero;
using BugReport.Level;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using static BugReport.EditorTools.Setup.TestRoomBuilder;

namespace BugReport.EditorTools.Levels
{
    /// <summary>
    /// Building blocks for the procedurally built levels, so each level file reads like its design:
    /// a hall along +x with optional pits, firewalls, shadows, hologram bridges and doors, plates and
    /// gates, pistons, moving platforms, the exit, the hero's route and objectives. Gameplay objects
    /// are configured exactly like TRAINING LAB's (same components, layers and capabilities).
    /// </summary>
    internal sealed partial class LevelKit
    {
        public const float WallHeight = 4.5f;
        public const float Thickness = 0.4f;
        public const float Ceiling = 6f;

        public MaterialSet Mats;
        public Transform Env;
        public Scene Scene;
        public int World;
        public int Intangible;
        public InputActionAsset Actions;
        public CollisionTransferDefinition CollisionTransfer;
        public DuplicationDefinition Duplication;
        public FreezeDefinition Freeze;
        public ContainmentSettingsAsset Containment;
        public GameObject CratePrefab;
        public PresentationBuilder.Assets Presentation;
        public float HalfX;
        public float HalfZ;
        public HeroController Hero;
        public HeroNavNode Goal;

        private readonly List<HeroNavNode> nodes = new List<HeroNavNode>();

        // ---------------------------------------------------------------- start / finish

        /// <summary>New empty scene with every shared asset loaded (after NewScene, see TestRoomBuilder).</summary>
        public static LevelKit Begin(out string error, int chapter = 1)
        {
            error = null;
            EnsureFolder(LevelCatalogBuilder.ScenesFolder);
            var kit = new LevelKit { Chapter = chapter, Scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single) };
            kit.Mats = new MaterialSet();
            if (!kit.Mats.Create(out error)) return null;
            kit.Actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (kit.Actions == null)
            {
                error = "Input actions not found at " + InputActionsPath;
                return null;
            }
            kit.CollisionTransfer = LoadOrCreate<CollisionTransferDefinition>(CollisionTransferPath, d => d.ApplyDefaults());
            kit.Duplication = LoadOrCreate<DuplicationDefinition>(DuplicationPath, d => d.ApplyDefaults());
            kit.Freeze = LoadOrCreate<FreezeDefinition>(FreezePath, d => d.ApplyDefaults());
            var crateProfile = LoadOrCreate<DuplicationProfile>(CrateProfilePath, p => p.Configure(null, 2));
            kit.Containment = LoadOrCreate<ContainmentSettingsAsset>(ContainmentSettingsPath, _ => { });
            kit.CratePrefab = CreateCratePrefab(kit.Mats, crateProfile);
            if (kit.CratePrefab == null)
            {
                error = "Could not save " + CratePrefabPath;
                return null;
            }
            kit.Presentation = PresentationBuilder.LoadOrCreateAssets(kit.Mats);
            kit.World = GameLayers.Index(GameLayers.World);
            kit.Intangible = GameLayers.Index(GameLayers.Intangible);
            BuildLighting();
            ChapterTheme.Apply(chapter, RenderSettings.sun);
            kit.Env = new GameObject("--- ENVIRONMENT ---").transform;
            return kit;
        }

        /// <summary>Systems, player, presentation, validation, save, catalog/build order.</summary>
        public bool Finish(string scenePath, string title, GlitchDefinition[] glitches, ObjectiveDirector objectives,
            Vector3 anomalyStart, out string error)
        {
            error = null;
            var anomaly = BuildAnomaly(Mats, Actions, out var motor);
            anomaly.transform.position = anomalyStart;
            var cameraRig = BuildCamera(anomaly.transform, out var scanner);
            var systems = BuildSystems(Mats, scanner, motor, Hero, glitches, Containment, objectives, debugOverlayVisible: false, levelTitle: title);
            WirePlayer(anomaly, motor, cameraRig, systems);
            foreach (var configure in deferred) configure(anomaly.transform, systems.Containment);
            Surroundings(scenePath);
            PresentationBuilder.Apply(Presentation, Mats, systems, anomaly, Hero, cameraRig.GetComponent<Camera>());

            var missing = FindUnassignedReferences(Scene);
            if (missing.Count > 0)
            {
                error = "Scene NOT saved; unassigned references:\n  " + string.Join("\n  ", missing);
                return false;
            }
            if (!EditorSceneManager.SaveScene(Scene, scenePath))
            {
                error = "Could not save " + scenePath;
                return false;
            }
            AddToBuildSettings(scenePath);
            LevelCatalogBuilder.SyncBuildSettings();
            return true;
        }

        // ---------------------------------------------------------------- architecture

        /// <summary>
        /// Floor x −halfX…halfX, z −halfZ…halfZ with pits (no floor) over the given x ranges; four walls
        /// and invisible bounds up to the ceiling. Pits are bottomless: anything that falls in is caught
        /// by the kill plane (props and the hero respawn). A dark plane far below sells the depth.
        /// </summary>
        public void Hall(float halfX, float halfZ, params (float from, float to)[] pits)
        {
            HalfX = halfX;
            HalfZ = halfZ;
            float x = -halfX;
            int index = 0;
            var cuts = new List<(float from, float to)>(pits);
            cuts.Sort((a, b) => a.from.CompareTo(b.from));
            foreach (var pit in cuts)
            {
                FloorSegment("Floor_" + index++, x, pit.from);
                x = pit.to;
                var abyss = Primitive(PrimitiveType.Quad, "Abyss", Env, new Vector3((pit.from + pit.to) * 0.5f, -14f, 0f),
                    new Vector3(pit.to - pit.from + 2f, halfZ * 2f, 1f), Mats.Shadow, World, keepCollider: false);
                abyss.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            }
            FloorSegment("Floor_" + index, x, halfX);

            float y = WallHeight * 0.5f;
            Block("Wall_North", Env, new Vector3(0f, y, halfZ), new Vector3(halfX * 2f, WallHeight, Thickness), Mats.Wall, World);
            Block("Wall_South", Env, new Vector3(0f, y, -halfZ), new Vector3(halfX * 2f, WallHeight, Thickness), Mats.Wall, World);
            Block("Wall_West", Env, new Vector3(-halfX, y, 0f), new Vector3(Thickness, WallHeight, halfZ * 2f), Mats.Wall, World);
            Block("Wall_East", Env, new Vector3(halfX, y, 0f), new Vector3(Thickness, WallHeight, halfZ * 2f), Mats.Wall, World);

            float lid = Thickness * 2f;
            InvisibleBox("Bounds_Ceiling", Env, new Vector3(0f, Ceiling, 0f), new Vector3(halfX * 2f + lid, 0.2f, halfZ * 2f + lid), World);
            float extHeight = Ceiling - WallHeight + 0.2f;
            float extY = WallHeight + extHeight * 0.5f - 0.1f;
            InvisibleBox("Bounds_North", Env, new Vector3(0f, extY, halfZ), new Vector3(halfX * 2f, extHeight, Thickness), World);
            InvisibleBox("Bounds_South", Env, new Vector3(0f, extY, -halfZ), new Vector3(halfX * 2f, extHeight, Thickness), World);
            InvisibleBox("Bounds_West", Env, new Vector3(-halfX, extY, 0f), new Vector3(Thickness, extHeight, halfZ * 2f), World);
            InvisibleBox("Bounds_East", Env, new Vector3(halfX, extY, 0f), new Vector3(Thickness, extHeight, halfZ * 2f), World);
        }

        private void FloorSegment(string name, float fromX, float toX)
        {
            if (toX - fromX < 0.01f) return;
            var floor = Block(name, Env, new Vector3((fromX + toX) * 0.5f, -0.1f, 0f), new Vector3(toX - fromX, 0.2f, HalfZ * 2f), Mats.Floor, World);
            floor.GetComponent<MeshRenderer>().sharedMaterial = PresentationBuilder.GridFloor(toX - fromX, HalfZ * 2f);
        }

        /// <summary>A wall across the hall at <paramref name="x"/> with a doorway of <paramref name="doorWidth"/> around z = 0.</summary>
        public void Divider(string name, float x, float doorWidth)
        {
            float half = doorWidth * 0.5f;
            float side = HalfZ - half;
            Block(name + "_N", Env, new Vector3(x, WallHeight * 0.5f, half + side * 0.5f), new Vector3(Thickness, WallHeight, side), Mats.Wall, World);
            Block(name + "_S", Env, new Vector3(x, WallHeight * 0.5f, -half - side * 0.5f), new Vector3(Thickness, WallHeight, side), Mats.Wall, World);
        }

        // ---------------------------------------------------------------- glitchable objects

        /// <summary>Solid, collision-transferable firewall across the full hall width at <paramref name="x"/>.</summary>
        public GameEntity Firewall(string name, string displayName, float x)
        {
            var go = Block(name, Env, new Vector3(x, 1.5f, 0f), new Vector3(0.5f, 3f, HalfZ * 2f - Thickness), Mats.Firewall, World);
            AddEntity(go, displayName, EntityCategory.Structure, EntityCapabilities.Inspectable | EntityCapabilities.CollisionTransferable,
                "Blocks the way. Its collision looks... negotiable.");
            AddCollision(go, CollisionMode.Solid, Mats);
            return go.GetComponent<GameEntity>();
        }

        /// <summary>Solid, static, collision-transferable leftover wall: a source of solidity.</summary>
        public GameEntity Barrier(string name, string displayName, Vector3 center, Vector3 size, string description)
        {
            var go = Block(name, Env, center, size, Mats.Firewall, World);
            AddEntity(go, displayName, EntityCategory.Structure, EntityCapabilities.Inspectable | EntityCapabilities.CollisionTransferable, description);
            AddCollision(go, CollisionMode.Solid, Mats);
            return go.GetComponent<GameEntity>();
        }

        public GameEntity Shadow(string name, string displayName, Vector3 position)
            => SliceLevelBuilder.ShadowDecal(Env, Mats, name, displayName, position, Intangible);

        /// <summary>
        /// Hologram walkway over a pit from <paramref name="fromX"/> to <paramref name="toX"/>: intangible
        /// (no floor for the hero) until Collision Transfer makes it solid.
        /// </summary>
        public GameEntity HoloBridge(string name, string displayName, float fromX, float toX, float width)
        {
            float length = toX - fromX + 0.4f; // overlaps both edges so there is no crack
            var go = Block(name, Env, new Vector3((fromX + toX) * 0.5f, -0.12f, 0f), new Vector3(length, 0.2f, width), Mats.Firewall, Intangible);
            AddEntity(go, displayName, EntityCategory.Structure, EntityCapabilities.Inspectable | EntityCapabilities.CollisionTransferable,
                "A bridge in every way except the one that matters.");
            AddCollision(go, CollisionMode.Intangible, Mats);
            return go.GetComponent<GameEntity>();
        }

        /// <summary>Hologram filling a doorway: harmless while intangible, a wall if it becomes solid.</summary>
        public GameEntity HoloDoor(string name, string displayName, float x, float width)
        {
            var go = Block(name, Env, new Vector3(x, 1.5f, 0f), new Vector3(0.3f, 3f, width), Mats.Firewall, Intangible);
            AddEntity(go, displayName, EntityCategory.Structure, EntityCapabilities.Inspectable | EntityCapabilities.CollisionTransferable,
                "A projected door. Decorative. For now.");
            AddCollision(go, CollisionMode.Intangible, Mats);
            return go.GetComponent<GameEntity>();
        }

        public PressurePlate Plate(string name, Vector3 position) => SliceLevelBuilder.Plate(Env, Mats, name, position);

        public GameEntity Crate(string name, string displayName, Vector3 floorPosition, string description)
            => CrateInstance(CratePrefab, Env, name, displayName, description, floorPosition + Vector3.up * 0.62f, Quaternion.identity);

        /// <summary>Divider at <paramref name="x"/> whose doorway is closed by a gate that sinks when all plates are pressed.</summary>
        public GameEntity Gate(string name, float x, PressurePlate[] plates, bool transferable = false)
        {
            const float doorWidth = 3.2f;
            Divider(name + "Wall", x, doorWidth);
            var gate = Block(name, Env, new Vector3(x, 1.5f, 0f), new Vector3(0.45f, 3f, doorWidth), Mats.Gate, World);
            var body = gate.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            // Not transferable by default: giving the gate's solidity away would skip the plates entirely.
            AddEntity(gate, "SECURITY_GATE", EntityCategory.Structure,
                transferable ? EntityCapabilities.Inspectable | EntityCapabilities.CollisionTransferable : EntityCapabilities.Inspectable,
                "Opens when all of its pressure plates are weighed down.");
            if (transferable) AddCollision(gate, CollisionMode.Solid, Mats);
            gate.AddComponent<GateController>().Configure(plates, new Vector3(0f, -3.2f, 0f), 1.2f);
            return gate.GetComponent<GameEntity>();
        }

        /// <summary>
        /// A 4 m corridor centred on <paramref name="x"/> (dividers at x ± 2) with a piston slamming across it
        /// from the south. It starts retracted (phase 0) unless <paramref name="startPhase"/> says otherwise.
        /// </summary>
        public GameEntity Piston(string name, string displayName, float x, float period, float startPhase, bool transferable = false)
        {
            const float doorHalf = 1.6f;
            Divider(name + "_DividerW", x - 2f, doorHalf * 2f);
            Divider(name + "_DividerE", x + 2f, doorHalf * 2f);
            Block(name + "_CorridorN", Env, new Vector3(x, WallHeight * 0.5f, doorHalf + 0.2f), new Vector3(4f, WallHeight, 0.4f), Mats.Wall, World);
            Block(name + "_CorridorSW", Env, new Vector3(x - 1.3f, WallHeight * 0.5f, -doorHalf - 0.2f), new Vector3(1.4f, WallHeight, 0.4f), Mats.Wall, World);
            Block(name + "_CorridorSE", Env, new Vector3(x + 1.3f, WallHeight * 0.5f, -doorHalf - 0.2f), new Vector3(1.4f, WallHeight, 0.4f), Mats.Wall, World);

            var piston = Block(name, Env, new Vector3(x, 1.5f, -3.4f), new Vector3(1f, 3f, 3.2f), Mats.Piston, World);
            var body = piston.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            AddEntity(piston, displayName, EntityCategory.SystemDevice,
                transferable
                    ? EntityCapabilities.Inspectable | EntityCapabilities.Freezable | EntityCapabilities.CollisionTransferable
                    : EntityCapabilities.Inspectable | EntityCapabilities.Freezable,
                "Slams across the corridor on a timer. The hero will not walk into it while it moves.");
            piston.AddComponent<PingPongMover>().Configure(new Vector3(0f, 0f, 3.4f), period, startPhase);
            AddFreeze(piston, Mats);
            if (transferable) AddCollision(piston, CollisionMode.Solid, Mats);
            Hazard(piston, new Vector3(x, 1.5f, 0f), new Vector3(1.6f, 3f, doorHalf * 2f));
            return piston.GetComponent<GameEntity>();
        }

        /// <summary>
        /// A platform spanning the pit <paramref name="fromX"/>…<paramref name="toX"/>, sliding along z between
        /// the hero's path (z = 0, phase 0) and <paramref name="travelZ"/>. The hero only crosses on it while
        /// it is frozen in line with the path (hazard + ground check).
        /// </summary>
        public GameEntity MovingPlatform(string name, string displayName, float fromX, float toX, float travelZ, float period, float startPhase)
        {
            const float width = 3.2f; // wide enough that the hero's whole capsule is supported while it lines up
            float length = toX - fromX + 0.3f;
            var platform = Block(name, Env, new Vector3((fromX + toX) * 0.5f, -0.12f, 0f), new Vector3(length, 0.2f, width), Mats.Lift, World);
            var body = platform.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            AddEntity(platform, displayName, EntityCategory.SystemDevice, EntityCapabilities.Inspectable | EntityCapabilities.Freezable,
                "Shuttles back and forth over the pit. The hero won't step onto something that moves.");
            platform.AddComponent<PingPongMover>().Configure(new Vector3(0f, 0f, travelZ), period, startPhase);
            AddFreeze(platform, Mats);
            float zMin = Mathf.Min(0f, travelZ) - width * 0.5f;
            float zMax = Mathf.Max(0f, travelZ) + width * 0.5f;
            Hazard(platform, new Vector3((fromX + toX) * 0.5f, 1f, (zMin + zMax) * 0.5f), new Vector3(toX - fromX, 2f, zMax - zMin));
            return platform.GetComponent<GameEntity>();
        }

        private void Hazard(GameObject machine, Vector3 center, Vector3 size)
        {
            var zone = new GameObject(machine.name + "_DangerZone", typeof(BoxCollider));
            zone.transform.SetParent(Env, false);
            zone.transform.position = center;
            zone.layer = GameLayers.IgnoreRaycastLayer;
            var box = zone.GetComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = size;
            machine.AddComponent<HeroHazard>().Configure(box);
        }

        // ---------------------------------------------------------------- exit, hero, objectives

        public void Exit(float x)
        {
            var root = new GameObject("ExitDoor");
            root.transform.SetParent(Env, false);
            root.transform.position = new Vector3(x, 0f, 0f);
            Block("Pad", root.transform, new Vector3(x, 0.05f, 0f), new Vector3(1.6f, 0.1f, 2.6f), Mats.Exit, World);
            Block("Post_L", root.transform, new Vector3(x + 1.2f, 1.4f, -1.4f), new Vector3(0.25f, 2.8f, 0.25f), Mats.Exit, World);
            Block("Post_R", root.transform, new Vector3(x + 1.2f, 1.4f, 1.4f), new Vector3(0.25f, 2.8f, 0.25f), Mats.Exit, World);
            Block("Lintel", root.transform, new Vector3(x + 1.2f, 2.9f, 0f), new Vector3(0.25f, 0.25f, 3.05f), Mats.Exit, World);
            AddEntity(root, "EXIT_DOOR", EntityCategory.Objective, EntityCapabilities.Inspectable | EntityCapabilities.ObjectiveRelevant,
                "Level objective. The hero wants to be here.");
        }

        /// <summary>
        /// The hero at <paramref name="start"/> facing +x, following a chain of nodes (feet positions) to the
        /// last one. With <paramref name="requireGround"/> every edge needs solid floor under it.
        /// </summary>
        public HeroController HeroOnRoute(Vector3 start, bool requireGround, params Vector3[] route)
        {
            var heroGo = CreateHeroBody(Mats, start + Vector3.up * 1.05f, 90f, out var cc);
            var graphRoot = new GameObject("HeroNavGraph");
            nodes.Clear();
            nodes.Add(NavNode(graphRoot.transform, "N_Start", start));
            for (int i = 0; i < route.Length; i++) nodes.Add(NavNode(graphRoot.transform, "N_" + i, route[i]));
            for (int i = 0; i < nodes.Count - 1; i++) nodes[i].Configure(nodes[i + 1]);
            var graph = graphRoot.AddComponent<HeroNavGraph>();
            graph.Configure(nodes.ToArray(), cc.radius, cc.height, requireGround);
            Goal = nodes[nodes.Count - 1];
            Hero = heroGo.AddComponent<HeroController>();
            Hero.Configure(graph, Goal, newStartDelay: 2f);
            return Hero;
        }

        /// <summary>Hero-reached zone spanning the hall's width between two x positions.</summary>
        public ObjectiveCondition ZoneBetween(string name, float fromX, float toX)
            => SliceLevelBuilder.Zone(ObjectivesRoot, name, new Vector3((fromX + toX) * 0.5f, 1.5f, 0f), new Vector3(toX - fromX, 3f, HalfZ * 2f), Hero);

        public ObjectiveCondition ZoneAtExit(float x)
            => SliceLevelBuilder.Zone(ObjectivesRoot, "Zone_Exit", new Vector3(x, 1.5f, 0f), new Vector3(2.4f, 3f, 3f), Hero);

        private Transform objectivesRoot;
        private Transform ObjectivesRoot => objectivesRoot != null ? objectivesRoot : (objectivesRoot = new GameObject("--- OBJECTIVES ---").transform);

        public ObjectiveDirector Objectives(params ObjectiveDirector.Entry[] entries)
        {
            var director = ObjectivesRoot.gameObject.AddComponent<ObjectiveDirector>();
            director.Configure(entries);
            return director;
        }

        /// <summary>A few dungeon props against the side walls (skipped without the CC0 art).</summary>
        public void Dress(params (string model, float x, float z, float yaw, float height)[] props)
        {
            if (!ArtBuilder.DungeonAvailable) return;
            var material = ArtBuilder.AtlasMaterial("M_Dungeon", ArtBuilder.DungeonTexturePath);
            var root = new GameObject("Decor").transform;
            root.SetParent(Env, false);
            foreach (var p in props) ArtBuilder.Decor(p.model, root, new Vector3(p.x, 0f, p.z), p.yaw, p.height, material, World);
        }
    }
}
