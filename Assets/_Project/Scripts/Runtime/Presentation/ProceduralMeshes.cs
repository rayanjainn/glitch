using UnityEngine;

namespace BugReport.Presentation
{
    /// <summary>Small generated meshes for placeholder art (no imported models).</summary>
    public static class ProceduralMeshes
    {
        /// <summary>Flat-shaded regular tetrahedron with unit circumradius, centred on the origin.</summary>
        public static Mesh Tetrahedron()
        {
            var p = new[]
            {
                new Vector3(1f, 1f, 1f), new Vector3(-1f, -1f, 1f), new Vector3(-1f, 1f, -1f), new Vector3(1f, -1f, -1f),
            };
            for (int i = 0; i < p.Length; i++) p[i] = p[i] / Mathf.Sqrt(3f);
            int[][] faces = { new[] { 0, 1, 2 }, new[] { 0, 3, 1 }, new[] { 0, 2, 3 }, new[] { 1, 3, 2 } };

            var vertices = new Vector3[12];
            var triangles = new int[12];
            for (int f = 0; f < 4; f++)
            {
                for (int k = 0; k < 3; k++)
                {
                    vertices[f * 3 + k] = p[faces[f][k]];
                    triangles[f * 3 + k] = f * 3 + k;
                }
            }
            var mesh = new Mesh { name = "Tetrahedron", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Square grid texture (thin bright lines on a dark-ish base) for floors; one cell per texture.</summary>
        public static Texture2D GridTexture(int size, Color background, Color line, int lineWidth)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true) { name = "Grid", wrapMode = TextureWrapMode.Repeat };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool onLine = x < lineWidth || y < lineWidth;
                    pixels[y * size + x] = onLine ? line : background;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }
    }
}
