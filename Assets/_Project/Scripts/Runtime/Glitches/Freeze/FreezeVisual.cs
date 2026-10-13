using BugReport.Presentation;
using UnityEngine;

namespace BugReport.Glitches
{
    /// <summary>Pale-cyan "frozen" material while frozen. Wins over collision visuals (higher priority).</summary>
    [RequireComponent(typeof(FreezeCapability), typeof(RendererOverrides))]
    public sealed class FreezeVisual : MonoBehaviour
    {
        public const string OverrideKey = "freeze";
        public const int Priority = 20;

        [SerializeField] private Material frozenMaterial;

        private FreezeCapability capability;
        private RendererOverrides overrides;

        public void Configure(Material frozen) => frozenMaterial = frozen;

        private void Awake()
        {
            capability = GetComponent<FreezeCapability>();
            overrides = GetComponent<RendererOverrides>();
        }

        private void OnEnable()
        {
            capability.FrozenChanged += OnFrozenChanged;
            OnFrozenChanged(capability.IsFrozen);
        }

        private void OnDisable()
        {
            if (capability != null) capability.FrozenChanged -= OnFrozenChanged;
        }

        private void OnFrozenChanged(bool frozen)
        {
            if (overrides == null) return;
            if (frozen) overrides.Set(OverrideKey, frozenMaterial, Priority);
            else overrides.Clear(OverrideKey);
        }
    }
}
