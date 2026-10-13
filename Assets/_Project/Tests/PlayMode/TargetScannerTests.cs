using BugReport.Interaction;
using NUnit.Framework;
using UnityEngine;

namespace BugReport.Tests.PlayMode
{
    public class TargetScannerTests : PlayModeTestBase
    {
        private TargetScanner CreateScanner(Transform ignoreRoot = null)
        {
            var go = Track(new GameObject("Camera", typeof(Camera)));
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var scanner = go.AddComponent<TargetScanner>();
            scanner.AutoScan = false;
            scanner.Configure(go.GetComponent<Camera>(), ignoreRoot);
            return scanner;
        }

        private GameEntity Entity(string name, Vector3 position)
        {
            var go = Box(name, position, Vector3.one);
            return go.AddComponent<GameEntity>();
        }

        [Test]
        public void FindsEntityUnderReticle()
        {
            var scanner = CreateScanner();
            var target = Entity("Target", new Vector3(0f, 0f, 5f));
            Physics.SyncTransforms();

            Assert.That(scanner.Scan(), Is.SameAs(target));
            Assert.That(scanner.HasHit, Is.True);
        }

        [Test]
        public void FindsEntityFromChildCollider()
        {
            var scanner = CreateScanner();
            var parent = Track(new GameObject("Parent"));
            var entity = parent.AddComponent<GameEntity>();
            var child = Box("Child", new Vector3(0f, 0f, 5f), Vector3.one);
            child.transform.SetParent(parent.transform, true);
            Physics.SyncTransforms();

            Assert.That(scanner.Scan(), Is.SameAs(entity));
        }

        [Test]
        public void NonEntityOccluder_BlocksTarget()
        {
            var scanner = CreateScanner();
            Entity("Target", new Vector3(0f, 0f, 8f));
            Box("Wall", new Vector3(0f, 0f, 4f), new Vector3(4f, 4f, 0.2f));
            Physics.SyncTransforms();

            Assert.That(scanner.Scan(), Is.Null);
            Assert.That(scanner.HasHit, Is.True, "the wall itself was hit");
        }

        [Test]
        public void IgnoredHierarchy_IsSeenThrough()
        {
            var self = Box("Self", new Vector3(0f, 0f, 2f), Vector3.one * 0.6f);
            var scanner = CreateScanner(self.transform);
            var target = Entity("Target", new Vector3(0f, 0f, 6f));
            Physics.SyncTransforms();

            Assert.That(scanner.Scan(), Is.SameAs(target));
        }

        [Test]
        public void NothingInRange_ReturnsNull()
        {
            var scanner = CreateScanner();
            Entity("Far", new Vector3(0f, 0f, scanner.MaxDistance + 10f));
            Physics.SyncTransforms();

            Assert.That(scanner.Scan(), Is.Null);
            Assert.That(scanner.HasHit, Is.False);
        }
    }
}
