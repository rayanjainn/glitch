using System;

namespace BugReport.Core.Tutorial
{
    /// <summary>
    /// Escalating help for a stuck player, so the level never has to state its own solution up front.
    /// Time without progress raises the tier: 0 nothing, 1 gentle pulse on the relevant objects,
    /// 2 the objective's hint text, 3 a pointed marker on the solution's key object. Any progress (a
    /// new objective, or the player using a glitch) restarts the clock; using a glitch also keeps it
    /// from escalating straight back to full help.
    /// </summary>
    public sealed class StuckHints
    {
        private readonly float[] thresholds;
        private float idle;
        private int objective = -1;
        private int actions;

        public StuckHints(float pulseAfter = 25f, float hintAfter = 50f, float markerAfter = 100f)
        {
            if (!(pulseAfter > 0f) || hintAfter < pulseAfter || markerAfter < hintAfter) throw new ArgumentException("Thresholds must be positive and increasing.");
            thresholds = new[] { pulseAfter, hintAfter, markerAfter };
        }

        public int Tier { get; private set; }
        public float IdleSeconds => idle;

        /// <summary>Call every frame with unscaled gameplay time (not while paused).</summary>
        public int Update(float deltaTime, int currentObjective, int glitchActions)
        {
            if (currentObjective != objective)
            {
                objective = currentObjective;
                actions = glitchActions;
                idle = 0f;
            }
            else if (glitchActions != actions)
            {
                actions = glitchActions;
                idle = Math.Max(0f, idle - thresholds[0]); // trying things is not being stuck
            }
            idle += Math.Max(0f, deltaTime);

            int tier = 0;
            for (int i = 0; i < thresholds.Length; i++) if (idle >= thresholds[i]) tier = i + 1;
            Tier = tier;
            return tier;
        }

        public void Reset()
        {
            idle = 0f;
            Tier = 0;
            objective = -1;
        }
    }
}
