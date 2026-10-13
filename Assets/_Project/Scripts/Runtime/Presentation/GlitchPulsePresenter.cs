using System;
using System.Collections.Generic;
using BugReport.Core.Glitches;
using BugReport.Glitches;
using UnityEngine;
using UnityEngine.Rendering;

namespace BugReport.Presentation
{
    /// <summary>
    /// Placeholder "transfer" effect: when a multi-target glitch is applied (or one creates a
    /// copy), a beam in the glitch's colour shoots from the source to the result and fades out.
    /// Short and intentional (GDD 7.3), not continuous. Replaced in the art pass.
    /// </summary>
    public sealed class GlitchPulsePresenter : MonoBehaviour
    {
        [SerializeField] private Material lineMaterial;
        [SerializeField] private GlitchController glitches;
        [SerializeField, Min(0.05f)] private float duration = 0.6f;
        [SerializeField, Min(0.001f)] private float width = 0.06f;

        private readonly List<Pulse> pulses = new List<Pulse>();
        private GameServices services;
        private Action<GameServices> bindCallback;

        private void OnEnable()
        {
            bindCallback = Bind;
            GameServices.WhenReady(bindCallback);
        }

        private void OnDisable()
        {
            GameServices.CancelWhenReady(bindCallback);
            if (services != null && !services.IsDisposed) services.Glitches.OperationStarted -= OnStarted;
            services = null;
            foreach (var p in pulses) p.Line.enabled = false;
        }

        private void Bind(GameServices s)
        {
            services = s;
            s.Glitches.OperationStarted += OnStarted;
        }

        private void OnStarted(GlitchOperation operation)
        {
            if (lineMaterial == null) return;
            // Beam from the first target to the last target, or to the first thing the glitch created (a copy).
            Core.Entities.GameEntityId endId;
            if (operation.ProductCount > 0) endId = operation.Product(0);
            else if (operation.TargetCount > 1) endId = operation.Target(operation.TargetCount - 1);
            else return;
            if (!TryCenter(operation.Target(0), out var from) || !TryCenter(endId, out var to)) return;

            var pulse = Acquire();
            pulse.Color = glitches != null ? glitches.ColorFor(operation) : Color.white;
            pulse.StartTime = Time.unscaledTime;
            pulse.Line.SetPosition(0, from);
            pulse.Line.SetPosition(1, to);
            pulse.Line.enabled = true;
        }

        private void Update()
        {
            float now = Time.unscaledTime;
            for (int i = 0; i < pulses.Count; i++)
            {
                var p = pulses[i];
                if (!p.Line.enabled) continue;
                float t = (now - p.StartTime) / duration;
                if (t >= 1f)
                {
                    p.Line.enabled = false;
                    continue;
                }
                var c = p.Color;
                c.a = 1f - t;
                p.Line.startColor = c;
                p.Line.endColor = c;
                float w = width * (1f - 0.7f * t);
                p.Line.startWidth = w;
                p.Line.endWidth = w;
            }
        }

        private bool TryCenter(Core.Entities.GameEntityId id, out Vector3 center)
        {
            center = default;
            if (services == null || !services.Registry.TryGet(id, out var descriptor) || !(descriptor is GameEntity entity)) return false;
            center = entity.GetWorldBounds().center;
            return true;
        }

        private Pulse Acquire()
        {
            foreach (var p in pulses)
            {
                if (!p.Line.enabled) return p;
            }
            var go = new GameObject("Pulse_" + pulses.Count, typeof(LineRenderer));
            go.layer = GameLayers.IgnoreRaycastLayer;
            go.transform.SetParent(transform, false);
            var line = go.GetComponent<LineRenderer>();
            line.sharedMaterial = lineMaterial;
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.numCapVertices = 2;
            line.enabled = false;
            var pulse = new Pulse { Line = line };
            pulses.Add(pulse);
            return pulse;
        }

        private sealed class Pulse
        {
            public LineRenderer Line;
            public Color Color;
            public float StartTime;
        }
    }
}
