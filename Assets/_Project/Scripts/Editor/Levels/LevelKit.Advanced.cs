using System;
using System.Collections.Generic;
using BugReport.Containment;
using BugReport.Core.Entities;
using BugReport.EditorTools.Setup;
using BugReport.Glitches;
using BugReport.Guards;
using BugReport.Level;
using UnityEditor;
using UnityEngine;
using static BugReport.EditorTools.Setup.TestRoomBuilder;

namespace BugReport.EditorTools.Levels
{
    /// <summary>
    /// Building blocks for the second half of the game: weights for Property Swap, guards and decoys
    /// for AI Override, false walls and floors for Desync, security drones, and the scenery around
    /// the hall. Same rules as the rest of the kit: gameplay components on the gameplay objects,
    /// models as child visuals only.
    /// </summary>
    internal sealed partial class LevelKit
    {
        /// <summary>Story chapter (1…7): picks the sky, lighting and the scenery kit.</summary>
        public int Chapter = 1;

        public const string MassSwapPath = GlitchFolder + "/GL_MassSwap.asset";
        public const string AiOverridePath = GlitchFolder + "/GL_AiOverride.asset";
        public const string DesyncPath = GlitchFolder + "/GL_Desync.asset";

        private PropertySwapDefinition massSwap;
        private AiOverrideDefinition aiOverride;
        private DesyncDefinition desync;

        public PropertySwapDefinition MassSwap => massSwap != null ? massSwap
            : massSwap = LoadOrCreate<PropertySwapDefinition>(MassSwapPath, d => d.ApplyDefaults(BugReport.Core.Properties.SwapProperty.Mass));
        public AiOverrideDefinition AiOverride => aiOverride != null ? aiOverride
            : aiOverride = LoadOrCreate<AiOverrideDefinition>(AiOverridePath, d => d.ApplyDefaults());
        public DesyncDefinition Desync => desync != null ? desync
            : desync = LoadOrCreate<DesyncDefinition>(DesyncPath, d => d.ApplyDefaults());

        private readonly List<Action<Transform, ContainmentDirector>> deferred = new List<Action<Transform, ContainmentDirector>>();

        // ---------------------------------------------------------------- Property Swap

        /// <summary>Pressure plate that needs <paramref name="minMass"/> to trigger.</summary>
        public PressurePlate HeavyPlate(string name, Vector3 position, float minMass)
            => SliceLevelBuilder.Plate(Env, Mats, name, position, minMass);

        /// <summary>A heavy prop for Mass Swap: swapping with a light crate makes the crate press heavy plates.</summary>
        public GameEntity Weight(string name, string displayName, Vector3 floorPosition, float mass, float size, string description)
        {
            var go = Block(name, Env, floorPosition + Vector3.up * (size * 0.5f), new Vector3(size, size, size), Mats.Anvil, Props());
            AddBody(go, mass);
            AddEntity(go, displayName, EntityCategory.Prop,
                EntityCapabilities.Inspectable | EntityCapabilities.PropertyMutable | EntityCapabilities.Freezable, description);
            go.AddComponent<PropertyCapability>();
            AddFreeze(go, Mats);
            return go.GetComponent<GameEntity>();
        }

        private static int Props() => GameLayers.Index(GameLayers.Props);

        // ---------------------------------------------------------------- AI Override

        /// <summary>A decoy a guard can be lured to: any inspectable prop will do.</summary>
        public GameEntity Decoy(string name, string displayName, Vector3 floorPosition)
        {
            var go = Block(name, Env, floorPosition + Vector3.up * 0.6f, new Vector3(0.9f, 1.2f, 0.9f), Mats.Crate, Props());
            AddEntity(go, displayName, EntityCategory.Prop, EntityCapabilities.Inspectable,
                "Something shiny. Guards cannot resist investigating it.");
            ArtBuilder.SkinWithDungeonModel(go, "barrel_large", ArtBuilder.DungeonAvailable
                ? ArtBuilder.AtlasMaterial("M_Dungeon", ArtBuilder.DungeonTexturePath) : null);
            return go.GetComponent<GameEntity>();
        }

