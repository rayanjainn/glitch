using System;

namespace BugReport.Core.Entities
{
    /// <summary>
    /// Explicit list of what an entity allows the anomaly to do to it.
    /// Glitches never manipulate arbitrary components: a target must declare the
    /// matching capability (and, from Milestone 2 on, carry the capability
    /// component that actually implements it).
    /// </summary>
    [Flags]
    public enum EntityCapabilities
    {
        None = 0,
        Inspectable = 1 << 0,
        CollisionTransferable = 1 << 1,
        Duplicable = 1 << 2,
        Freezable = 1 << 3,
        PropertyMutable = 1 << 4,
        AIControllable = 1 << 5,
        ObjectiveRelevant = 1 << 6,
        Desyncable = 1 << 7,
    }

    public static class EntityCapabilitiesExtensions
    {
        public static bool HasAll(this EntityCapabilities self, EntityCapabilities required)
            => (self & required) == required;
    }
}
