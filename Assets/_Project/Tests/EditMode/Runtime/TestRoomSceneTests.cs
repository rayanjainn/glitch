using System.Collections.Generic;
using System.IO;
using System.Linq;
using BugReport.Anomaly;
using BugReport.Core.Entities;
using BugReport.EditorTools.Setup;
using BugReport.Glitches;
using BugReport.Hero;
using BugReport.InputHandling;
using BugReport.Interaction;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BugReport.Tests.EditMode
{
    /// <summary>
    /// Static validation of the generated test room: required systems exist once,
    /// no missing scripts, no broken object references in project components.
    /// </summary>
    public class TestRoomSceneTests
    {
        private Scene scene;
        private bool openedHere;

        [OneTimeSetUp]
        public void Open()
        {
            if (!File.Exists(TestRoomBuilder.ScenePath))
                Assert.Ignore("TestRoom scene not built yet. Run 'BUG REPORT > Setup > 2. Build Test Room Scene'.");

            scene = SceneManager.GetSceneByPath(TestRoomBuilder.ScenePath);
            if (!scene.isLoaded)
            {
                scene = EditorSceneManager.OpenScene(TestRoomBuilder.ScenePath, OpenSceneMode.Additive);
                openedHere = true;
            }
        }

        [OneTimeTearDown]
        public void Close()
        {
            if (openedHere && scene.IsValid()) EditorSceneManager.CloseScene(scene, true);
        }

        private List<T> All<T>() where T : Component
            => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).ToList();

        [Test]
        public void HasExactlyOneOfEachSystem()
        {
            Assert.That(All<GameBootstrap>().Count, Is.EqualTo(1), "GameBootstrap");
            Assert.That(All<AnomalyPlayerController>().Count, Is.EqualTo(1), "AnomalyPlayerController");
            Assert.That(All<AnomalyCameraRig>().Count, Is.EqualTo(1), "AnomalyCameraRig");
            Assert.That(All<TargetScanner>().Count, Is.EqualTo(1), "TargetScanner");
            Assert.That(All<SelectionController>().Count, Is.EqualTo(1), "SelectionController");
            Assert.That(All<InputReader>().Count, Is.EqualTo(1), "InputReader");
            Assert.That(All<HeroController>().Count, Is.EqualTo(1), "HeroController");
            Assert.That(All<HeroNavGraph>().Count, Is.EqualTo(1), "HeroNavGraph");
            Assert.That(All<BugReport.Containment.ContainmentDirector>().Count, Is.EqualTo(1), "ContainmentDirector");
            Assert.That(All<GlitchController>().Count, Is.EqualTo(1), "GlitchController");
        }

        [Test]
        public void HasNoMissingScripts()
        {
            int missing = scene.GetRootGameObjects()
                .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                .Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
            Assert.That(missing, Is.EqualTo(0));
        }

        [Test]
        public void ProjectComponentsHaveNoNullOrBrokenReferences()
        {
            // Same rule the builder enforces; fields marked [OptionalReference] may be empty.
            var problems = TestRoomBuilder.FindUnassignedReferences(scene);
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [Test]
        public void MainCameraIsTagged()
        {
            var cameras = All<Camera>().Where(c => c.CompareTag("MainCamera")).ToList();
            Assert.That(cameras.Count, Is.EqualTo(1));
        }

        [Test]
        public void AnomalyIsUntargetableAndHeroHasGoal()
        {
            var anomaly = All<AnomalyPlayerController>().Single().GetComponent<GameEntity>();
            Assert.That(anomaly, Is.Not.Null);
            Assert.That(anomaly.Category, Is.EqualTo(EntityCategory.Anomaly));

            var hero = All<HeroController>().Single();
            Assert.That(hero.Goal, Is.Not.Null);
            Assert.That(All<HeroNavGraph>().Single().TryGetId(hero.Goal, out _), Is.True, "goal must be a node of the graph");
            Assert.That(hero.GetComponent<GameEntity>().Category, Is.EqualTo(EntityCategory.Hero));
        }

        [Test]
        public void ContainsValidAndInvalidSelectionTargets()
        {
            var entities = All<GameEntity>();
            Assert.That(entities.Any(e => e.Capabilities.HasAll(EntityCapabilities.Inspectable) && e.Category != EntityCategory.Anomaly), Is.True);
            Assert.That(entities.Any(e => !e.Capabilities.HasAll(EntityCapabilities.Inspectable)), Is.True, "need a non-inspectable entity to exercise invalid feedback");
        }

        [Test]
        public void CollisionTransferPuzzle_IsAuthored()
        {
            var byName = All<GameEntity>().ToDictionary(e => e.DisplayName);
            Assert.That(byName["FIREWALL_01"].GetComponent<CollisionCapability>().AuthoredMode, Is.EqualTo(CollisionMode.Solid));
            Assert.That(byName["SHADOW_DECAL"].GetComponent<CollisionCapability>().AuthoredMode, Is.EqualTo(CollisionMode.Intangible));
            Assert.That(byName["HOLO_PANEL"].GetComponent<CollisionCapability>().AuthoredMode, Is.EqualTo(CollisionMode.Intangible));

            // Every entity declaring the capability must actually implement it.
            foreach (var e in All<GameEntity>())
            {
                if (e.Capabilities.HasAll(EntityCapabilities.CollisionTransferable))
                    Assert.That(e.GetComponent<CollisionCapability>(), Is.Not.Null, e.DisplayName + " declares CollisionTransferable without a CollisionCapability");
            }
        }

        [Test]
        public void EveryDeclaredCapability_IsImplemented()
        {
            foreach (var e in All<GameEntity>())
            {
                if (e.Capabilities.HasAll(EntityCapabilities.Freezable))
                    Assert.That(e.GetComponent<FreezeCapability>(), Is.Not.Null, e.DisplayName + " declares Freezable without FreezeCapability");
                if (e.Capabilities.HasAll(EntityCapabilities.Duplicable))
                {
                    var dup = e.GetComponent<DuplicationCapability>();
                    Assert.That(dup, Is.Not.Null, e.DisplayName + " declares Duplicable without DuplicationCapability");
                    Assert.That(dup.Profile, Is.Not.Null, e.DisplayName + ": no profile");
                    Assert.That(dup.Profile.Prefab, Is.Not.Null, e.DisplayName + ": profile has no prefab");
                    Assert.That(EditorUtility.IsPersistent(dup.Profile.Prefab), Is.True, e.DisplayName + ": profile prefab must be an asset, never a scene object");
                }
            }
        }

        [Test]
        public void DuplicableCrates_ArePrefabInstances()
        {
            foreach (var e in All<GameEntity>().Where(x => x.Capabilities.HasAll(EntityCapabilities.Duplicable)))
                Assert.That(PrefabUtility.IsPartOfPrefabInstance(e.gameObject), Is.True, e.DisplayName);
        }

        [Test]
        public void HeroCanReachTheExit_OnceTheFirewallIsPassable()
        {
            // Static graph check: connected start → exit ignoring physics.
            var graph = All<HeroNavGraph>().Single();
            var hero = All<HeroController>().Single();
            graph.Build();
            graph.TryGetId(hero.Goal, out var goal);
            int start = graph.Graph.Nearest(hero.transform.position.x, 0f, hero.transform.position.z);
            var path = new List<int>();
            Assert.That(graph.Graph.TryFindPath(start, goal, (a, b) => true, path), Is.True);
        }

        [Test]
        public void SceneIsInBuildSettings()
        {
            Assert.That(EditorBuildSettings.scenes.Any(s => s.path == TestRoomBuilder.ScenePath && s.enabled), Is.True);
        }
    }
}
