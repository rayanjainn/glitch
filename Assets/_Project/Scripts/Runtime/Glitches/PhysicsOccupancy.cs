using System.Collections.Generic;
using UnityEngine;

namespace BugReport.Glitches
{
    /// <summary>
    /// "Is anything standing inside this volume?" Used before a collider becomes solid,
    /// so nothing is ever embedded in geometry (which makes PhysX/CharacterController
    /// eject it violently or trap it).
    /// </summary>
    public static class PhysicsOccupancy
    {
        /// <summary>Volumes are shrunk by this much so merely touching (standing next to a wall) is not "inside".</summary>
        public const float ContactMargin = 0.02f;

        private static readonly Collider[] Hits = new Collider[32];

        public static bool IsOccupied(IReadOnlyList<Collider> volume, int occupantMask, out Collider occupant)
        {
            occupant = null;
            if (volume == null) return false;

            for (int i = 0; i < volume.Count; i++)
            {
                var c = volume[i];
                if (c == null) continue;

                int count;
                if (c is BoxCollider box)
                {
                    var t = box.transform;
                    var scale = t.lossyScale;
                    var size = Vector3.Scale(box.size, new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
                    count = Physics.OverlapBoxNonAlloc(t.TransformPoint(box.center), Shrink(size * 0.5f), Hits, t.rotation, occupantMask, QueryTriggerInteraction.Ignore);
                }
                else
                {
                    // Conservative fallback for non-box shapes: world AABB.
                    var b = c.bounds;
                    count = Physics.OverlapBoxNonAlloc(b.center, Shrink(b.extents), Hits, Quaternion.identity, occupantMask, QueryTriggerInteraction.Ignore);
                }

                for (int h = 0; h < count; h++)
                {
                    var hit = Hits[h];
                    if (hit == null || Contains(volume, hit)) continue;
                    occupant = hit;
                    return true;
                }
            }
            return false;
        }

        private static Vector3 Shrink(Vector3 halfExtents) => new Vector3(
            Mathf.Max(0.001f, halfExtents.x - ContactMargin),
            Mathf.Max(0.001f, halfExtents.y - ContactMargin),
            Mathf.Max(0.001f, halfExtents.z - ContactMargin));

        private static bool Contains(IReadOnlyList<Collider> list, Collider c)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == c) return true;
            }
            return false;
        }
    }
}
