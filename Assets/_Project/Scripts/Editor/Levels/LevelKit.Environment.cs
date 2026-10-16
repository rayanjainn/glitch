using System.Collections.Generic;
using BugReport.EditorTools.Setup;
using UnityEngine;
using static BugReport.EditorTools.Setup.TestRoomBuilder;

namespace BugReport.EditorTools.Levels
{
    /// <summary>
    /// Turns the gameplay primitives (floor slabs, wall blocks) into a dungeon hall: tiled floors, wall modules, torches with
    /// warm lights and banners. The primitives stay as invisible colliders, so gameplay never depends on the art; every model
    /// is placed from measured bounds. Without the CC0 dungeon pack the plain primitives remain.
    /// </summary>
    internal sealed partial class LevelKit
    {
        private readonly List<(GameObject block, bool dress)> wallBlocks = new List<(GameObject, bool)>();
        private readonly List<(float from, float to)> floorSegments = new List<(float, float)>();
        private readonly List<GameObject> floorBlocks = new List<GameObject>();

        /// <summary>A solid wall block (collider + stand-in visual). <paramref name="dress"/>: also receives torches and banners.</summary>
        private GameObject WallBlock(string name, Vector3 position, Vector3 size, bool dress = true)
        {
            var go = Block(name, Env, position, size, Mats.Wall, World);
            wallBlocks.Add((go, dress));
            return go;
        }

        private void DressArchitecture()
        {
            if (!ArtBuilder.DungeonAvailable) return;
            var material = ArtBuilder.AtlasMaterial("M_Dungeon", ArtBuilder.DungeonTexturePath);
            material.enableInstancing = true;
            var root = new GameObject("Architecture").transform;
            root.SetParent(Env, false);

            var floorSize = ArtBuilder.MeasureDungeonModel("floor_tile_large");
            var wallSize = ArtBuilder.MeasureDungeonModel("wall");
            if (floorSize.x < 0.2f || floorSize.z < 0.2f || wallSize.y < 0.2f || Mathf.Max(wallSize.x, wallSize.z) < 0.2f)
            {
                Debug.LogWarning("[BUG REPORT] Dungeon floor/wall models measured as empty; keeping plain architecture. Run 'git lfs pull' and reimport.");
                return;
            }

            DressFloors(root, material, floorSize);
            DressWalls(root, material, wallSize);
            LightTheHall(root, material);
        }

        private void DressFloors(Transform root, Material material, Vector3 tile)
        {
            for (int i = 0; i < floorSegments.Count; i++)
            {
                var (from, to) = floorSegments[i];
                float length = to - from;
                float depth = HalfZ * 2f;
                int nx = Mathf.Max(1, Mathf.RoundToInt(length / tile.x));
                int nz = Mathf.Max(1, Mathf.RoundToInt(depth / tile.z));
                float w = length / nx;
                float d = depth / nz;
                var scale = new Vector3(w / tile.x, (w / tile.x + d / tile.z) * 0.5f, d / tile.z);
                for (int ix = 0; ix < nx; ix++)
                {
                    for (int iz = 0; iz < nz; iz++)
                    {
                        var center = new Vector3(from + (ix + 0.5f) * w, 0f, -HalfZ + (iz + 0.5f) * d);
                        ArtBuilder.PlaceDungeonModel("floor_tile_large", root, center, 0f, 0f, scale, material);
                    }
                }
            }
            foreach (var floor in floorBlocks)
            {
                var r = floor.GetComponent<MeshRenderer>();
                if (r != null) r.enabled = false; // the collider stays
            }
        }

