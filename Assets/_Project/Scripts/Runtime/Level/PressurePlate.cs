using System;
using UnityEngine;

namespace BugReport.Level
{
    /// <summary>
    /// Pressed while a rigidbody of at least <see cref="minMass"/> rests in its volume. Frozen
    /// (kinematic) bodies still count, and so do duplicates: weight is weight. Polls with an
    /// overlap query instead of trigger callbacks so it is deterministic and testable.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    [DisallowMultipleComponent]
    public sealed class PressurePlate : MonoBehaviour
    {
        private static readonly Collider[] Hits = new Collider[16];

        [SerializeField, Min(0f)] private float minMass = 5f;
        [Tooltip("Optional renderer that shows the pressed state.")]
        [SerializeField, OptionalReference] private Renderer indicator;
        [SerializeField, OptionalReference] private Material idleMaterial;
        [SerializeField, OptionalReference] private Material pressedMaterial;

        private BoxCollider volume;
        private bool restCaptured;
        private float restY;

        public bool IsPressed { get; private set; }
        public event Action<bool> PressedChanged;

        public void Configure(float newMinMass, Renderer newIndicator, Material idle, Material pressed)
        {
            minMass = newMinMass;
            indicator = newIndicator;
            idleMaterial = idle;
            pressedMaterial = pressed;
        }

        private void Awake()
        {
            volume = GetComponent<BoxCollider>();
            volume.isTrigger = true;
            ApplyVisual();
        }

        private void FixedUpdate() => Evaluate();

        /// <summary>Re-checks the volume now. Public for tests.</summary>
        public bool Evaluate()
        {
            if (volume == null) volume = GetComponent<BoxCollider>();
            var t = volume.transform;
            var scale = t.lossyScale;
            var half = Vector3.Scale(volume.size, new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z))) * 0.5f;
            int count = Physics.OverlapBoxNonAlloc(t.TransformPoint(volume.center), half, Hits, t.rotation, Physics.AllLayers, QueryTriggerInteraction.Ignore);

            bool pressed = false;
            for (int i = 0; i < count; i++)
            {
                var body = Hits[i] != null ? Hits[i].attachedRigidbody : null;
                if (body != null && body.mass >= minMass && body.GetComponent<CharacterController>() == null)
                {
                    pressed = true;
                    break;
                }
            }

            if (pressed != IsPressed)
            {
                IsPressed = pressed;
                ApplyVisual();
                PressedChanged?.Invoke(pressed);
            }
            return IsPressed;
        }

        private void ApplyVisual()
        {
            if (indicator == null) return;
            var m = IsPressed ? pressedMaterial : idleMaterial;
            if (m != null) indicator.sharedMaterial = m;
            // A cue that does not depend on colour: a pressed plate sits lower.
            var t = indicator.transform;
            if (!restCaptured)
            {
                restCaptured = true;
                restY = t.localPosition.y;
            }
            var p = t.localPosition;
            p.y = restY - (IsPressed ? 0.04f : 0f);
            t.localPosition = p;
        }
    }
}
