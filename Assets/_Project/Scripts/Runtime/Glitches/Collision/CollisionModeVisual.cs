using BugReport.Presentation;
using UnityEngine;

namespace BugReport.Glitches
{
    /// <summary>
    /// Placeholder presentation for collision changes: requests a material override while
    /// the effective collision differs from authoring. Ghost look when an authored-solid
    /// object turns intangible; "solidified" look when an authored-intangible one turns solid.
    /// </summary>
    [RequireComponent(typeof(CollisionCapability), typeof(RendererOverrides))]
    public sealed class CollisionModeVisual : MonoBehaviour
    {
        public const string OverrideKey = "collision";
        public const int Priority = 10;

        [SerializeField] private Material ghostMaterial;
        [SerializeField] private Material solidifiedMaterial;

        private CollisionCapability capability;
        private RendererOverrides overrides;

        public void Configure(Material ghost, Material solidified)
        {
            ghostMaterial = ghost;
            solidifiedMaterial = solidified;
        }

        private void Awake()
        {
            capability = GetComponent<CollisionCapability>();
            overrides = GetComponent<RendererOverrides>();
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
            if (overrides == null) return;
            if (mode == capability.AuthoredMode) overrides.Clear(OverrideKey);
            else overrides.Set(OverrideKey, mode == CollisionMode.Intangible ? ghostMaterial : solidifiedMaterial, Priority);
        }
    }
}
