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
            Assert.That(All<PlaceholderHero>().Count, Is.EqualTo(1), "PlaceholderHero");
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
            var problems = new List<string>();
            foreach (var behaviour in All<MonoBehaviour>())
            {
                if (behaviour == null) continue;
                var ns = behaviour.GetType().Namespace ?? string.Empty;
                if (!ns.StartsWith("BugReport")) continue;

                var so = new SerializedObject(behaviour);
                var prop = so.GetIterator();
                while (prop.NextVisible(true))
                {
                    if (prop.propertyType != SerializedPropertyType.ObjectReference) continue;
                    if (prop.name == "m_Script") continue;
                    if (prop.objectReferenceValue == null)
                        problems.Add(behaviour.GetType().Name + " on '" + behaviour.name + "': " + prop.propertyPath);
                }
            }
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [Test]
        public void MainCameraIsTagged()
        {
            var cameras = All<Camera>().Where(c => c.CompareTag("MainCamera")).ToList();
            Assert.That(cameras.Count, Is.EqualTo(1));
        }

        [Test]
        public void AnomalyIsUntargetableAndHeroHasRoute()
        {
            var anomaly = All<AnomalyPlayerController>().Single().GetComponent<GameEntity>();
            Assert.That(anomaly, Is.Not.Null);
            Assert.That(anomaly.Category, Is.EqualTo(EntityCategory.Anomaly));

            var hero = All<PlaceholderHero>().Single();
            Assert.That(hero.RouteLength, Is.GreaterThan(0));
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
        public void SceneIsInBuildSettings()
        {
            Assert.That(EditorBuildSettings.scenes.Any(s => s.path == TestRoomBuilder.ScenePath && s.enabled), Is.True);
        }
    }
}
