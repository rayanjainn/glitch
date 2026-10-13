using System.Collections.Generic;
using System.IO;
using System.Linq;
using BugReport.Containment;
using BugReport.Core.Entities;
using BugReport.EditorTools.Setup;
using BugReport.Glitches;
using BugReport.Hero;
using BugReport.Level;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BugReport.Tests.EditMode
{
    /// <summary>Static validation of the generated vertical-slice level.</summary>
    public class SliceLevelSceneTests
    {
        private Scene scene;
        private bool openedHere;

        [OneTimeSetUp]
        public void Open()
        {
            if (!File.Exists(SliceLevelBuilder.ScenePath))
                Assert.Ignore("Level not built yet. Run 'BUG REPORT > Setup > 3. Build Vertical Slice Level'.");
            scene = SceneManager.GetSceneByPath(SliceLevelBuilder.ScenePath);
            if (!scene.isLoaded)
            {
                scene = EditorSceneManager.OpenScene(SliceLevelBuilder.ScenePath, OpenSceneMode.Additive);
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
        public void HasCoreSystemsOnce()
        {
            foreach (var count in new[]
            {
                (All<GameBootstrap>().Count, "GameBootstrap"), (All<GlitchController>().Count, "GlitchController"),
                (All<ContainmentDirector>().Count, "ContainmentDirector"), (All<ObjectiveDirector>().Count, "ObjectiveDirector"),
                (All<HeroController>().Count, "HeroController"), (All<LevelMenus>().Count, "LevelMenus"), (All<LevelHud>().Count, "LevelHud"),
            })
                Assert.That(count.Item1, Is.EqualTo(1), count.Item2);
        }

        [Test]
        public void NoUnassignedReferences_OrMissingScripts()
        {
            Assert.That(TestRoomBuilder.FindUnassignedReferences(scene), Is.Empty);
            int missing = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                .Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
            Assert.That(missing, Is.EqualTo(0));
        }

        [Test]
        public void HasThreeObjectives_EachWithACondition()
        {
            var director = All<ObjectiveDirector>().Single();
            Assert.That(director.Count, Is.EqualTo(3));
            for (int i = 0; i < director.Count; i++)
            {
                Assert.That(director.Get(i).condition, Is.Not.Null, director.Get(i).id);
                Assert.That(director.Get(i).title, Is.Not.Empty);
                Assert.That(director.Get(i).hint, Is.Not.Empty, "every objective needs an in-game hint");
            }
        }

        [Test]
        public void EveryDeclaredCapability_IsImplemented()
        {
            foreach (var e in All<GameEntity>())
            {
                if (e.Capabilities.HasAll(EntityCapabilities.CollisionTransferable)) Assert.That(e.GetComponent<CollisionCapability>(), Is.Not.Null, e.DisplayName);
                if (e.Capabilities.HasAll(EntityCapabilities.Freezable)) Assert.That(e.GetComponent<FreezeCapability>(), Is.Not.Null, e.DisplayName);
                if (e.Capabilities.HasAll(EntityCapabilities.Duplicable))
                {
                    var d = e.GetComponent<DuplicationCapability>();
                    Assert.That(d != null && d.Profile != null && d.Profile.Prefab != null && EditorUtility.IsPersistent(d.Profile.Prefab), Is.True, e.DisplayName);
                }
            }
        }

        [Test]
        public void HeroGraph_ConnectsStartToExit()
        {
            var graph = All<HeroNavGraph>().Single();
            var hero = All<HeroController>().Single();
            graph.Build();
            Assert.That(graph.TryGetId(hero.Goal, out var goal), Is.True);
            int start = graph.Graph.Nearest(hero.transform.position.x, 0f, hero.transform.position.z);
            Assert.That(graph.Graph.TryFindPath(start, goal, (a, b) => true, new List<int>()), Is.True);
        }

        [Test]
        public void PuzzlePieces_AreWired()
        {
            Assert.That(All<PressurePlate>().Count, Is.EqualTo(2));
            Assert.That(All<GateController>().Count, Is.EqualTo(1));
            var hazard = All<HeroHazard>().Single();
            Assert.That(hazard.Volume, Is.Not.Null);
            Assert.That(hazard.Volume.isTrigger, Is.True);
            Assert.That(hazard.GetComponent<FreezeCapability>(), Is.Not.Null, "the intended solution freezes the hazard");
            Assert.That(hazard.Volume.transform.IsChildOf(hazard.transform), Is.False, "the danger zone must not move with the machine");
        }

        [Test]
        public void IsTheFirstSceneInBuildSettings()
        {
            Assert.That(EditorBuildSettings.scenes.Length, Is.GreaterThan(0));
            Assert.That(EditorBuildSettings.scenes[0].path, Is.EqualTo(SliceLevelBuilder.ScenePath));
        }
    }
}
