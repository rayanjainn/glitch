using System;
using System.Collections.Generic;
using BugReport.Core.Entities;
using BugReport.Core.Glitches;
using BugReport.Glitches;
using UnityEngine;
using UnityEngine.Rendering;

namespace BugReport.Presentation
{
    /// <summary>
    /// What using a glitch should feel like: an energy orb leaves the anomaly, flies to the target, and
    /// bursts into rays in the glitch's colour. Short and intentional (GDD 7.3); purely visual, driven by
    /// the glitch manager's events and ignored when glitch camera bursts are turned off.
    /// </summary>
    public sealed class PowerSpectacle : MonoBehaviour
    {
        private const int RaysPerBurst = 12;

        [SerializeField] private Material lineMaterial;
        [SerializeField] private GlitchController glitches;
        [SerializeField, OptionalReference] private Transform origin;
        [SerializeField, OptionalReference] private VisualSettings settings;
        [SerializeField, OptionalReference, Tooltip("Celebrates here (the hero) when the last objective is done.")] private Transform celebrate;
        [SerializeField, OptionalReference] private Level.ObjectiveDirector objectives;
        [SerializeField, Min(0.05f)] private float orbSeconds = 0.22f;
        [SerializeField, Min(0.05f)] private float burstSeconds = 0.5f;
        [SerializeField, Min(0.1f)] private float rayLength = 1.1f;

        private readonly List<Orb> orbs = new List<Orb>();
        private readonly List<Burst> bursts = new List<Burst>();
        private GameServices services;
        private Action<GameServices> bindCallback;

        public int ActiveOrbs
        {
            get
            {
                int n = 0;
                foreach (var o in orbs) if (o.Line.enabled) n++;
                return n;
            }
        }

        private static readonly Color[] Confetti =
        {
            new Color(1f, 0.85f, 0.2f), new Color(0.3f, 0.95f, 1f), new Color(1f, 0.3f, 0.85f), new Color(0.5f, 1f, 0.4f), new Color(1f, 0.55f, 0.2f),
        };

        private void OnEnable()
        {
            if (objectives != null) objectives.LevelCompleted += OnLevelCompleted;
            bindCallback = s =>
            {
                services = s;
                s.Glitches.OperationStarted += OnStarted;
            };
            GameServices.WhenReady(bindCallback);
        }

        private void OnLevelCompleted()
        {
            if (lineMaterial == null || celebrate == null || !VisualSettings.On(settings, s => s.glitchCameraBursts)) return;
            float now = Time.unscaledTime;
            for (int i = 0; i < Confetti.Length; i++)
            {
                var burst = AcquireBurst();
                burst.Start = now + i * 0.18f;
                burst.Center = celebrate.position + new Vector3((i - 2) * 0.5f, 1.6f + (i % 2) * 0.5f, 0f);
                burst.Color = Confetti[i];
                burst.Active = true;
            }
        }

        private void OnDisable()
        {
            if (objectives != null) objectives.LevelCompleted -= OnLevelCompleted;
            GameServices.CancelWhenReady(bindCallback);
            if (services != null && !services.IsDisposed) services.Glitches.OperationStarted -= OnStarted;
            services = null;
            foreach (var o in orbs) o.Line.enabled = false;
            foreach (var b in bursts) b.Hide();
        }

        private void OnStarted(GlitchOperation operation)
        {
            if (lineMaterial == null || !VisualSettings.On(settings, s => s.glitchCameraBursts)) return;
            var color = glitches != null ? glitches.ColorFor(operation) : Color.white;
            float now = Time.unscaledTime;
            var from = origin != null ? origin.position : Vector3.zero;

            for (int i = 0; i < operation.TargetCount && i < 2; i++)
            {
                if (!TryCenter(operation.Target(i), out var to)) continue;
                var orb = AcquireOrb();
                orb.From = i == 0 ? from : (TryCenter(operation.Target(0), out var first) ? first : from);
                orb.To = to;
                orb.Color = color;
                orb.Start = now + (i == 0 ? 0f : orbSeconds);
                orb.Line.enabled = false;
                orb.Active = true;
                var burst = AcquireBurst();
                burst.Start = orb.Start + orbSeconds;
                burst.Center = to;
                burst.Color = color;
                burst.Active = true;
            }
            for (int i = 0; i < operation.ProductCount; i++)
            {
                if (!TryCenter(operation.Product(i), out var at)) continue;
                var burst = AcquireBurst();
                burst.Start = now + orbSeconds;
                burst.Center = at;
                burst.Color = color;
                burst.Active = true;
            }
        }

