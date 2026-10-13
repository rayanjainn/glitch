using System.Collections.Generic;
using BugReport.Core.Entities;
using UnityEngine;

namespace BugReport
{
    /// <summary>
    /// Makes a GameObject a gameplay entity: registers it with the level's
    /// <see cref="EntityRegistry"/> while enabled and declares what the anomaly may
    /// do to it. Capabilities are explicit data; glitches will additionally require
    /// the matching capability component (Milestone 2+).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameEntity : MonoBehaviour, IEntityDescriptor
    {
        [SerializeField] private string displayName = string.Empty;
        [SerializeField] private EntityCategory category = EntityCategory.Prop;
        [SerializeField] private EntityCapabilities capabilities = EntityCapabilities.Inspectable;
        [SerializeField, TextArea(2, 4)] private string description = string.Empty;

        private readonly List<Renderer> renderers = new List<Renderer>();
        private readonly List<Collider> colliders = new List<Collider>();
        private EntityRegistry registeredWith;

        public EntityId Id { get; private set; }
        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public EntityCategory Category => category;
        public EntityCapabilities Capabilities => capabilities;
        public string Description => description;
        public bool IsRegistered => Id.IsValid;

        /// <summary>Uses Unity's overloaded null check, so it turns false as soon as the object is destroyed.</summary>
        public bool IsAlive => this != null;

        /// <summary>
        /// Authoring helper for editor tooling and tests. Call before the entity is
        /// enabled; changing identity of a registered entity is not supported.
        /// </summary>
        public void Configure(string newDisplayName, EntityCategory newCategory, EntityCapabilities newCapabilities, string newDescription = "")
        {
            if (IsRegistered)
            {
                Debug.LogWarning("[BUG REPORT] Configure called on registered entity " + Id + "; ignored.", this);
                return;
            }
            displayName = newDisplayName ?? string.Empty;
            category = newCategory;
            capabilities = newCapabilities;
            description = newDescription ?? string.Empty;
        }

        private void Awake() => RefreshVisualCache();

        private void OnEnable() => GameServices.WhenReady(Register);

        private void OnDisable()
        {
            GameServices.CancelWhenReady(Register);
            Unregister();
        }

        /// <summary>Rebuilds the renderer/collider cache used for bounds. Call after changing the hierarchy.</summary>
        public void RefreshVisualCache()
        {
            renderers.Clear();
            colliders.Clear();
            GetComponentsInChildren(true, renderers);
            // Effect renderers (lines, trails, particles) must not inflate the selection bounds.
            renderers.RemoveAll(r => r is LineRenderer || r is TrailRenderer || r is ParticleSystemRenderer);
            GetComponentsInChildren(true, colliders);
        }

        /// <summary>World-space bounds from renderers, falling back to colliders, then to the pivot.</summary>
        public Bounds GetWorldBounds()
        {
            bool has = false;
            var bounds = new Bounds(transform.position, Vector3.zero);
            for (int i = 0; i < renderers.Count; i++)
            {
                var r = renderers[i];
                if (r == null || !r.enabled || !r.gameObject.activeInHierarchy) continue;
                if (has) bounds.Encapsulate(r.bounds);
                else { bounds = r.bounds; has = true; }
            }
            if (has) return bounds;

            for (int i = 0; i < colliders.Count; i++)
            {
                var c = colliders[i];
                if (c == null || !c.enabled) continue;
                if (has) bounds.Encapsulate(c.bounds);
                else { bounds = c.bounds; has = true; }
            }
            return bounds;
        }

        private void Register(GameServices services)
        {
            // Only reachable from OnEnable or a pending callback that OnDisable cancels.
            if (this == null || IsRegistered) return;
            registeredWith = services.Registry;
            Id = registeredWith.Register(this);
        }

        private void Unregister()
        {
            var registry = registeredWith;
            var id = Id;
            registeredWith = null;
            Id = EntityId.None;
            if (registry != null && id.IsValid) registry.Unregister(id);
        }
    }
}
