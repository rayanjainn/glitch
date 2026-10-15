using BugReport.Containment;
using BugReport.Glitches;
using UnityEngine;

namespace BugReport.Guards
{
    /// <summary>
    /// A hovering security drone that sweeps a searchlight over the floor along a route. While the
    /// anomaly is inside the light (and nothing solid is between them) the system "sights" it and
    /// containment pressure climbs. It does not touch the hero: it is the game noticing you (GDD
    /// pillar C). Freezing it blinds it.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GameEntity))]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class SecurityDrone : MonoBehaviour, ITimeScaled
    {
        [SerializeField] private Transform[] route = new Transform[0];
        [SerializeField, Min(0.1f)] private float speed = 2.2f;
        [SerializeField, Min(0.5f)] private float searchRadius = 3.2f;
        [SerializeField, Min(0f)] private float hoverBob = 0.12f;
        [SerializeField, OptionalReference] private Transform anomaly;
        [SerializeField, OptionalReference] private ContainmentDirector containment;
        [SerializeField, OptionalReference, Tooltip("Flat disc on the floor that follows the drone.")] private Transform searchDisc;
        [SerializeField, OptionalReference] private Renderer discRenderer;

        private FreezeCapability freeze;
        private Rigidbody body;
        private int index;
        private int direction = 1;
        private Vector3 start;

        public bool IsFrozen => freeze != null && freeze.IsFrozen;
        public bool SeesAnomaly { get; private set; }
        public float SearchRadius => searchRadius;

        public void Configure(Transform[] newRoute, Transform anomalyTransform, ContainmentDirector containmentDirector, Transform disc, Renderer discRend)
        {
            route = newRoute ?? new Transform[0];
            anomaly = anomalyTransform;
            containment = containmentDirector;
            searchDisc = disc;
            discRenderer = discRend;
        }

        private void Awake()
        {
            freeze = GetComponent<FreezeCapability>();
            body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            start = transform.position;
        }

        private void Update() => Step(Time.deltaTime);

        /// <summary>Advances the drone. Public for tests.</summary>
        public void Step(float deltaTime)
        {
            if (deltaTime <= 0f) return;
            SeesAnomaly = false;
            if (IsFrozen)
            {
                UpdateDisc();
                return;
            }

            if (route.Length > 1)
            {
                var target = route[index].position;
                var to = target - transform.position;
                to.y = 0f;
                if (to.magnitude < 0.2f)
                {
                    if (index + direction < 0 || index + direction >= route.Length) direction = -direction;
                    index += direction;
                }
                else
                {
                    var pos = transform.position + to.normalized * Mathf.Min(to.magnitude, speed * deltaTime);
                    pos.y = start.y + Mathf.Sin(Time.time * 2f) * hoverBob;
                    body.position = pos;
                    transform.position = pos;
                }
            }

            if (anomaly != null)
            {
                var d = anomaly.position - transform.position;
                float flat = new Vector2(d.x, d.z).magnitude;
                if (flat <= searchRadius && anomaly.position.y < transform.position.y &&
                    !Physics.Linecast(transform.position, anomaly.position, GameLayers.HeroBlockingMask, QueryTriggerInteraction.Ignore))
                {
                    SeesAnomaly = true;
                    if (containment != null) containment.ReportSighted();
                }
            }
            UpdateDisc();
        }

        private void UpdateDisc()
        {
            if (searchDisc == null) return;
            var p = transform.position;
            searchDisc.position = new Vector3(p.x, 0.04f, p.z);
            searchDisc.localScale = new Vector3(searchRadius * 2f, 0.01f, searchRadius * 2f);
            if (discRenderer != null)
            {
                var block = new MaterialPropertyBlock();
                discRenderer.GetPropertyBlock(block);
                var color = IsFrozen ? new Color(0.5f, 0.8f, 1f, 0.12f) : SeesAnomaly ? new Color(1f, 0.15f, 0.2f, 0.45f) : new Color(1f, 0.85f, 0.3f, 0.25f);
                block.SetColor("_BaseColor", color);
                block.SetColor("_EmissionColor", new Color(color.r, color.g, color.b) * 1.4f);
                discRenderer.SetPropertyBlock(block);
            }
        }
    }
}
