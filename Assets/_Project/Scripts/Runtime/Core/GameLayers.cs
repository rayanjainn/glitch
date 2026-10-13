using UnityEngine;

namespace BugReport
{
    /// <summary>
    /// Physics layers the project relies on. Created by
    /// "BUG REPORT > Setup > 1. Configure Project" (never by hand-editing indices).
    ///
    /// Intangible: objects whose collision has been removed. Collides with nothing,
    /// but stays visible to targeting raycasts so the anomaly can still select and
    /// restore it (see ARCHITECTURE.md, Collision Transfer).
    /// </summary>
    public static class GameLayers
    {
        public const string World = "World";
        public const string Props = "Props";
        public const string Hero = "Hero";
        public const string Anomaly = "Anomaly";
        public const string Intangible = "Intangible";

        public static readonly string[] Required = { World, Props, Hero, Anomaly, Intangible };

        /// <summary>Built-in "Ignore Raycast" layer index.</summary>
        public const int IgnoreRaycastLayer = 2;

        public static int Index(string layerName) => LayerMask.NameToLayer(layerName);

        public static bool AreConfigured(out string missing)
        {
            missing = string.Empty;
            foreach (var layer in Required)
            {
                if (Index(layer) < 0) missing += (missing.Length > 0 ? ", " : string.Empty) + layer;
            }
            return missing.Length == 0;
        }

        /// <summary>Everything except the anomaly and Ignore Raycast. Includes Intangible on purpose.</summary>
        public static int TargetingMask
        {
            get
            {
                int mask = Physics.AllLayers & ~(1 << IgnoreRaycastLayer);
                int anomaly = Index(Anomaly);
                if (anomaly >= 0) mask &= ~(1 << anomaly);
                return mask;
            }
        }

        /// <summary>
        /// Things that may not be inside a collider when it becomes solid: the hero, the
        /// anomaly and movable props. Static world geometry is excluded on purpose (authored overlaps).
        /// </summary>
        public static int OccupantMask
        {
            get
            {
                int mask = 0;
                foreach (var layer in new[] { Hero, Anomaly, Props })
                {
                    int index = Index(layer);
                    if (index >= 0) mask |= 1 << index;
                }
                return mask != 0 ? mask : Physics.AllLayers & ~(1 << IgnoreRaycastLayer);
            }
        }

        /// <summary>Geometry the camera must not clip through. Falls back to Default if layers are missing.</summary>
        public static int CameraObstructionMask
        {
            get
            {
                int mask = 0;
                int world = Index(World);
                int props = Index(Props);
                if (world >= 0) mask |= 1 << world;
                if (props >= 0) mask |= 1 << props;
                return mask != 0 ? mask : 1; // 1 = Default layer
            }
        }
    }
}
