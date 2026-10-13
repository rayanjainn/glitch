using System;
using System.Collections.Generic;
using BugReport.Core.Glitches;
using UnityEngine;

namespace BugReport.Glitches
{
    public enum CollisionMode
    {
        Solid,
        Intangible,
    }

    /// <summary>
    /// The only code allowed to change an entity's collision. Works on an explicit,
    /// authored list of colliders, never on discovered children.
    ///
    /// Solid      → each collider sits on its solid layer (captured from authoring, or
    ///              <see cref="solidLayerName"/> for objects authored intangible).
    /// Intangible → each collider moves to the Intangible layer: nothing collides with it,
    ///              but targeting still sees it so the player can restore it.
    ///
    /// The mode is a <see cref="ModifierStack{T}"/> (exclusive), so glitches add and
    /// release modifiers instead of writing layers directly.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GameEntity))]
    public sealed class CollisionCapability : MonoBehaviour
    {
        [SerializeField] private Collider[] colliders = new Collider[0];
        [SerializeField] private CollisionMode authoredMode = CollisionMode.Solid;
        [Tooltip("Layer used when an object authored Intangible becomes Solid.")]
        [SerializeField] private string solidLayerName = GameLayers.World;

        private int[] solidLayers;
        private ModifierStack<CollisionMode> channel;
        private bool initialized;
        private bool loggedMissingLayer;

        /// <summary>Raised after the effective mode changed and layers were updated.</summary>
        public event Action<CollisionMode> ModeChanged;

        public CollisionMode AuthoredMode => authoredMode;
        public CollisionMode EffectiveMode => Channel.Effective;
        public IReadOnlyList<Collider> Colliders => colliders;

        public ModifierStack<CollisionMode> Channel
        {
            get
            {
                EnsureInitialized();
                return channel;
            }
        }

        /// <summary>True if any collider belongs to a non-kinematic rigidbody (it would fall through the world if intangible).</summary>
        public bool HasDynamicBody
        {
            get
            {
                for (int i = 0; i < colliders.Length; i++)
                {
                    var body = colliders[i] != null ? colliders[i].attachedRigidbody : null;
                    if (body != null && !body.isKinematic) return true;
                }
                return false;
            }
        }

        /// <summary>Authoring helper (editor tooling, tests). Not allowed while a glitch modifies the channel.</summary>
        public void Configure(Collider[] newColliders, CollisionMode newAuthoredMode, string newSolidLayerName = GameLayers.World)
        {
            if (channel != null && channel.IsModified)
            {
                Debug.LogWarning("[BUG REPORT] Configure ignored: collision of " + name + " is currently glitched.", this);
                return;
            }
            colliders = newColliders ?? new Collider[0];
            authoredMode = newAuthoredMode;
            solidLayerName = newSolidLayerName;
            initialized = false;
            if (Application.isPlaying) EnsureInitialized();
        }

        private void Awake() => EnsureInitialized();

        private void EnsureInitialized()
        {
            if (initialized) return;
            initialized = true;

            int fallbackSolid = GameLayers.Index(solidLayerName);
            if (fallbackSolid < 0) fallbackSolid = 0;
            solidLayers = new int[colliders.Length];
            for (int i = 0; i < colliders.Length; i++)
            {
                var c = colliders[i];
                solidLayers[i] = authoredMode == CollisionMode.Solid && c != null ? c.gameObject.layer : fallbackSolid;
            }

            channel = new ModifierStack<CollisionMode>(authoredMode, ConflictPolicy.Exclusive, ApplyMode);
            ApplyMode(authoredMode);
        }

        private void ApplyMode(CollisionMode mode)
        {
            if (this == null) return; // destroyed while a lease was still outstanding (scene teardown)

            int intangible = GameLayers.Index(GameLayers.Intangible);
            if (mode == CollisionMode.Intangible && intangible < 0)
            {
                if (!loggedMissingLayer)
                {
                    loggedMissingLayer = true;
                    Debug.LogError("[BUG REPORT] 'Intangible' layer missing; run BUG REPORT > Setup > 1. Configure Project.", this);
                }
                return;
            }

            for (int i = 0; i < colliders.Length; i++)
            {
                var c = colliders[i];
                if (c == null) continue;
                c.gameObject.layer = mode == CollisionMode.Solid ? solidLayers[i] : intangible;
            }
            ModeChanged?.Invoke(mode);
        }
    }
}
