namespace BugReport.Core.Entities
{
    /// <summary>
    /// Engine-independent view of a gameplay entity. Implemented by the Unity
    /// <c>GameEntity</c> component and by fakes in unit tests.
    /// </summary>
    public interface IEntityDescriptor
    {
        EntityId Id { get; }
        string DisplayName { get; }
        EntityCategory Category { get; }
        EntityCapabilities Capabilities { get; }

        /// <summary>
        /// False once the underlying object has been destroyed. Unity objects
        /// can be destroyed without notifying us, so consumers must check this
        /// instead of trusting a cached reference.
        /// </summary>
        bool IsAlive { get; }
    }
}
