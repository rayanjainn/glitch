using BugReport.Core.Glitches;
using BugReport.Core.Properties;
using UnityEngine;

namespace BugReport.Glitches
{
    /// <summary>
    /// The properties of one entity that Property Swap may exchange (GDD 4.4: an explicit list, never
    /// arbitrary reflection). Mass drives the Rigidbody (pressure plates read it); Speed scales a
    /// <see cref="PingPongMover"/>. Each property is an exclusive modifier channel owned by the
    /// operation that changed it, so reverting always restores the authored value.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GameEntity))]
    public sealed class PropertyCapability : MonoBehaviour
    {
        private ModifierStack<float> mass;
        private ModifierStack<float> speed;
        private Rigidbody body;
        private PingPongMover mover;

        private void Awake() => EnsureInitialized();

        private void EnsureInitialized()
        {
            if (mass != null) return;
            body = GetComponent<Rigidbody>();
            mover = GetComponent<PingPongMover>();
            mass = new ModifierStack<float>(body != null ? body.mass : 1f, ConflictPolicy.Exclusive, ApplyMass);
            speed = new ModifierStack<float>(mover != null ? mover.SpeedScale : 1f, ConflictPolicy.Exclusive, ApplySpeed);
        }

        public bool Has(SwapProperty property)
        {
            EnsureInitialized();
            return property == SwapProperty.Mass ? body != null : mover != null;
        }

        public ModifierStack<float> Channel(SwapProperty property)
        {
            EnsureInitialized();
            return property == SwapProperty.Mass ? mass : speed;
        }

        public float Value(SwapProperty property) => Channel(property).Effective;
        public bool IsAltered(SwapProperty property) => Channel(property).IsModified;

        private void ApplyMass(float value)
        {
            if (this == null || body == null) return;
            body.mass = Mathf.Max(0.01f, value);
        }

        private void ApplySpeed(float value)
        {
            if (this == null || mover == null) return;
            mover.SpeedScale = value;
        }
    }
}
