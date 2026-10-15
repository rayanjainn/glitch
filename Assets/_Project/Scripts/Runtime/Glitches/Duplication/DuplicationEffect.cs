using System.Collections.Generic;
using BugReport.Core.Entities;
using BugReport.Core.Glitches;
using UnityEngine;

namespace BugReport.Glitches
{
    /// <summary>
    /// Instantiates a copy from the source's profile PREFAB (never from the live object, which
    /// would clone glitch state such as kinematic-while-frozen with no operation owning it),
    /// places it in free space next to the source, transfers only approved state (pose, scale,
    /// optionally velocity), and registers it as the operation's product. Reverting the
    /// operation despawns the copy; anything glitching the copy then ends via TargetLost.
    /// </summary>
    public sealed class DuplicationEffect : IGlitchEffect
    {
        private const float Gap = 0.1f;

        private readonly GameServices services;
        private readonly string specId;
        private readonly HashSet<GameEntity> warnedMisconfigured = new HashSet<GameEntity>();
        private int copyCounter;

        public DuplicationEffect(GameServices services, string specId)
        {
            this.services = services;
            this.specId = specId;
        }

        public string Validate(GlitchRequest request)
        {
            if (!TryResolve(request.Targets[0], out var source, out var capability, out var sourceName, out var problem))
                return problem;
            if (services.Glitches.CountActiveOn(specId, request.Targets[0]) >= capability.Profile.MaxCopiesPerSource)
                return sourceName + ": COPY LIMIT (" + capability.Profile.MaxCopiesPerSource + ") REACHED";
            if (!TryFindSpawn(source, out _)) return "NO SPACE NEXT TO " + sourceName;
            return null;
        }

        public bool Apply(GlitchOperation operation, out string failure)
        {
            failure = null;
            if (!TryResolve(operation.Target(0), out var source, out var capability, out _, out var problem))
            {
                failure = problem;
                return false;
            }
            if (!TryFindSpawn(source, out var position))
            {
                failure = "NO SPACE";
                return false;
            }

            var copy = Spawn(source, capability.Profile, position, operation);
            if (copy == null || !copy.IsRegistered)
            {
                if (copy != null) Despawn(copy.gameObject);
                failure = "COPY FAILED TO INITIALISE";
                return false;
            }

            operation.AddLease(new DespawnLease(operation.Id, copy));
            operation.AddProduct(copy.Id);
            operation.EffectState = copy;
            return true;
        }

        public string CanEnd(GlitchOperation operation, GlitchEndReason reason) => null;

        public void OnEnded(GlitchOperation operation, GlitchEndReason reason)
        {
        }

        private GameEntity Spawn(GameEntity source, DuplicationProfile profile, Vector3 position, GlitchOperation operation)
        {
            // Instantiate under an inactive holder so nothing on the copy runs (Awake/OnEnable,
            // registration) until identity and lineage are configured.
            var holder = new GameObject("~DuplicateHolder");
            holder.SetActive(false);
            try
            {
                var go = Object.Instantiate(profile.Prefab, position, source.transform.rotation, holder.transform);
                go.transform.localScale = source.transform.lossyScale;

                var entity = go.GetComponent<GameEntity>();
                if (entity == null)
                {
                    Debug.LogError("[BUG REPORT] Duplication prefab '" + profile.Prefab.name + "' has no GameEntity.", profile);
                    Object.Destroy(go);
                    return null;
                }

                var caps = source.Capabilities;
                if (profile.ObjectivePolicy == DuplicateObjectivePolicy.NotObjective) caps &= ~EntityCapabilities.ObjectiveRelevant;
                copyCounter++;
                entity.Configure(source.DisplayName + " COPY " + copyCounter, source.Category, caps, source.Description);
                go.name = source.name + "_Copy" + copyCounter;

                var sourceMarker = source.GetComponent<DuplicateMarker>();
                var marker = go.GetComponent<DuplicateMarker>();
                if (marker == null) marker = go.AddComponent<DuplicateMarker>();
                marker.Stamp(source.Id, sourceMarker != null ? sourceMarker.Generation + 1 : 1, operation.Id);

                go.transform.SetParent(null, true);
                if (!go.activeSelf) go.SetActive(true);

                if (profile.InheritVelocity) CopyVelocity(source, go);
                return entity;
            }
            finally
            {
                Object.Destroy(holder);
            }
        }

