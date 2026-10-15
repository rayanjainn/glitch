using UnityEngine;
using UnityEngine.Rendering;

namespace BugReport.Presentation
{
    /// <summary>
    /// The highlight under a targeted object: a glowing rounded-rectangle ring on the floor around its footprint
    /// (one closed LineRenderer). It replaces the old wireframe box, which read as a debug view.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public sealed class BoundsOutline : MonoBehaviour
    {
        private const int ArcSteps = 7;
        private const int PointCount = (ArcSteps + 1) * 4;
        private static readonly Vector3[] Scratch = new Vector3[PointCount];

        private LineRenderer line;

        public void Initialize(Material material, float width)
        {
            line = GetComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = true;
            line.loop = true;
            line.positionCount = PointCount;
            line.startWidth = width * 2.2f;
            line.endWidth = width * 2.2f;
            line.numCornerVertices = 2;
            line.numCapVertices = 2;
            line.alignment = LineAlignment.View;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.allowOcclusionWhenDynamic = false;
            line.enabled = false;
        }

        public void Show(Bounds bounds, Color color)
        {
            if (line == null) return;
            var c = bounds.center;
            float hx = bounds.extents.x + 0.12f;
            float hz = bounds.extents.z + 0.12f;
            float y = bounds.min.y + 0.04f;
            float r = Mathf.Min(0.6f, Mathf.Min(hx, hz));

            int n = 0;
            // Four quarter arcs, counter-clockwise from the +x/+z corner.
            n = Arc(n, c.x + hx - r, c.z + hz - r, y, r, 0f);
            n = Arc(n, c.x - hx + r, c.z + hz - r, y, r, 90f);
            n = Arc(n, c.x - hx + r, c.z - hz + r, y, r, 180f);
            n = Arc(n, c.x + hx - r, c.z - hz + r, y, r, 270f);

            line.SetPositions(Scratch);
            line.startColor = color;
            line.endColor = color;
            line.enabled = true;
        }

        private static int Arc(int index, float cx, float cz, float y, float r, float startDegrees)
        {
            for (int i = 0; i <= ArcSteps; i++)
            {
                float a = (startDegrees + 90f * i / ArcSteps) * Mathf.Deg2Rad;
                Scratch[index++] = new Vector3(cx + Mathf.Cos(a) * r, y, cz + Mathf.Sin(a) * r);
            }
            return index;
        }

        public void Hide()
        {
            if (line != null) line.enabled = false;
        }
    }
}
