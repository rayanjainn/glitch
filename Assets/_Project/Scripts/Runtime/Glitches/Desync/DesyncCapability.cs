using BugReport.Core.Glitches;
using UnityEngine;

namespace BugReport.Glitches
{
    /// <summary>
    /// Makes an entity's collision separable from its looks (GDD 4.6, in its controlled form): every
    /// collider lives under <c>collisionRoot</c>, which Desync moves by a fixed authored offset while
    /// the visible mesh stays put. A pulsing outline shows where the collision really is, so the
    /// player can always tell what is actually happening. Reverting snaps the collision back.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GameEntity))]
    public sealed class DesyncCapability : MonoBehaviour
    {
        [SerializeField] private Transform collisionRoot;
        [SerializeField, Tooltip("Where the collision goes, in this object's local axes.")] private Vector3 offset = new Vector3(0f, 0f, -3f);
        [SerializeField, OptionalReference] private Material ghostMaterial;

        private ModifierStack<Vector3> channel;
        private Vector3 restLocalPosition;
        private LineRenderer ghost;
        private readonly Vector3[] outline = new Vector3[16];
        private Collider[] colliders;

        public Vector3 Offset => offset;
        public Transform CollisionRoot => collisionRoot;
        public bool IsDesynced => channel != null && channel.IsModified;

        public ModifierStack<Vector3> Channel
        {
            get
            {
                EnsureInitialized();
                return channel;
            }
        }

        public void Configure(Transform root, Vector3 newOffset, Material outlineMaterial)
        {
            collisionRoot = root;
            offset = newOffset;
            ghostMaterial = outlineMaterial;
        }

        private void Awake() => EnsureInitialized();

        private void EnsureInitialized()
        {
            if (channel != null) return;
            if (collisionRoot == null) collisionRoot = transform;
            restLocalPosition = collisionRoot.localPosition;
            colliders = collisionRoot.GetComponentsInChildren<Collider>(true);
            channel = new ModifierStack<Vector3>(Vector3.zero, ConflictPolicy.Exclusive, Apply);
        }

        /// <summary>Colliders (solid) that move with the desync.</summary>
        public Collider[] Colliders
        {
            get
            {
                EnsureInitialized();
                return colliders;
            }
        }

        /// <summary>World bounds of the collision at its current position.</summary>
        public Bounds CollisionBounds()
        {
            EnsureInitialized();
            bool has = false;
            var bounds = new Bounds(collisionRoot.position, Vector3.zero);
            foreach (var c in colliders)
            {
                if (c == null || c.isTrigger) continue;
                if (has) bounds.Encapsulate(c.bounds);
                else { bounds = c.bounds; has = true; }
            }
            return bounds;
        }

        private void Apply(Vector3 value)
        {
            if (this == null || collisionRoot == null) return;
            collisionRoot.localPosition = restLocalPosition + value;
            Physics.SyncTransforms();
            if (ghost != null) ghost.enabled = value != Vector3.zero;
            if (value != Vector3.zero && ghost == null) CreateGhost();
            if (ghost != null) ghost.enabled = value != Vector3.zero;
        }

        private void CreateGhost()
        {
            var go = new GameObject("DesyncOutline");
            go.transform.SetParent(transform, false);
            ghost = go.AddComponent<LineRenderer>();
            ghost.useWorldSpace = true;
            ghost.loop = false;
            ghost.positionCount = 16;
            ghost.widthMultiplier = 0.05f;
            ghost.numCapVertices = 2;
            if (ghostMaterial != null) ghost.sharedMaterial = ghostMaterial;
            ghost.startColor = ghost.endColor = new Color(0.6f, 1f, 0.35f, 1f);
            ghost.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ghost.receiveShadows = false;
        }

        private void LateUpdate()
        {
            if (ghost == null || !ghost.enabled) return;
            var b = CollisionBounds();
            var min = b.min;
            var max = b.max;
            var p = outline;
            p[0] = new Vector3(min.x, min.y, min.z); p[1] = new Vector3(max.x, min.y, min.z); p[2] = new Vector3(max.x, min.y, max.z); p[3] = new Vector3(min.x, min.y, max.z);
            p[4] = new Vector3(min.x, min.y, min.z); p[5] = new Vector3(min.x, max.y, min.z); p[6] = new Vector3(max.x, max.y, min.z); p[7] = new Vector3(max.x, min.y, min.z);
            p[8] = new Vector3(max.x, max.y, min.z); p[9] = new Vector3(max.x, max.y, max.z); p[10] = new Vector3(max.x, min.y, max.z); p[11] = new Vector3(max.x, max.y, max.z);
            p[12] = new Vector3(min.x, max.y, max.z); p[13] = new Vector3(min.x, min.y, max.z); p[14] = new Vector3(min.x, max.y, max.z); p[15] = new Vector3(min.x, max.y, min.z);
            ghost.SetPositions(p);
            float pulse = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 6f);
            ghost.startColor = ghost.endColor = new Color(0.6f * pulse + 0.2f, 1f, 0.35f * pulse + 0.1f, 1f);
        }
    }
}
