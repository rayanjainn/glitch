using System.Collections.Generic;
using UnityEngine;

namespace BugReport.Presentation
{
    /// <summary>
    /// Arbitrates material overrides on one entity's renderers. Several visuals (collision
    /// ghost, freeze, …) can request an override at once; the highest priority wins and the
    /// originals return when all are cleared. Without this, two visuals restoring "their"
    /// remembered materials would overwrite each other.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RendererOverrides : MonoBehaviour
    {
        private readonly List<Request> requests = new List<Request>();
        private readonly List<Renderer> renderers = new List<Renderer>();
        private Material[][] originals;
        private Material applied;

        public int RequestCount => requests.Count;
        public Material Current => applied;

        private void Awake() => CaptureIfNeeded();

        /// <summary>Adds or updates the override for <paramref name="key"/>.</summary>
        public void Set(string key, Material material, int priority)
        {
            CaptureIfNeeded();
            for (int i = 0; i < requests.Count; i++)
            {
                if (requests[i].Key == key)
                {
                    requests[i] = new Request(key, material, priority);
                    Refresh();
                    return;
                }
            }
            requests.Add(new Request(key, material, priority));
            Refresh();
        }

        public void Clear(string key)
        {
            for (int i = requests.Count - 1; i >= 0; i--)
            {
                if (requests[i].Key == key) requests.RemoveAt(i);
            }
            Refresh();
        }

        private void CaptureIfNeeded()
        {
            if (originals != null) return;
            GetComponentsInChildren(true, renderers);
            renderers.RemoveAll(r => r is LineRenderer || r is TrailRenderer || r is ParticleSystemRenderer);
            originals = new Material[renderers.Count][];
            for (int i = 0; i < renderers.Count; i++) originals[i] = renderers[i].sharedMaterials;
        }

        private void Refresh()
        {
            if (this == null) return;
            Material winner = null;
            int best = int.MinValue;
            for (int i = 0; i < requests.Count; i++)
            {
                if (requests[i].Material != null && requests[i].Priority > best)
                {
                    best = requests[i].Priority;
                    winner = requests[i].Material;
                }
            }
            if (winner == applied) return;
            applied = winner;

            for (int i = 0; i < renderers.Count; i++)
            {
                var r = renderers[i];
                if (r == null) continue;
                if (winner == null)
                {
                    r.sharedMaterials = originals[i];
                    continue;
                }
                var swapped = new Material[originals[i].Length];
                for (int m = 0; m < swapped.Length; m++) swapped[m] = winner;
                r.sharedMaterials = swapped;
            }
        }

        private readonly struct Request
        {
            public readonly string Key;
            public readonly Material Material;
            public readonly int Priority;

            public Request(string key, Material material, int priority)
            {
                Key = key;
                Material = material;
                Priority = priority;
            }
        }
    }
}