        private void Update()
        {
            float now = Time.unscaledTime;
            foreach (var o in orbs)
            {
                if (!o.Active) continue;
                float t = (now - o.Start) / orbSeconds;
                if (t < 0f) continue;
                if (t >= 1f)
                {
                    o.Active = false;
                    o.Line.enabled = false;
                    continue;
                }
                float eased = 1f - (1f - t) * (1f - t);
                var arc = Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.8f;
                o.Line.enabled = true;
                o.Line.SetPosition(1, Vector3.Lerp(o.From, o.To, eased) + arc);
                float tail = Mathf.Max(0f, eased - 0.22f);
                o.Line.SetPosition(0, Vector3.Lerp(o.From, o.To, tail) + Vector3.up * Mathf.Sin(tail * Mathf.PI) * 0.8f);
                var c = o.Color;
                o.Line.startColor = new Color(c.r, c.g, c.b, 0f);
                o.Line.endColor = new Color(1f, 1f, 1f, 1f);
                o.Line.startWidth = 0.02f;
                o.Line.endWidth = 0.22f;
            }
            foreach (var b in bursts)
            {
                if (!b.Active) continue;
                float t = (now - b.Start) / burstSeconds;
                if (t < 0f) continue;
                if (t >= 1f)
                {
                    b.Active = false;
                    b.Hide();
                    continue;
                }
                b.Draw(t, rayLength);
            }
        }

        private bool TryCenter(GameEntityId id, out Vector3 center)
        {
            center = default;
            if (services == null || !services.Registry.TryGet(id, out var descriptor) || !(descriptor is GameEntity entity)) return false;
            center = entity.GetWorldBounds().center;
            return true;
        }

        private Orb AcquireOrb()
        {
            foreach (var o in orbs) if (!o.Active) return o;
            var line = NewLine("Orb_" + orbs.Count);
            var orb = new Orb { Line = line };
            orbs.Add(orb);
            return orb;
        }

        private Burst AcquireBurst()
        {
            foreach (var b in bursts) if (!b.Active) return b;
            var rays = new LineRenderer[RaysPerBurst];
            var directions = new Vector3[RaysPerBurst];
            var random = new System.Random(bursts.Count * 977 + 13);
            for (int i = 0; i < RaysPerBurst; i++)
            {
                rays[i] = NewLine("Ray_" + bursts.Count + "_" + i);
                var d = new Vector3((float)random.NextDouble() * 2f - 1f, (float)random.NextDouble() * 1.4f - 0.2f, (float)random.NextDouble() * 2f - 1f);
                directions[i] = d.sqrMagnitude < 0.01f ? Vector3.up : d.normalized;
            }
            var burst = new Burst { Rays = rays, Directions = directions };
            bursts.Add(burst);
            return burst;
        }

        private LineRenderer NewLine(string lineName)
        {
            var go = new GameObject(lineName, typeof(LineRenderer));
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
            return line;
        }

        private sealed class Orb
        {
            public LineRenderer Line;
            public Vector3 From, To;
            public Color Color;
            public float Start;
            public bool Active;
        }

        private sealed class Burst
        {
            public LineRenderer[] Rays;
            public Vector3[] Directions;
            public Vector3 Center;
            public Color Color;
            public float Start;
            public bool Active;

            public void Draw(float t, float length)
            {
                float outer = length * (1f - (1f - t) * (1f - t));
                float inner = outer * Mathf.Clamp01(t * 1.6f - 0.2f);
                var c = Color;
                c.a = 1f - t;
                float width = Mathf.Lerp(0.09f, 0.01f, t);
                for (int i = 0; i < Rays.Length; i++)
                {
                    var ray = Rays[i];
                    ray.enabled = true;
                    ray.SetPosition(0, Center + Directions[i] * inner);
                    ray.SetPosition(1, Center + Directions[i] * outer);
                    ray.startColor = c;
                    ray.endColor = new Color(c.r, c.g, c.b, 0f);
                    ray.startWidth = width;
                    ray.endWidth = width * 0.4f;
                }
            }

            public void Hide()
            {
                foreach (var ray in Rays) ray.enabled = false;
            }
        }
    }
}
