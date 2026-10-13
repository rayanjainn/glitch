using System.Collections.Generic;
using BugReport.Glitches;
using UnityEngine;

namespace BugReport.Level
{
    /// <summary>
    /// Moving machinery the hero refuses to walk through while it is dangerous: any route
    /// segment crossing <see cref="Volume"/> is impassable unless the machine is frozen or
    /// intangible. The hero is not "frustratingly stupid" (GDD 2.2); the player has to make the
    /// machine safe. The volume is also what Freeze/Collision Transfer check before letting the
    /// machine move/solidify again, so a machine never restarts with someone in its path.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HeroHazard : MonoBehaviour
    {
        private static readonly List<HeroHazard> active = new List<HeroHazard>();

        [Tooltip("Trigger box covering everywhere the machine can move.")]
        [SerializeField] private BoxCollider volume;

        private FreezeCapability freeze;
        private CollisionCapability collision;
        private Collider[] volumeList;

        public static IReadOnlyList<HeroHazard> Active => active;
        public BoxCollider Volume => volume;

        public IReadOnlyList<Collider> VolumeColliders => volumeList ??= new Collider[] { volume };

        public bool IsSafe =>
            (freeze != null && freeze.IsFrozen) ||
            (collision != null && collision.EffectiveMode == CollisionMode.Intangible);

        public void Configure(BoxCollider newVolume)
        {
            volume = newVolume;
            volumeList = null;
        }

        private void Awake()
        {
            freeze = GetComponent<FreezeCapability>();
            collision = GetComponent<CollisionCapability>();
            if (volume != null) volume.isTrigger = true;
        }

        private void OnEnable()
        {
            if (!active.Contains(this)) active.Add(this);
        }

        private void OnDisable() => active.Remove(this);

        /// <summary>Does the walk from <paramref name="from"/> to <paramref name="to"/> (XZ plane) pass through the danger volume, padded by <paramref name="radius"/>?</summary>
        public bool Intersects(Vector3 from, Vector3 to, float radius)
        {
            if (volume == null) return false;
            var b = volume.bounds;
            float minX = b.min.x - radius, maxX = b.max.x + radius;
            float minZ = b.min.z - radius, maxZ = b.max.z + radius;

            // Slab test of the segment against the expanded rectangle.
            float t0 = 0f, t1 = 1f;
            float dx = to.x - from.x, dz = to.z - from.z;
            if (!Clip(-dx, from.x - minX, ref t0, ref t1)) return false;
            if (!Clip(dx, maxX - from.x, ref t0, ref t1)) return false;
            if (!Clip(-dz, from.z - minZ, ref t0, ref t1)) return false;
            if (!Clip(dz, maxZ - from.z, ref t0, ref t1)) return false;
            return true;
        }

        private static bool Clip(float p, float q, ref float t0, ref float t1)
        {
            if (Mathf.Approximately(p, 0f)) return q >= 0f;
            float r = q / p;
            if (p < 0f)
            {
                if (r > t1) return false;
                if (r > t0) t0 = r;
            }
            else
            {
                if (r < t0) return false;
                if (r < t1) t1 = r;
            }
            return true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => active.Clear();
    }
}
