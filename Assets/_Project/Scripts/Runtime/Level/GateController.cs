using System;
using UnityEngine;

namespace BugReport.Level
{
    /// <summary>
    /// A gate that sinks into the floor once all its plates are pressed at the same time, and
    /// then stays open (latched), so it can never close on the hero. Announces the change so a
    /// waiting hero re-plans immediately.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [DisallowMultipleComponent]
    public sealed class GateController : MonoBehaviour
    {
        public enum GateState
        {
            Closed,
            Opening,
            Open,
        }

        [SerializeField] private PressurePlate[] plates = new PressurePlate[0];
        [SerializeField] private Vector3 openOffset = new Vector3(0f, -3.2f, 0f);
        [SerializeField, Min(0.05f)] private float openSeconds = 1.2f;
        [SerializeField] private bool autoStep = true;

        private Rigidbody body;
        private Vector3 closedPosition;
        private float progress;

        public GateState State { get; private set; } = GateState.Closed;
        public bool IsOpen => State == GateState.Open;
        public event Action Opened;

        public bool AutoStep
        {
            get => autoStep;
            set => autoStep = value;
        }

        public void Configure(PressurePlate[] newPlates, Vector3 newOpenOffset, float newOpenSeconds)
        {
            plates = newPlates ?? new PressurePlate[0];
            openOffset = newOpenOffset;
            openSeconds = Mathf.Max(0.05f, newOpenSeconds);
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            closedPosition = transform.position;
        }

        private void FixedUpdate()
        {
            if (autoStep) Step(Time.fixedDeltaTime);
        }

        public bool AllPlatesPressed
        {
            get
            {
                if (plates.Length == 0) return false;
                foreach (var plate in plates)
                {
                    if (plate == null || !plate.IsPressed) return false;
                }
                return true;
            }
        }

        public void Step(float deltaTime)
        {
            if (State == GateState.Closed && AllPlatesPressed) State = GateState.Opening;
            if (State != GateState.Opening) return;

            progress = Mathf.Min(1f, progress + deltaTime / openSeconds);
            float eased = progress * progress * (3f - 2f * progress);
            var target = closedPosition + openOffset * eased;
            if (autoStep) body.MovePosition(target);
            else transform.position = target; // tests step without physics frames

            if (progress < 1f) return;
            State = GateState.Open;
            Opened?.Invoke();
            if (GameServices.IsReady) GameServices.Current.RaiseWorldChanged();
        }
    }
}
