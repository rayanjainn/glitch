using System;
using UnityEngine;

namespace BugReport
{
    /// <summary>
    /// Marks a serialized object reference that may legitimately be empty (e.g. a HUD in a level
    /// without objectives). Scene validation (builder + EditMode tests) skips these; every other
    /// unassigned reference on a project component is treated as a broken scene.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class OptionalReferenceAttribute : PropertyAttribute
    {
    }
}