        /// <summary>
        /// A skeleton guard standing in a doorway at <paramref name="doorX"/>, pacing along z by ±<paramref name="pace"/>.
        /// The hero waits while the guard's danger zone is unsafe: patrolling, not lured, not frozen.
        /// </summary>
        public GuardController GuardPost(string name, string displayName, float doorX, float pace = 0.9f, string model = "Skeleton_Warrior")
        {
            Divider(name + "_Door", doorX, 3.2f);
            var root = new GameObject(name) { layer = Props() };
            root.transform.SetParent(Env, false);
            root.transform.SetPositionAndRotation(new Vector3(doorX, 0f, -pace), Quaternion.Euler(0f, 270f, 0f));

            var capsule = root.AddComponent<CapsuleCollider>();
            capsule.radius = 0.4f;
            capsule.height = 1.9f;
            capsule.center = new Vector3(0f, 0.95f, 0f);
            var body = root.AddComponent<Rigidbody>();
            body.isKinematic = true;
            AddEntity(root, displayName, EntityCategory.Enemy,
                EntityCapabilities.Inspectable | EntityCapabilities.AIControllable | EntityCapabilities.Freezable,
                "A security skeleton. It walks its beat, and it will not let a hero through.");

            Animator animator;
            if (ArtBuilder.BuildGuardModel(root.transform, model, 1.9f, Props(), out animator) == null)
            {
                animator = null;
                var visual = Primitive(PrimitiveType.Capsule, "Body", root.transform, root.transform.position + Vector3.up * 0.95f,
                    new Vector3(0.8f, 0.95f, 0.8f), Mats.GuardBody, Props(), keepCollider: false);
                visual.transform.localRotation = Quaternion.identity;
            }
            AddFreeze(root, Mats);

            var zone = new GameObject("DangerZone", typeof(BoxCollider));
            zone.transform.SetParent(root.transform, false);
            zone.transform.localPosition = new Vector3(0f, 1f, 0f);
            zone.layer = GameLayers.IgnoreRaycastLayer;
            var box = zone.GetComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(3.4f, 2.2f, 3.4f);

            var a = new GameObject(name + "_PatrolA").transform;
            var b = new GameObject(name + "_PatrolB").transform;
            a.SetParent(Env, false);
            b.SetParent(Env, false);
            a.position = new Vector3(doorX, 0f, pace);
            b.position = new Vector3(doorX, 0f, -pace);

            var guard = root.AddComponent<GuardController>();
            var hazard = root.AddComponent<HeroHazard>();
            hazard.Configure(box);
            var hero = Hero;
            var route = new[] { a, b };
            deferred.Add((anomaly, containment) => guard.Configure(route, hero, anomaly, containment, animator));
            return guard;
        }

        // ---------------------------------------------------------------- Desync

        /// <summary>
        /// A wall across the whole hall at <paramref name="x"/>. Its looks stay; Desync lifts its collision up
        /// out of the hero's way (the hero walks through the picture of a wall). Everything but the collision
        /// is a child visual with no collider.
        /// </summary>
        public GameEntity FalseWall(string name, string displayName, float x)
        {
            float width = HalfZ * 2f - Thickness;
            var root = new GameObject(name);
            root.transform.SetParent(Env, false);
            root.transform.position = new Vector3(x, 0f, 0f);

            Primitive(PrimitiveType.Cube, "Visual", root.transform, new Vector3(x, 1.3f, 0f), new Vector3(0.5f, 2.6f, width),
                Mats.DesyncSurface, World, keepCollider: false);

            var collision = new GameObject("Collision", typeof(BoxCollider));
            collision.transform.SetParent(root.transform, false);
            collision.transform.position = new Vector3(x, 1.2f, 0f);
            collision.layer = World;
            collision.GetComponent<BoxCollider>().size = new Vector3(0.5f, 2.4f, width);

            AddEntity(root, displayName, EntityCategory.Structure, EntityCapabilities.Inspectable | EntityCapabilities.Desyncable,
                "A wall whose collision has come apart from its looks.");
            // 3.4 up: the collision floats clear of the hero (2 m) and still fits under the ceiling box.
            root.AddComponent<DesyncCapability>().Configure(collision.transform, new Vector3(0f, 3.4f, 0f), Mats.Line);
            return root.GetComponent<GameEntity>();
        }

