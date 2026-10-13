using System.Collections;
using BugReport.Core.Entities;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BugReport.Tests.PlayMode
{
    public class GameEntityLifecycleTests : PlayModeTestBase
    {
        [Test]
        public void RegistersOnEnable_WhenServicesAlreadyInstalled()
        {
            var services = new GameServices();
            GameServices.Install(services);

            var entity = Track(new GameObject("E")).AddComponent<GameEntity>();

            Assert.That(entity.IsRegistered, Is.True);
            Assert.That(services.Registry.TryGet(entity.Id, out var found), Is.True);
            Assert.That(found, Is.SameAs(entity));
        }

        [Test]
        public void RegistersLater_WhenServicesInstalledAfterEnable()
        {
            var entity = Track(new GameObject("E")).AddComponent<GameEntity>();
            Assert.That(entity.IsRegistered, Is.False);

            var services = new GameServices();
            GameServices.Install(services);

            Assert.That(entity.IsRegistered, Is.True);
            Assert.That(services.Registry.Count, Is.EqualTo(1));
        }

        [Test]
        public void DisabledBeforeServices_DoesNotRegisterLater()
        {
            var go = Track(new GameObject("E"));
            var entity = go.AddComponent<GameEntity>();
            go.SetActive(false);

            var services = new GameServices();
            GameServices.Install(services);

            Assert.That(entity.IsRegistered, Is.False);
            Assert.That(services.Registry.Count, Is.EqualTo(0));
        }

        [Test]
        public void Disable_UnregistersAndClearsSelection()
        {
            var services = new GameServices();
            GameServices.Install(services);
            var go = Track(new GameObject("E"));
            var entity = go.AddComponent<GameEntity>();
            var id = entity.Id;
            services.Selection.TrySelect(id);

            go.SetActive(false);

            Assert.That(services.Registry.Contains(id), Is.False);
            Assert.That(services.Selection.HasSelection, Is.False);
            Assert.That(entity.Id, Is.EqualTo(GameEntityId.None));
        }

        [UnityTest]
        public IEnumerator Destroy_UnregistersAndStaleIdNeverResolves()
        {
            var services = new GameServices();
            GameServices.Install(services);
            var go = new GameObject("E");
            var id = go.AddComponent<GameEntity>().Id;

            Object.Destroy(go);
            yield return null;

            Assert.That(services.Registry.TryGet(id, out _), Is.False);
            var replacement = Track(new GameObject("E2")).AddComponent<GameEntity>();
            Assert.That(replacement.Id, Is.Not.EqualTo(id));
        }

        [Test]
        public void ServicesDispose_ClearsRegistry()
        {
            var services = new GameServices();
            GameServices.Install(services);
            Track(new GameObject("A")).AddComponent<GameEntity>();
            Track(new GameObject("B")).AddComponent<GameEntity>();

            services.Dispose();

            Assert.That(services.Registry.Count, Is.EqualTo(0));
            Assert.That(GameServices.IsReady, Is.False);
        }

        [Test]
        public void WorldBounds_ComeFromRenderers()
        {
            var go = Box("Crate", new Vector3(1f, 2f, 3f), new Vector3(2f, 2f, 2f));
            var entity = go.AddComponent<GameEntity>();
            var bounds = entity.GetWorldBounds();
            Assert.That(Vector3.Distance(bounds.center, new Vector3(1f, 2f, 3f)), Is.LessThan(1e-3f));
            Assert.That(Vector3.Distance(bounds.size, new Vector3(2f, 2f, 2f)), Is.LessThan(1e-3f));
        }

        [Test]
        public void InstallingSecondServices_Throws()
        {
            GameServices.Install(new GameServices());
            Assert.Throws<System.InvalidOperationException>(() => GameServices.Install(new GameServices()));
        }
    }
}