        private static void CopyVelocity(GameEntity source, GameObject copy)
        {
            var from = source.GetComponent<Rigidbody>();
            var to = copy.GetComponent<Rigidbody>();
            if (from == null || to == null || from.isKinematic || to.isKinematic) return;
            to.linearVelocity = from.linearVelocity;
            to.angularVelocity = from.angularVelocity;
        }

        /// <summary>First free spot beside the source (right, left, front, back, top). World-axis aligned.</summary>
        private static bool TryFindSpawn(GameEntity source, out Vector3 position)
        {
            var bounds = source.GetPhysicalBounds();
            var size = bounds.size;
            var half = bounds.extents * 0.98f;
            var candidates = new[]
            {
                Vector3.right * (size.x + Gap),
                Vector3.left * (size.x + Gap),
                Vector3.forward * (size.z + Gap),
                Vector3.back * (size.z + Gap),
                Vector3.up * (size.y + Gap),
            };

            int mask = SpawnBlockingMask();
            foreach (var offset in candidates)
            {
                var center = bounds.center + offset;
                if (!Physics.CheckBox(center, half, Quaternion.identity, mask, QueryTriggerInteraction.Ignore))
                {
                    position = source.transform.position + offset;
                    return true;
                }
            }
            position = default;
            return false;
        }

        private static int SpawnBlockingMask()
        {
            int mask = Physics.AllLayers & ~(1 << GameLayers.IgnoreRaycastLayer);
            int intangible = GameLayers.Index(GameLayers.Intangible);
            if (intangible >= 0) mask &= ~(1 << intangible);
            return mask;
        }

        private bool TryResolve(GameEntityId id, out GameEntity entity, out DuplicationCapability capability, out string displayName, out string problem)
        {
            entity = null;
            capability = null;
            displayName = id.ToString();
            problem = "TARGET LOST";
            if (!services.Registry.TryGet(id, out var descriptor) || !(descriptor is GameEntity e)) return false;

            entity = e;
            displayName = e.DisplayName;
            capability = e.GetComponent<DuplicationCapability>();
            if (capability == null || capability.Profile == null || capability.Profile.Prefab == null)
            {
                problem = displayName + " HAS NO DUPLICATION PROFILE";
                if (warnedMisconfigured.Add(e))
                    Debug.LogWarning("[BUG REPORT] " + displayName + " declares Duplicable but has no DuplicationCapability with a profile and prefab.", e);
                return false;
            }
            problem = null;
            return true;
        }

        internal static void Despawn(GameObject copy)
        {
            if (copy == null) return;
            copy.SetActive(false); // unregister now (deterministic cascade), destroy at end of frame
            Object.Destroy(copy);
        }

        /// <summary>
        /// Releasing it removes the copy. Idempotent. If the copy has already unregistered, it is
        /// being disabled/destroyed by someone else (that is what ended the operation), so it is
        /// left alone instead of being destroyed a second time mid-destruction.
        /// </summary>
        private sealed class DespawnLease : IModifierLease
        {
            private readonly GameEntity copy;

            public DespawnLease(OperationId owner, GameEntity copy)
            {
                Owner = owner;
                this.copy = copy;
            }

            public OperationId Owner { get; }
            public bool IsReleased { get; private set; }

            public void Release()
            {
                if (IsReleased) return;
                IsReleased = true;
                if (copy == null || !copy.IsRegistered) return;
                Despawn(copy.gameObject);
            }
        }
    }
}