        /// <summary>
        /// A floor plate lying at <paramref name="restX"/>. Desync slides its collision sideways by
        /// <paramref name="shift"/> to bridge a pit while the plate still looks like it is lying there.
        /// </summary>
        public GameEntity FalseFloor(string name, string displayName, float restX, float length, float shift)
        {
            const float width = 3.2f;
            var root = new GameObject(name);
            root.transform.SetParent(Env, false);
            root.transform.position = new Vector3(restX, 0f, 0f);

            Primitive(PrimitiveType.Cube, "Visual", root.transform, new Vector3(restX, -0.08f, 0f), new Vector3(length, 0.2f, width),
                Mats.DesyncSurface, World, keepCollider: false);

            var collision = new GameObject("Collision", typeof(BoxCollider));
            collision.transform.SetParent(root.transform, false);
            collision.transform.position = new Vector3(restX, -0.1f, 0f);
            collision.layer = World;
            collision.GetComponent<BoxCollider>().size = new Vector3(length, 0.2f, width);

            AddEntity(root, displayName, EntityCategory.Structure, EntityCapabilities.Inspectable | EntityCapabilities.Desyncable,
                "A maintenance plate. What it looks like and where it is solid do not agree.");
            root.AddComponent<DesyncCapability>().Configure(collision.transform, new Vector3(shift, 0f, 0f), Mats.Line);
            return root.GetComponent<GameEntity>();
        }

        // ---------------------------------------------------------------- security drones

        /// <summary>A drone patrolling the given (x, z) points at <paramref name="height"/>, searchlight on the floor.</summary>
        public SecurityDrone Drone(string name, string displayName, float height, params Vector2[] route)
        {
            var root = new GameObject(name) { layer = Props() };
            root.transform.SetParent(Env, false);
            root.transform.position = new Vector3(route[0].x, height, route[0].y);

            var sphere = root.AddComponent<SphereCollider>();
            sphere.radius = 0.4f;
            root.AddComponent<Rigidbody>().isKinematic = true;
            AddEntity(root, displayName, EntityCategory.Enemy, EntityCapabilities.Inspectable | EntityCapabilities.Freezable,
                "A security drone. Anything inside its light is reported to the system.");
            Primitive(PrimitiveType.Sphere, "Body", root.transform, root.transform.position, new Vector3(0.8f, 0.5f, 0.8f),
                Mats.Drone, Props(), keepCollider: false);
            AddFreeze(root, Mats);

            var disc = Primitive(PrimitiveType.Cylinder, "SearchLight", Env, new Vector3(route[0].x, 0.04f, route[0].y),
                new Vector3(6.4f, 0.01f, 6.4f), Mats.SearchLight, 0, keepCollider: false);
            disc.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var points = new Transform[route.Length];
            for (int i = 0; i < route.Length; i++)
            {
                var p = new GameObject(name + "_Route" + i).transform;
                p.SetParent(Env, false);
                p.position = new Vector3(route[i].x, height, route[i].y);
                points[i] = p;
            }
            var drone = root.AddComponent<SecurityDrone>();
            var discTransform = disc.transform;
            var discRenderer = disc.GetComponent<MeshRenderer>();
            deferred.Add((anomaly, containment) => drone.Configure(points, anomaly, containment, discTransform, discRenderer));
            return drone;
        }

        // ---------------------------------------------------------------- scenery

