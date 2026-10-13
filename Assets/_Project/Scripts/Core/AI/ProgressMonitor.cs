using System;

namespace BugReport.Core.AI
{
    /// <summary>
    /// Detects when an agent stops making progress toward a goal (e.g. the hero
    /// walking into a wall). Progress means the distance to the goal dropped by at
    /// least <see cref="MinProgress"/> below the best distance seen so far.
    /// </summary>
    public sealed class ProgressMonitor
    {
        private float bestDistance;
        private bool hasSample;

        public ProgressMonitor(float minProgress, float timeout)
        {
            if (minProgress <= 0f) throw new ArgumentOutOfRangeException(nameof(minProgress));
            if (timeout <= 0f) throw new ArgumentOutOfRangeException(nameof(timeout));
            MinProgress = minProgress;
            Timeout = timeout;
        }

        public float MinProgress { get; }
        public float Timeout { get; }
        public float StalledTime { get; private set; }
        public bool IsStuck => StalledTime >= Timeout;

        public void Reset()
        {
            hasSample = false;
            StalledTime = 0f;
        }

        /// <summary>Feeds the current distance to the goal. Returns <see cref="IsStuck"/>.</summary>
        public bool Tick(float distanceToGoal, float deltaTime)
        {
            if (deltaTime < 0f) throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (float.IsNaN(distanceToGoal)) throw new ArgumentException("Distance is NaN.", nameof(distanceToGoal));

            if (!hasSample)
            {
                hasSample = true;
                bestDistance = distanceToGoal;
                StalledTime = 0f;
                return false;
            }

            if (distanceToGoal <= bestDistance - MinProgress)
            {
                bestDistance = distanceToGoal;
                StalledTime = 0f;
            }
            else
            {
                StalledTime += deltaTime;
            }
            return IsStuck;
        }
    }
}
