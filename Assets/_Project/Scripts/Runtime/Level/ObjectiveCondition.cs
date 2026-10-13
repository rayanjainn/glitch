using UnityEngine;

namespace BugReport.Level
{
    /// <summary>An explicit, inspectable condition an objective waits for.</summary>
    public abstract class ObjectiveCondition : MonoBehaviour
    {
        public abstract bool IsMet { get; }
    }
}
