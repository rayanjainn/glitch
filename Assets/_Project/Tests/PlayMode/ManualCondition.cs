using BugReport.Level;

namespace BugReport.Tests.PlayMode
{
    /// <summary>Objective condition a test sets directly.</summary>
    public sealed class ManualCondition : ObjectiveCondition
    {
        public bool met;
        public override bool IsMet => met;
    }
}
