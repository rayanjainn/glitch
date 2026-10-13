using UnityEngine;

namespace BugReport.Presentation
{
    /// <summary>Shared helper for IMGUI labels anchored to world positions.</summary>
    public static class WorldLabel
    {
        /// <summary>Screen rect centred above a world point, or false if behind the camera / too far.</summary>
        public static bool TryGetRect(Camera cam, Vector3 world, Vector2 size, float maxDistance, out Rect rect)
        {
            rect = default;
            if (cam == null) return false;
            var screen = cam.WorldToScreenPoint(world);
            if (screen.z <= 0.1f || screen.z > maxDistance) return false;
            float y = Screen.height - screen.y; // IMGUI origin is top-left
            rect = new Rect(screen.x - size.x * 0.5f, y - size.y, size.x, size.y);
            return true;
        }

        public static void Panel(Rect rect, Color color)
        {
            var previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }
    }
}