        private static readonly (string model, float height)[] NatureProps =
        {
            ("trees_A_large", 7f), ("tree_single_A", 6f), ("rock_single_A", 2.2f), ("trees_B_medium", 6f), ("rock_single_C", 2f),
            ("hill_single_A", 4f), ("trees_A_medium", 5f), ("rock_single_E", 2.4f),
        };
        private static readonly (string model, float height)[] PrototypeProps =
        {
            ("Pillar_A", 5f), ("Barrel_A", 1.4f), ("Box_A", 1.6f), ("Cube_Prototype_Large_A", 3f), ("target_stand_A", 2.4f),
            ("table_medium", 1.4f), ("Pillar_B", 5f),
        };
        private static readonly (string model, float height)[] SpaceProps =
        {
            ("cargo_A_stacked", 3f), ("containers_A", 3.5f), ("cargodepot_A", 4f), ("structure_low", 4f), ("solarpanel", 2.6f),
            ("windturbine_low", 7f), ("rocks_A", 2.2f), ("containers_C", 3.5f), ("structure_tall", 9f),
        };
        private static readonly (string model, float height)[] GraveyardProps =
        {
            ("tree_dead_large", 8f), ("gravestone", 1.6f), ("crypt", 5f), ("lantern_standing", 2.4f), ("pillar", 4f),
            ("tree_pine_orange_large", 7f), ("grave_A", 1.8f), ("shrine_candles", 2.6f), ("fence", 1.6f),
        };

        private static (ArtBuilder.Kit kit, (string model, float height)[] props) SceneryFor(int chapter)
        {
            switch (chapter)
            {
                case 3: return (ArtBuilder.Kit.Prototype, PrototypeProps);
                case 4:
                case 5: return (ArtBuilder.Kit.Space, SpaceProps);
                case 6:
                case 7: return (ArtBuilder.Kit.Graveyard, GraveyardProps);
                default: return (ArtBuilder.Kit.Nature, NatureProps);
            }
        }

        /// <summary>
        /// Dresses the area around the hall with the chapter's scenery: two staggered rows along each long side
        /// and a few props at both ends. Static, collider-free, deterministic per scene. Does nothing when the
        /// pack has not been fetched.
        /// </summary>
        private void Surroundings(string seedText)
        {
            var (kit, props) = SceneryFor(Chapter);
            if (!ArtBuilder.KitAvailable(kit)) return;
            var random = new System.Random(StableHash(seedText));
            var root = new GameObject("Scenery").transform;
            root.SetParent(Env, false);

            for (int side = -1; side <= 1; side += 2)
            {
                for (float x = -HalfX - 4f; x <= HalfX + 4f; x += 6.5f)
                {
                    var (model, height) = props[random.Next(props.Length)];
                    float z = side * (HalfZ + 3.5f + (float)random.NextDouble() * 3f);
                    ArtBuilder.Scenery(kit, model, root, new Vector3(x + (float)random.NextDouble() * 2f, 0f, z),
                        (float)random.NextDouble() * 360f, height * (0.85f + (float)random.NextDouble() * 0.3f));
                }
                for (float x = -HalfX - 2f; x <= HalfX + 2f; x += 13f)
                {
                    var (model, height) = props[random.Next(props.Length)];
                    ArtBuilder.Scenery(kit, model, root, new Vector3(x, 0f, side * (HalfZ + 11f)), (float)random.NextDouble() * 360f, height * 1.4f);
                }
            }
            for (int end = -1; end <= 1; end += 2)
            {
                var (model, height) = props[random.Next(props.Length)];
                ArtBuilder.Scenery(kit, model, root, new Vector3(end * (HalfX + 5f), 0f, (float)random.NextDouble() * 4f - 2f),
                    (float)random.NextDouble() * 360f, height);
            }
        }

        private static int StableHash(string text)
        {
            unchecked
            {
                int hash = 17;
                foreach (char c in text ?? string.Empty) hash = hash * 31 + c;
                return hash;
            }
        }
    }
}
