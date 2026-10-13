using System;
using BugReport.Presentation;
using UnityEngine;

namespace BugReport.Anomaly
{
    /// <summary>
    /// The anomaly's "eye": a dark pupil on the core that turns toward whatever the player is
    /// aiming at (or along the view when nothing is hovered), and dilates briefly when a glitch
    /// fires. Pure presentation; reads selection state only.
    /// </summary>
    public sealed class AnomalyEye : MonoBehaviour
    {
        [SerializeField] private Transform pupil;
        [SerializeField] private Transform cameraTransform;
        [SerializeField, OptionalReference] private VisualSettings settings;
        [SerializeField, Min(0f)] private float pupilOffset = 0.15f;
        [SerializeField, Min(0.1f)] private float turnSpeed = 12f;

        private GameServices services;
        private Action<GameServices> bindCallback;
        private Vector3 pupilScale;
        private Vector3 lookDirection = Vector3.forward;

        public Vector3 LookDirection => lookDirection;

        public void Configure(Transform newPupil, Transform newCamera)
        {
            pupil = newPupil;
            cameraTransform = newCamera;
            if (pupil != null) pupilScale = pupil.localScale;
        }

        private void Awake()
        {
            if (pupil != null) pupilScale = pupil.localScale;
        }

        private void OnEnable()
        {
            bindCallback = s => services = s;
            GameServices.WhenReady(bindCallback);
        }

        private void OnDisable()
        {
            GameServices.CancelWhenReady(bindCallback);
            services = null;
        }

        private void LateUpdate() => Look(Time.deltaTime);

        /// <summary>Public for tests.</summary>
        public void Look(float deltaTime)
        {
            if (pupil == null) return;
            bool on = VisualSettings.On(settings, s => s.anomalyEye);
            pupil.gameObject.SetActive(on);
            if (!on) return;

            var desired = cameraTransform != null ? cameraTransform.forward : transform.forward;
            if (services != null && !services.IsDisposed && services.Selection.TryGetHovered(out var hovered) && hovered is GameEntity entity)
            {
                var toTarget = entity.GetWorldBounds().center - transform.position;
                if (toTarget.sqrMagnitude > 1e-4f) desired = toTarget.normalized;
            }
            lookDirection = Vector3.Slerp(lookDirection, desired, 1f - Mathf.Exp(-turnSpeed * Mathf.Max(0f, deltaTime)));
            pupil.position = transform.position + lookDirection * pupilOffset;
            pupil.rotation = Quaternion.LookRotation(lookDirection);
            pupil.localScale = pupilScale;
        }
    }
}
