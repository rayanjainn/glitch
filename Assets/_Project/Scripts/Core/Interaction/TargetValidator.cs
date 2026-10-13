using BugReport.Core.Entities;

namespace BugReport.Core.Interaction
{
    public enum TargetValidationFailure
    {
        None = 0,
        NoTarget,
        Destroyed,
        CategoryNotAllowed,
        MissingCapability,
    }

    public readonly struct TargetValidationResult
    {
        public readonly TargetValidationFailure Failure;
        /// <summary>Capabilities the target lacked (only set for <see cref="TargetValidationFailure.MissingCapability"/>).</summary>
        public readonly EntityCapabilities MissingCapabilities;

        public TargetValidationResult(TargetValidationFailure failure, EntityCapabilities missing = EntityCapabilities.None)
        {
            Failure = failure;
            MissingCapabilities = missing;
        }

        public bool IsValid => Failure == TargetValidationFailure.None;
        public static TargetValidationResult Valid => new TargetValidationResult(TargetValidationFailure.None);

        /// <summary>Short player-facing explanation (UI may localize/replace later).</summary>
        public string Describe()
        {
            switch (Failure)
            {
                case TargetValidationFailure.None: return "OK";
                case TargetValidationFailure.NoTarget: return "NO TARGET";
                case TargetValidationFailure.Destroyed: return "TARGET LOST";
                case TargetValidationFailure.CategoryNotAllowed: return "ACCESS DENIED: PROTECTED ENTITY";
                case TargetValidationFailure.MissingCapability: return "INCOMPATIBLE: REQUIRES " + MissingCapabilities.ToString().ToUpperInvariant();
                default: return Failure.ToString();
            }
        }
    }

    /// <summary>Stateless validation of an entity against a <see cref="TargetRequirement"/>.</summary>
    public static class TargetValidator
    {
        public static TargetValidationResult Validate(IEntityDescriptor entity, in TargetRequirement requirement)
        {
            if (entity == null) return new TargetValidationResult(TargetValidationFailure.NoTarget);
            if (!entity.IsAlive) return new TargetValidationResult(TargetValidationFailure.Destroyed);
            if (requirement.Excludes(entity.Category)) return new TargetValidationResult(TargetValidationFailure.CategoryNotAllowed);

            var missing = requirement.RequiredCapabilities & ~entity.Capabilities;
            if (missing != EntityCapabilities.None)
                return new TargetValidationResult(TargetValidationFailure.MissingCapability, missing);

            return TargetValidationResult.Valid;
        }
    }
}
