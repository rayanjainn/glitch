using System.Collections.Generic;
using BugReport.Core.Entities;
using BugReport.Glitches;
using UnityEngine;

namespace BugReport.Level
{
    /// <summary>
    /// Kill plane for physics props. A duplicate that falls out of the world is removed (its
    /// glitch ends and the copy budget is freed); an original puzzle object is put back where
    /// it started, at rest. Prevents softlocks from lost puzzle items.
    /// </summary>
    public sealed class WorldBoundsGuard : MonoBehaviour
    {
        [SerializeField] private float minY = -10f;
        [SerializeField, Min(0.05f)] private float checkInterval = 0.25f;

        private readonly List<IEntityDescriptor> buffer = new List<IEntityDescriptor>();
        private readonly Dictionary<GameEntity, Pose> spawnPoses = new Dictionary<GameEntity, Pose>();
        private float timer;

        public float MinY => minY;

        private void Update()
        {
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = checkInterval;
            Check();
        }

        /// <summary>Checks all entities now. Public for tests. Returns how many were handled.</summary>
        public int Check()
        {
            if (!GameServices.IsReady) return 0;
            GameServices.Current.Registry.GetAll(buffer);
            int handled = 0;
            foreach (var descriptor in buffer)
            {
                if (!(descriptor is GameEntity entity)) continue;
                var body = entity.GetComponent<Rigidbody>();
                if (body == null) continue;

                if (!spawnPoses.ContainsKey(entity) && entity.transform.position.y >= minY)
                    spawnPoses[entity] = new Pose(entity.transform.position, entity.transform.rotation);
                if (entity.transform.position.y >= minY) continue;

                handled++;
                if (entity.GetComponent<DuplicateMarker>() != null)
                {
                    entity.gameObject.SetActive(false); // unregisters → its duplication operation ends
                    Destroy(entity.gameObject);
                }
                else if (spawnPoses.TryGetValue(entity, out var pose))
                {
                    bool wasKinematic = body.isKinematic;
                    body.isKinematic = false;
                    body.linearVelocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                    body.isKinematic = wasKinematic;
                    body.position = pose.position;
                    entity.transform.SetPositionAndRotation(pose.position, pose.rotation);
                }
            }
            buffer.Clear();
            return handled;
        }
    }
}
