using System.Collections.Generic;
using BugReport.Core.Entities;

namespace BugReport.Core.Glitches
{
    /// <summary>A proposed application: which glitch, on which targets, owned by which (future) operation id.</summary>
    public readonly struct GlitchRequest
    {
        public readonly GlitchSpec Spec;
        public readonly IReadOnlyList<GameEntityId> Targets;
        /// <summary>The id the operation will get if applied; effects use it to test modifier conflicts.</summary>
        public readonly OperationId PendingId;

        public GlitchRequest(GlitchSpec spec, IReadOnlyList<GameEntityId> targets, OperationId pendingId)
        {
            Spec = spec;
            Targets = targets;
            PendingId = pendingId;
        }
    }
}
