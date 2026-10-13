using UnityEngine;

namespace BugReport.Glitches
{
    /// <summary>
    /// Placeholder presentation for collision changes: swaps renderer materials while
    /// the effective collision differs from authoring. Ghost look when an authored-solid
    /// object turns intangible; "solidified" look when an authored-intangible one turns solid.
    /// Reads state only; restores the original materials when the change ends.
    /// </summary>
    [RequireComponent(typeof(CollisionCapability))]
    public sealed class CollisionModeVisual : MonoBehaviour
    {
        [SerializeField] private Renderer[] renderers = new Renderer[0];
        [SerializeField] private Material ghostMaterial;
        [SerializeField] private Material solidifiedMaterial;

        private CollisionCapability capability;
        private Material[][] originals;

        public void Configure(Renderer[] targetRenderers, Material ghost, Material solidified)
        {
            renderers = targetRenderers ?? new Renderer[0];
            ghostMaterial = ghost;
            solidifiedMaterial = solidified;
        }

        private void Awake()
        {
            capability = GetComponent<CollisionCapability>();
            originals = new Material[renderers.Length][];
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null) originals[i] = renderers[i].sharedMaterials;
            }
        }

        private void OnEnable()
        {
            capability.ModeChanged += OnModeChanged;
            OnModeChanged(capability.EffectiveMode);
        }

        private void OnDisable()
        {
            if (capability != null) capability.ModeChanged -= OnModeChanged;
        }

        private void OnModeChanged(CollisionMode mode)
        {
            Material overrideMaterial = null;
            if (mode != capability.AuthoredMode)
                overrideMaterial = mode == CollisionMode.Intangible ? ghostMaterial : solidifiedMaterial;

            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r == null || originals[i] == null) continue;
                if (overrideMaterial == null)
                {
                    r.sharedMaterials = originals[i];
                }
                else
                {
                    var swapped = new Material[originals[i].Length];
                    for (int m = 0; m < swapped.Length; m++) swapped[m] = overrideMaterial;
                    r.sharedMaterials = swapped;
                }
            }
        }
    }
}
