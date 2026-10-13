using BugReport.Core.Interaction;

namespace BugReport.Core.Glitches
{
    public enum GlitchFailure
    {
        None = 0,
        ManagerDisposed,
        WrongTargetCount,
        DuplicateTarget,
        InvalidTarget,
        OnCooldown,
        LimitReached,
        EffectRejected,
        EffectFailed,
        NotFound,
        AlreadyEnded,
        EndBlocked,
    }

    /// <summary>Outcome of an apply or revert request.</summary>
    public readonly struct GlitchResult
    {
        public readonly GlitchFailure Failure;
        public readonly GlitchOperation Operation;
        /// <summary>For <see cref="GlitchFailure.InvalidTarget"/>: which slot failed, and why.</summary>
        public readonly int TargetIndex;
        public readonly TargetValidationResult TargetValidation;
        /// <summary>Effect-provided reason, or cooldown seconds formatted, etc.</summary>
        public readonly string Detail;

        public GlitchResult(GlitchFailure failure, GlitchOperation operation = null, string detail = null,
            int targetIndex = -1, TargetValidationResult targetValidation = default)
        {
            Failure = failure;
            Operation = operation;
            Detail = detail;
            TargetIndex = targetIndex;
            TargetValidation = targetValidation;
        }

        public bool Succeeded => Failure == GlitchFailure.None;

        public static GlitchResult Success(GlitchOperation operation) => new GlitchResult(GlitchFailure.None, operation);

        /// <summary>Short player-facing text.</summary>
        public string Describe()
        {
            switch (Failure)
            {
                case GlitchFailure.None: return "OK";
                case GlitchFailure.ManagerDisposed: return "SYSTEM OFFLINE";
                case GlitchFailure.WrongTargetCount: return "WRONG NUMBER OF TARGETS";
                case GlitchFailure.DuplicateTarget: return "SOURCE AND TARGET MUST DIFFER";
                case GlitchFailure.InvalidTarget:
                    return (TargetIndex == 0 ? "SOURCE " : TargetIndex == 1 ? "TARGET " : string.Empty) + TargetValidation.Describe();
                case GlitchFailure.OnCooldown: return "COOLDOWN " + Detail;
                case GlitchFailure.LimitReached: return "LIMIT REACHED: REVERT ONE FIRST";
                case GlitchFailure.EffectRejected:
                case GlitchFailure.EffectFailed:
                case GlitchFailure.EndBlocked:
                    return string.IsNullOrEmpty(Detail) ? Failure.ToString().ToUpperInvariant() : Detail;
                case GlitchFailure.NotFound: return "NOTHING TO REVERT";
                case GlitchFailure.AlreadyEnded: return "ALREADY RESTORED";
                default: return Failure.ToString();
            }
        }
    }
}
