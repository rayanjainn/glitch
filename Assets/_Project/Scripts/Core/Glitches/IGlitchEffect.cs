namespace BugReport.Core.Glitches
{
    /// <summary>
    /// Implements one glitch. The <see cref="GlitchManager"/> owns the workflow
    /// (validation of slots, cooldowns, limits, lifetime, cleanup); an effect only
    /// knows how to check and perform its own change.
    ///
    /// Contract:
    /// - <see cref="Validate"/> has no side effects.
    /// - <see cref="Apply"/> expresses every change as modifier leases added to the
    ///   operation. If it returns false, the manager releases any leases already added,
    ///   so a partial apply never leaks.
    /// - Restoration is the manager releasing those leases; <see cref="OnEnded"/> is for
    ///   anything else (feedback, bookkeeping) and must tolerate destroyed targets.
    /// </summary>
    public interface IGlitchEffect
    {
        /// <summary>Effect-specific checks beyond slot requirements. Return null when valid, otherwise a short player-facing reason.</summary>
        string Validate(GlitchRequest request);

        /// <summary>Performs the change. Return false with a reason to abort.</summary>
        bool Apply(GlitchOperation operation, out string failure);

        /// <summary>
        /// Lets an effect postpone a non-forced ending (e.g. a wall cannot become solid while
        /// the hero is inside it). Return null to allow, otherwise the reason. Never called for
        /// forced reasons (<see cref="GlitchEndReasonExtensions.IsForced"/>).
        /// </summary>
        string CanEnd(GlitchOperation operation, GlitchEndReason reason);

        /// <summary>Called after the operation's leases were released.</summary>
        void OnEnded(GlitchOperation operation, GlitchEndReason reason);
    }
}