        private void DressWalls(Transform root, Material material, Vector3 module)
        {
            bool alongX = module.x >= module.z;                 // which local axis the wall runs along
            float moduleLength = alongX ? module.x : module.z;
            float moduleThickness = alongX ? module.z : module.x;
            int torchIndex = 0;

            foreach (var (block, dress) in wallBlocks)
            {
                var size = block.transform.localScale;
                var center = block.transform.position;
                bool runsAlongX = size.x >= size.z;
                float length = runsAlongX ? size.x : size.z;
                float thickness = runsAlongX ? size.z : size.x;
                if (length < 0.5f) continue;

                int n = Mathf.Max(1, Mathf.RoundToInt(length / moduleLength));
                float step = length / n;
                // Scale in the model's own axes: stretch along its long axis to fit the module count, squash the thin axis to the
                // collider thickness, fit the height. The quarter turn is only for blocks that run the other way than the model.
                var scale = alongX
                    ? new Vector3(step / moduleLength, size.y / module.y, thickness / moduleThickness)
                    : new Vector3(thickness / moduleThickness, size.y / module.y, step / moduleLength);
                float yaw = runsAlongX == alongX ? 0f : 90f;
                for (int i = 0; i < n; i++)
                {
                    float offset = -length * 0.5f + (i + 0.5f) * step;
                    var at = runsAlongX ? new Vector3(center.x + offset, 0f, center.z) : new Vector3(center.x, 0f, center.z + offset);
                    ArtBuilder.PlaceDungeonModel("wall", root, at, 0f, yaw, scale, material);
                }
                var r = block.GetComponent<MeshRenderer>();
                if (r != null) r.enabled = false; // the collider stays

                if (dress && length >= 8f) torchIndex = MountTorches(root, material, block, runsAlongX, length, torchIndex);
            }
        }

        /// <summary>Torches (and every other gap a banner) along the inner face of a long wall.</summary>
        private int MountTorches(Transform root, Material material, GameObject block, bool runsAlongX, float length, int torchIndex)
        {
            var center = block.transform.position;
            int count = Mathf.Max(1, Mathf.FloorToInt(length / 8f));
            float spacing = length / count;
            // Inward direction: towards the middle of the hall.
            Vector3 inward = runsAlongX ? new Vector3(0f, 0f, Mathf.Sign(-center.z)) : new Vector3(Mathf.Sign(-center.x), 0f, 0f);
            if (inward.sqrMagnitude < 0.1f) return torchIndex;
            float thickness = runsAlongX ? block.transform.localScale.z : block.transform.localScale.x;
            float yawFacing = Mathf.Atan2(inward.x, inward.z) * Mathf.Rad2Deg;

            for (int i = 0; i < count; i++)
            {
                float along = -length * 0.5f + (i + 0.5f) * spacing;
                var wallPoint = runsAlongX ? new Vector3(center.x + along, 0f, center.z) : new Vector3(center.x, 0f, center.z + along);
                var innerFace = wallPoint + inward * (thickness * 0.5f);
                if ((torchIndex + i) % 2 == 0)
                {
                    var torch = ArtBuilder.PlaceDungeonModel("torch_mounted", root, innerFace + inward * 0.15f, 1.7f, yawFacing, Vector3.one, material);
                    if (torch != null) AddTorchLight(root, innerFace + inward * 0.5f + Vector3.up * 2.3f);
                }
                else
                {
                    ArtBuilder.PlaceDungeonModel(BannerFor(Chapter), root, innerFace + inward * 0.05f, 1.0f, yawFacing, Vector3.one * 1.2f, material);
                }
            }
            return torchIndex + count;
        }

        private static string BannerFor(int chapter)
        {
            switch (chapter)
            {
                case 2: return "banner_green";
                case 3: return "banner_blue";
                case 4: return "banner_yellow";
                case 5: return "banner_brown";
                case 6: return "banner_patternA_green";
                case 7: return "banner_red";
                default: return "banner_blue";
            }
        }

        private static void AddTorchLight(Transform root, Vector3 position)
        {
            var go = new GameObject("TorchLight", typeof(Light));
            go.transform.SetParent(root, false);
            go.transform.position = position;
            var light = go.GetComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.62f, 0.3f);
            light.intensity = 5f;
            light.range = 7f;
            light.shadows = LightShadows.None;
        }

        /// <summary>A torch-lit hall wants a dimmer sun and ambient so the torches read.</summary>
        private void LightTheHall(Transform root, Material material)
        {
            var sun = RenderSettings.sun;
            if (sun != null) sun.intensity *= 0.7f;
            RenderSettings.ambientSkyColor *= 0.7f;
            RenderSettings.ambientEquatorColor *= 0.7f;
            RenderSettings.ambientGroundColor *= 0.7f;
        }
    }
}
