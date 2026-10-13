using UnityEngine;

namespace BugReport.Anomaly
{
    /// <summary>
    /// Placeholder presentation for the anomaly: a bobbing core with orbiting
    /// polygon fragments. Purely visual; no gameplay reads from it.
    /// </summary>
    public sealed class AnomalyVisual : MonoBehaviour
    {
        [SerializeField] private Transform core;
        [SerializeField] private Transform[] fragments = new Transform[0];
        [SerializeField] private float orbitRadius = 0.42f;
        [SerializeField] private float orbitSpeed = 110f;
        [SerializeField] private float fragmentSpin = 220f;
        [SerializeField] private float bobAmplitude = 0.06f;
        [SerializeField] private float bobFrequency = 1.4f;

        private Vector3 coreRestPosition;

        private void Awake()
        {
            if (core != null) coreRestPosition = core.localPosition;
        }

        private void Update()
        {
            float t = Time.time;
            if (core != null)
            {
                core.localPosition = coreRestPosition + Vector3.up * (Mathf.Sin(t * bobFrequency * Mathf.PI * 2f) * bobAmplitude);
            }

            int count = fragments.Length;
            for (int i = 0; i < count; i++)
            {
                var fragment = fragments[i];
                if (fragment == null) continue;
                float phase = i * (360f / Mathf.Max(1, count));
                // Each fragment orbits on a differently tilted plane.
                var plane = Quaternion.Euler(25f + 40f * i, phase, 0f);
                var angle = Quaternion.AngleAxis(t * orbitSpeed + phase, Vector3.up);
                fragment.localPosition = (core != null ? core.localPosition : Vector3.zero) + plane * (angle * Vector3.forward) * orbitRadius;
                fragment.localRotation = Quaternion.Euler(t * fragmentSpin + phase, t * fragmentSpin * 0.7f, 0f);
            }
        }
    }
}
