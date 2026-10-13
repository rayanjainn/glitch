using System;
using System.Collections.Generic;

namespace BugReport.Core.Entities
{
    /// <summary>
    /// Tracks live gameplay entities by runtime id.
    ///
    /// Safety rules:
    /// - Ids are assigned by the registry, monotonically, and never reused within
    ///   one registry lifetime, so a stale id can never resolve to a new object.
    /// - Lookups prune entities whose <see cref="IEntityDescriptor.IsAlive"/> is
    ///   false (destroyed without unregistering) and raise <see cref="Unregistered"/>.
    /// - <see cref="Clear"/> is called on level teardown; it unregisters everything.
    /// </summary>
    public sealed class EntityRegistry
    {
        private readonly Dictionary<GameEntityId, IEntityDescriptor> entities = new Dictionary<GameEntityId, IEntityDescriptor>();
        private readonly Dictionary<IEntityDescriptor, GameEntityId> idsByEntity = new Dictionary<IEntityDescriptor, GameEntityId>(ReferenceEqualityComparer.Instance);
        private int nextId = 1;

        /// <summary>Raised with the new id (the entity may not have stored it yet).</summary>
        public event Action<GameEntityId, IEntityDescriptor> Registered;
        /// <summary>Raised with the id and the (possibly destroyed) entity.</summary>
        public event Action<GameEntityId, IEntityDescriptor> Unregistered;

        public int Count => entities.Count;

        /// <summary>
        /// Registers an entity and returns its new id. Registering an entity that
        /// is already registered returns its existing id.
        /// </summary>
        public GameEntityId Register(IEntityDescriptor entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (idsByEntity.TryGetValue(entity, out var existing)) return existing;
            if (nextId == int.MaxValue) throw new InvalidOperationException("Entity id space exhausted.");

            var id = new GameEntityId(nextId++);
            entities.Add(id, entity);
            idsByEntity.Add(entity, id);
            Registered?.Invoke(id, entity);
            return id;
        }

        /// <summary>Returns false if the id is unknown (already removed or never registered).</summary>
        public bool Unregister(GameEntityId id)
        {
            if (!entities.TryGetValue(id, out var entity)) return false;
            entities.Remove(id);
            idsByEntity.Remove(entity);
            Unregistered?.Invoke(id, entity);
            return true;
        }

        public bool Contains(GameEntityId id) => TryGet(id, out _);

        /// <summary>Resolves a live entity. Dead entities are pruned and reported as missing.</summary>
        public bool TryGet(GameEntityId id, out IEntityDescriptor entity)
        {
            if (entities.TryGetValue(id, out entity))
            {
                if (entity.IsAlive) return true;
                Unregister(id);
            }
            entity = null;
            return false;
        }

        public bool TryGetId(IEntityDescriptor entity, out GameEntityId id)
        {
            if (entity != null && idsByEntity.TryGetValue(entity, out id)) return true;
            id = GameEntityId.None;
            return false;
        }

        /// <summary>Copies live entities into <paramref name="results"/> (cleared first). Prunes dead ones.</summary>
        public void GetAll(List<IEntityDescriptor> results)
        {
            if (results == null) throw new ArgumentNullException(nameof(results));
            results.Clear();
            PruneDead();
            foreach (var pair in entities) results.Add(pair.Value);
        }

        /// <summary>Removes entities that were destroyed without unregistering. Returns how many were removed.</summary>
        public int PruneDead()
        {
            int deadCount = 0;
            foreach (var pair in entities)
            {
                if (!pair.Value.IsAlive) deadCount++;
            }
            if (deadCount == 0) return 0;

            // Snapshot first: Unregistered handlers may re-enter the registry.
            var dead = new GameEntityId[deadCount];
            int i = 0;
            foreach (var pair in entities)
            {
                if (!pair.Value.IsAlive) dead[i++] = pair.Key;
            }

            int removed = 0;
            for (i = 0; i < dead.Length; i++)
            {
                if (Unregister(dead[i])) removed++;
            }
            return removed;
        }

        /// <summary>Unregisters every entity (level teardown). Ids are not reused afterwards.</summary>
        public void Clear()
        {
            if (entities.Count == 0) return;
            // Snapshot first: Unregistered handlers may re-enter the registry.
            var all = new GameEntityId[entities.Count];
            entities.Keys.CopyTo(all, 0);
            for (int i = 0; i < all.Length; i++) Unregister(all[i]);
        }

        /// <summary>
        /// Reference equality so that a Unity object that overrides Equals (or is
        /// destroyed and compares equal to null) is still found by identity.
        /// System.Collections.Generic.ReferenceEqualityComparer is .NET 5+, so it
        /// is not available in Unity's .NET Standard 2.1 profile.
        /// </summary>
        private sealed class ReferenceEqualityComparer : IEqualityComparer<IEntityDescriptor>
        {
            public static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();
            public bool Equals(IEntityDescriptor x, IEntityDescriptor y) => ReferenceEquals(x, y);
            public int GetHashCode(IEntityDescriptor obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }
}
