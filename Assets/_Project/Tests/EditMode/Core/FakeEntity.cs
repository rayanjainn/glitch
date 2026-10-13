using BugReport.Core.Entities;

namespace BugReport.Core.Tests
{
    /// <summary>Plain C# stand-in for a Unity GameEntity.</summary>
    internal sealed class FakeEntity : IEntityDescriptor
    {
        public FakeEntity(string name, EntityCategory category = EntityCategory.Prop,
            EntityCapabilities capabilities = EntityCapabilities.Inspectable)
        {
            DisplayName = name;
            Category = category;
            Capabilities = capabilities;
            IsAlive = true;
        }

        public EntityId Id { get; set; }
        public string DisplayName { get; }
        public EntityCategory Category { get; set; }
        public EntityCapabilities Capabilities { get; set; }
        public bool IsAlive { get; set; }

        /// <summary>Registers and stores the id, like GameEntity does.</summary>
        public FakeEntity RegisterIn(EntityRegistry registry)
        {
            Id = registry.Register(this);
            return this;
        }
    }
}
