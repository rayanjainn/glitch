using BugReport.Core.Glitches;
using UnityEngine;

namespace BugReport.Guards
{
    /// <summary>
    /// Marks a prop as a decoy that can hold only one AI Override at a time ("one decoy, one lure"). Props
    /// without this component can be used by several overrides at once.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DecoyMarker : MonoBehaviour
    {
        private ModifierStack<bool> channel;

        public ModifierStack<bool> Channel => channel ??= new ModifierStack<bool>(false, ConflictPolicy.Exclusive);
        public bool InUse => channel != null && channel.IsModified;
    }
}
