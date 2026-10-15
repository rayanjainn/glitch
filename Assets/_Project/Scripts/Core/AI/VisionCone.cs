using System;

namespace BugReport.Core.AI
{
    /// <summary>
    /// Flat (x/z plane) field-of-view test for guards and drones. Line of sight and height are checked
    /// by the adapter with physics; this is the pure geometry part.
    /// </summary>
    public static class VisionCone
    {
        public static bool Contains(float observerX, float observerZ, float forwardX, float forwardZ,
            float targetX, float targetZ, float range, float halfAngleDegrees)
        {
            float dx = targetX - observerX;
            float dz = targetZ - observerZ;
            float distSq = dx * dx + dz * dz;
            if (distSq > range * range) return false;
            if (distSq < 1e-6f) return true; // standing on the observer

            float fLen = (float)Math.Sqrt(forwardX * forwardX + forwardZ * forwardZ);
            if (fLen < 1e-6f) return false;
            float dist = (float)Math.Sqrt(distSq);
            float cos = (dx * forwardX + dz * forwardZ) / (dist * fLen);
            float limit = (float)Math.Cos(Math.Max(0f, Math.Min(180f, halfAngleDegrees)) * Math.PI / 180.0);
            return cos >= limit;
        }
    }
}
