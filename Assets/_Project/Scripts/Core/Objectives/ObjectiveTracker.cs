using System;
using System.Collections.Generic;

namespace BugReport.Core.Objectives
{
    public enum ObjectiveStatus
    {
        Pending,
        Active,
        Complete,
    }

    /// <summary>
    /// Sequential level objectives. Exactly one objective is active at a time; its condition
    /// is reported by the game (explicit conditions such as "hero is in zone C", never scene
    /// names). Completion latches and activates the next one; if the next condition already
    /// holds, the chain completes in the same call. The level is complete when all are.
    /// </summary>
    public sealed class ObjectiveTracker
    {
        private readonly string[] ids;
        private readonly ObjectiveStatus[] status;

        public ObjectiveTracker(IReadOnlyList<string> objectiveIds)
        {
            if (objectiveIds == null || objectiveIds.Count == 0) throw new ArgumentException("At least one objective is required.", nameof(objectiveIds));
            ids = new string[objectiveIds.Count];
            var seen = new HashSet<string>();
            for (int i = 0; i < ids.Length; i++)
            {
                if (string.IsNullOrEmpty(objectiveIds[i])) throw new ArgumentException("Objective ids must be non-empty.", nameof(objectiveIds));
                if (!seen.Add(objectiveIds[i])) throw new ArgumentException("Duplicate objective id '" + objectiveIds[i] + "'.", nameof(objectiveIds));
                ids[i] = objectiveIds[i];
            }
            status = new ObjectiveStatus[ids.Length];
            Reset();
        }

        public int Count => ids.Length;
        /// <summary>Index of the active objective, or -1 when everything is complete.</summary>
        public int CurrentIndex { get; private set; }
        public bool AllComplete => CurrentIndex < 0;
        public int CompletedCount { get; private set; }

        public event Action<int> Activated;
        public event Action<int> Completed;
        public event Action AllCompleted;

        public string Id(int index) => ids[index];
        public ObjectiveStatus Status(int index) => status[index];

        /// <summary>Reports whether objective <paramref name="index"/>'s condition currently holds. Ignored unless it is the active one.</summary>
        public void Report(int index, bool conditionMet)
        {
            if (index < 0 || index >= ids.Length) throw new ArgumentOutOfRangeException(nameof(index));
            if (!conditionMet || index != CurrentIndex) return;

            status[index] = ObjectiveStatus.Complete;
            CompletedCount++;
            Completed?.Invoke(index);

            if (index + 1 < ids.Length)
            {
                CurrentIndex = index + 1;
                status[CurrentIndex] = ObjectiveStatus.Active;
                Activated?.Invoke(CurrentIndex);
            }
            else
            {
                CurrentIndex = -1;
                AllCompleted?.Invoke();
            }
        }

        /// <summary>Evaluates conditions in order, completing as many as currently hold (one call per frame).</summary>
        public void Evaluate(Func<int, bool> condition)
        {
            if (condition == null) throw new ArgumentNullException(nameof(condition));
            while (!AllComplete)
            {
                int before = CurrentIndex;
                Report(CurrentIndex, condition(CurrentIndex));
                if (CurrentIndex == before) break;
            }
        }

        public void Reset()
        {
            for (int i = 0; i < status.Length; i++) status[i] = ObjectiveStatus.Pending;
            status[0] = ObjectiveStatus.Active;
            CurrentIndex = 0;
            CompletedCount = 0;
        }
    }
}
