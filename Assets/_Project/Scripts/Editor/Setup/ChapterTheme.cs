using UnityEngine;
using UnityEngine.Rendering;
using static BugReport.EditorTools.Setup.TestRoomBuilder;

namespace BugReport.EditorTools.Setup
{
    /// <summary>
    /// Per-chapter atmosphere: gradient sky, fog, ambient trilight and sun. Chapter numbers match the
    /// story arc (1 tutorial garden … 7 the report room); anything out of range uses chapter 1.
    /// </summary>
    internal static class ChapterTheme
    {
        internal const int ChapterCount = 7;

        private struct Look
        {
            public Color Top, Horizon, Bottom, Fog, Sun;
            public float FogDensity, SunIntensity, Stars, Grid;
            public Vector3 SunAngles;
        }

        private static Look For(int chapter)
        {
            switch (chapter)
            {
                case 2: return Make("5b7fa8", "e8b27a", "2a2230", "c99a78", "ffd0a0", 0.012f, 1.35f, 0.05f, 0f, 28f, -50f);
                case 3: return Make("0b1a33", "2f6f9f", "07101c", "12304f", "9fd8ff", 0.02f, 1.0f, 0.5f, 0.5f, 55f, -20f);
                case 4: return Make("1a1233", "b04a8f", "0a0716", "3a1a4d", "ffb0e0", 0.02f, 1.05f, 0.7f, 0.7f, 35f, -60f);
                case 5: return Make("0a0f1c", "1f4d5c", "05080d", "0b2230", "7fe8ff", 0.025f, 0.95f, 0.4f, 1f, 60f, 10f);
                case 6: return Make("120a1f", "5a2a52", "07040c", "24102c", "c9a0ff", 0.03f, 0.85f, 0.9f, 0.3f, 25f, 40f);
                case 7: return Make("000000", "8a1a2a", "000000", "1a0509", "ff5a5a", 0.03f, 0.9f, 1f, 1f, 45f, -30f);
                default: return Make("3f7fcf", "bfe0ff", "334455", "a8c8e8", "fff2d8", 0.008f, 1.35f, 0f, 0f, 50f, -35f);
            }
        }

        private static Look Make(string top, string horizon, string bottom, string fog, string sun, float density,
            float sunIntensity, float stars, float grid, float sunPitch, float sunYaw) => new Look
        {
            Top = Hex(top), Horizon = Hex(horizon), Bottom = Hex(bottom), Fog = Hex(fog), Sun = Hex(sun),
            FogDensity = density, SunIntensity = sunIntensity, Stars = stars, Grid = grid,
            SunAngles = new Vector3(sunPitch, sunYaw, 0f)
        };

        private static Color Hex(string rgb)
        {
            ColorUtility.TryParseHtmlString("#" + rgb, out var c);
            return c;
        }

        /// <summary>Applies the chapter's look to the open scene. Safe without the sky shader (keeps the default sky).</summary>
        internal static void Apply(int chapter, Light sun)
        {
            var look = For(chapter < 1 || chapter > ChapterCount ? 1 : chapter);

            var shader = Shader.Find("BugReport/GradientSky");
            if (shader != null)
            {
                var m = new Material(shader) { name = "M_Sky_Ch" + chapter };
                m.SetColor("_Top", look.Top);
                m.SetColor("_Horizon", look.Horizon);
                m.SetColor("_Bottom", look.Bottom);
                m.SetFloat("_Stars", look.Stars);
                m.SetFloat("_Grid", look.Grid);
                RenderSettings.skybox = MaterialSet.Save(m, "M_Sky_Ch" + chapter);
            }

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Color.Lerp(look.Top, look.Horizon, 0.5f);
            RenderSettings.ambientEquatorColor = Color.Lerp(look.Horizon, look.Fog, 0.5f);
            RenderSettings.ambientGroundColor = look.Bottom;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = look.Fog;
            RenderSettings.fogDensity = look.FogDensity;

            if (sun != null)
            {
                sun.color = look.Sun;
                sun.intensity = look.SunIntensity;
                sun.transform.rotation = Quaternion.Euler(look.SunAngles);
            }
        }
    }
}
