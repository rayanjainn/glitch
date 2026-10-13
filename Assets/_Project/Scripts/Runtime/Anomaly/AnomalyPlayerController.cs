using System;
using BugReport.InputHandling;
using BugReport.Interaction;
using UnityEngine;

namespace BugReport.Anomaly
{
    /// <summary>
    /// Player-side glue: reads <see cref="InputReader"/> and drives the motor,
    /// camera, selection and game-state toggles. Contains no movement or selection
    /// rules itself, so the same systems can be driven by tests or scripted events.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AnomalyPlayerController : MonoBehaviour
    {
        [SerializeField] private InputReader input;
        [SerializeField] private AnomalyMotor motor;
        [SerializeField] private AnomalyCameraRig cameraRig;
        [SerializeField] private SelectionController selection;

        private GameServices services;
        private Action<GameServices> bindCallback;

        private void Awake()
        {
            if (input == null) input = GetComponent<InputReader>();
            if (motor == null) motor = GetComponent<AnomalyMotor>();
            if (input == null || motor == null || cameraRig == null || selection == null)
            {
                Debug.LogError("[BUG REPORT] AnomalyPlayerController is missing references (input, motor, cameraRig, selection).", this);
            }
        }

        private void OnEnable()
        {
            bindCallback = s => services = s;
            GameServices.WhenReady(bindCallback);
            if (input == null) return;
            input.InspectPerformed += OnInspect;
            input.CancelPerformed += OnCancel;
            input.ToggleInspectionPerformed += OnToggleInspection;
            input.PausePerformed += OnPause;
        }

        private void OnDisable()
        {
            GameServices.CancelWhenReady(bindCallback);
            services = null;
            if (input == null) return;
            input.InspectPerformed -= OnInspect;
            input.CancelPerformed -= OnCancel;
            input.ToggleInspectionPerformed -= OnToggleInspection;
            input.PausePerformed -= OnPause;
        }

        private void Update()
        {
            if (input == null || motor == null || cameraRig == null) return;
            bool active = services != null && !services.IsDisposed && services.State.IsGameplayActive;
            if (!active) return;

            float dt = Time.deltaTime;
            var look = input.ReadLook(out bool fromPointer);
            cameraRig.AddLook(look, fromPointer, dt);
            motor.Tick(input.Move, input.Vertical, input.Boost, cameraRig.YawRotation, dt);
        }

        private void OnInspect()
        {
            if (selection != null) selection.SelectHovered();
        }

        private void OnCancel()
        {
            if (selection != null) selection.CancelSelection();
        }

        private void OnToggleInspection()
        {
            if (services != null && !services.IsDisposed) services.State.ToggleInspection();
        }

        private void OnPause()
        {
            if (services != null && !services.IsDisposed) services.State.TogglePause();
        }
    }
}
