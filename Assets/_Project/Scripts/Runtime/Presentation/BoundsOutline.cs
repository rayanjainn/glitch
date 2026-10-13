using UnityEngine;
using UnityEngine.Rendering;

namespace BugReport.Presentation
{
    /// <summary>
    /// Draws a world-space wireframe box with a single LineRenderer.
    /// The 16-point path covers all 12 edges (three edges are traced twice).
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public sealed class BoundsOutline : MonoBehaviour
    {
        private const int PointCount = 16;
        private static readonly Vector3[] Scratch = new Vector3[PointCount];

        private LineRenderer line;

        public void Initialize(Material material, float width)
        {
            line = GetComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = true;
            line.loop = false;
            line.positionCount = PointCount;
            line.startWidth = width;
            line.endWidth = width;
            line.numCornerVertices = 0;
            line.numCapVertices = 0;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.allowOcclusionWhenDynamic = false;
            line.enabled = false;
        }

        public void Show(Bounds bounds, Color color)
        {
            if (line == null) return;
            var min = bounds.min;
            var max = bounds.max;
            var p0 = new Vector3(min.x, min.y, min.z);
            var p1 = new Vector3(max.x, min.y, min.z);
            var p2 = new Vector3(max.x, min.y, max.z);
            var p3 = new Vector3(min.x, min.y, max.z);
            var p4 = new Vector3(min.x, max.y, min.z);
            var p5 = new Vector3(max.x, max.y, min.z);
            var p6 = new Vector3(max.x, max.y, max.z);
            var p7 = new Vector3(min.x, max.y, max.z);

            // bottom loop, up, then zig-zag around the top covering the remaining verticals
            Scratch[0] = p0; Scratch[1] = p1; Scratch[2] = p2; Scratch[3] = p3; Scratch[4] = p0;
            Scratch[5] = p4; Scratch[6] = p5; Scratch[7] = p1; Scratch[8] = p5; Scratch[9] = p6;
            Scratch[10] = p2; Scratch[11] = p6; Scratch[12] = p7; Scratch[13] = p3; Scratch[14] = p7;
            Scratch[15] = p4;

            line.SetPositions(Scratch);
            line.startColor = color;
            line.endColor = color;
            line.enabled = true;
        }

        public void Hide()
        {
            if (line != null) line.enabled = false;
        }
    }
}
