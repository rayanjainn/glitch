using BugReport.Core.Entities;
using BugReport.Core.Glitches;
using UnityEngine;

namespace BugReport.Glitches
{
    /// <summary>Runtime-only lineage stamped on every copy: where it came from and which operation owns it.</summary>
    [DisallowMultipleComponent]
    public sealed class DuplicateMarker : MonoBehaviour
    {
        public GameEntityId Origin { get; private set; }
        /// <summary>1 for a copy of an original, 2 for a copy of a copy, …</summary>
        public int Generation { get; private set; }
        public OperationId Operation { get; private set; }

        public void Stamp(GameEntityId origin, int generation, OperationId operation)
        {
            Origin = origin;
            Generation = generation;
            Operation = operation;
        }
    }
}
