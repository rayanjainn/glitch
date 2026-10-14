#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BugReport.Core.Entities;
using BugReport.Core.GameFlow;
using BugReport.Glitches;
using BugReport.Hero;
using BugReport.Level;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BugReport.Tests.PlayMode
{
    /// <summary>
    /// Every demo level is finished with its intended solution, through the player's workflow
    /// (equip, select source, aim, apply), from a fresh load, at 3× speed, with no errors logged.
    /// A failing test names the step that timed out. Levels that haven't been built are skipped.
    /// </summary>
    public class DemoLevelPlaythroughTests
    {
        private const string Folder = "Assets/_Project/Scenes/";
        private float previousTimeScale = 1f;
        private string loadedPath;

        private GameServices services;
        private GlitchController controller;
        private HeroController hero;
        private ObjectiveDirector objectives;

        [UnityTearDown]
        public IEnumerator Unload()
        {
            Time.timeScale = previousTimeScale;
            BugReport.Progress.SaveStore.RedirectForTests(null);
            if (loadedPath == null) yield break;
            var level = SceneManager.GetSceneByPath(loadedPath);
            loadedPath = null;
            if (!level.IsValid() || !level.isLoaded) yield break;
            SceneManager.SetActiveScene(SceneManager.CreateScene("PlaythroughCleanup"));
            var unload = SceneManager.UnloadSceneAsync(level);
            while (unload != null && !unload.isDone) yield return null;
        }

        private IEnumerator Load(string sceneName)
        {
            var path = Folder + sceneName + ".unity";
            if (!System.IO.File.Exists(path)) Assert.Ignore("Not built yet: BUG REPORT > Setup > 5. Build ALL.");
            PlayModeTestBase.RedirectSave();
            var load = UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));
            while (!load.isDone) yield return null;
            yield return null;
            loadedPath = path;
            previousTimeScale = Time.timeScale;
            Time.timeScale = 3f;
            services = GameServices.Current;
            Assert.That(services, Is.Not.Null, "bootstrap missing");
            controller = UnityEngine.Object.FindAnyObjectByType<GlitchController>();
            hero = UnityEngine.Object.FindAnyObjectByType<HeroController>();
            objectives = UnityEngine.Object.FindAnyObjectByType<ObjectiveDirector>();
        }

        private GameEntity Named(string displayName)
        {
            var all = new List<IEntityDescriptor>();
            services.Registry.GetAll(all);
            var match = all.OfType<GameEntity>().FirstOrDefault(e => e.DisplayName == displayName && e.GetComponent<DuplicateMarker>() == null);
            Assert.That(match, Is.Not.Null, "no entity named " + displayName);
            return match;
        }

        private GameEntity CopyOf(GameEntity source)
        {
            var all = new List<IEntityDescriptor>();
            services.Registry.GetAll(all);
            var copy = all.OfType<GameEntity>().LastOrDefault(e => e.GetComponent<DuplicateMarker>() is DuplicateMarker m && m.Origin == source.Id);
            Assert.That(copy, Is.Not.Null, "no copy of " + source.DisplayName);
            return copy;
        }

        /// <summary>Player workflow: equip, select the source (two-slot glitches), aim at the target, apply.</summary>
        private void Apply<T>(GameEntity source, GameEntity target) where T : GlitchDefinition
        {
            for (int i = 0; i < controller.LoadedCount && !(controller.CurrentDefinition is T); i++) controller.CycleGlitch();
            Assert.That(controller.CurrentDefinition, Is.InstanceOf<T>(), typeof(T).Name + " not available in this level");
            services.Selection.ClearSelection();
            if (source != null) Assert.That(services.Selection.TrySelect(source.Id).IsValid, Is.True, "cannot select " + source.DisplayName);
            services.Selection.SetHovered(target.Id);
            controller.ApplyCurrent();
            Assert.That(controller.LastFeedback.Kind, Is.EqualTo(GlitchFeedbackKind.Applied), controller.LastFeedback.Message);
        }

        private static IEnumerator WaitFor(Func<bool> condition, float maxGameSeconds, string what)
        {
            float start = Time.time;
            while (!condition())
            {
                if (Time.time - start > maxGameSeconds) Assert.Fail("Timed out waiting for: " + what);
                yield return null;
            }
        }

        private static IEnumerator GameSeconds(float seconds)
        {
            float end = Time.time + seconds;
            while (Time.time < end) yield return null;
        }

        private IEnumerator HeroBlocked(string where) => WaitFor(() => hero.State == HeroState.Blocked, 40f, "hero waiting " + where);
        private IEnumerator Objectives(int count, string what) => WaitFor(() => objectives.Tracker.CompletedCount >= count, 60f, what);

        private IEnumerator Finished()
        {
            yield return WaitFor(() => objectives.Tracker.AllComplete, 60f, "all objectives");
            Assert.That(services.State.Current, Is.EqualTo(GameState.Ending));
            Assert.That(hero.RespawnCount, Is.EqualTo(0), "the hero should never fall on the intended solution");
        }

        private static bool AtStart(PingPongMover mover, float tolerance = 0.05f)
            => Mathf.Abs(mover.Phase - Mathf.Round(mover.Phase)) < tolerance;

        private static PingPongMover Mover(GameEntity e) => e.GetComponent<PingPongMover>();

        // ---------------------------------------------------------------- chapter 1

        [UnityTest]
        public IEnumerator Level02_BridgeOut()
        {
            yield return Load("Level_02_BridgeOut");
            yield return HeroBlocked("at the gap");
            Apply<CollisionTransferDefinition>(Named("OLD_BARRIER"), Named("HOLO_BRIDGE"));
            yield return Finished();
        }

        [UnityTest]
        public IEnumerator Level03_TwoDoors()
        {
            yield return Load("Level_03_TwoDoors");
            yield return HeroBlocked("at the first firewall");
            Apply<CollisionTransferDefinition>(Named("FIREWALL_A"), Named("SHADOW_DECAL"));
            yield return Objectives(1, "past the first firewall");
            yield return HeroBlocked("at the second firewall");
            // The shadow is still leased by the first transfer: undo it (the hero is through), then reuse it.
            controller.RevertLatest();
            Assert.That(controller.LastFeedback.Kind, Is.EqualTo(GlitchFeedbackKind.Reverted), controller.LastFeedback.Message);
            yield return GameSeconds(0.7f);
            Apply<CollisionTransferDefinition>(Named("FIREWALL_B"), Named("SHADOW_DECAL"));
            yield return Finished();
        }

        [UnityTest]
        public IEnumerator Level04_TwoBirds()
        {
            yield return Load("Level_04_TwoBirds");
            yield return HeroBlocked("at the firewall");
            Apply<CollisionTransferDefinition>(Named("FIREWALL"), Named("HOLO_BRIDGE"));
            yield return Finished();
        }

        [UnityTest]
        public IEnumerator Level04_TwoBirds_TheTrapBlocks_AndUndoRecovers()
        {
            yield return Load("Level_04_TwoBirds");
            yield return HeroBlocked("at the firewall");
            Apply<CollisionTransferDefinition>(Named("FIREWALL"), Named("HOLO_DOOR"));
            yield return Objectives(1, "past the firewall (wrong solution still opens it)");
            yield return HeroBlocked("at the gap");
            controller.RevertLatest();
            Assert.That(controller.LastFeedback.Kind, Is.EqualTo(GlitchFeedbackKind.Reverted), controller.LastFeedback.Message);
            yield return GameSeconds(0.7f);
            // The firewall is solid again, but the hero is already past it: give its solidity to the bridge.
            Apply<CollisionTransferDefinition>(Named("FIREWALL"), Named("HOLO_BRIDGE"));
            yield return Finished();
        }

        // ---------------------------------------------------------------- chapter 2

        [UnityTest]
        public IEnumerator Level05_ThreePlates()
        {
            yield return Load("Level_05_ThreePlates");
            yield return HeroBlocked("at the gate");
            var crate = Named("CRATE");
            Apply<DuplicationDefinition>(null, crate);
            yield return GameSeconds(0.7f);
            Apply<DuplicationDefinition>(null, crate);
            yield return Finished();
        }

        [UnityTest]
        public IEnumerator Level06_CopyTheCopy()
        {
            yield return Load("Level_06_CopyTheCopy");
            yield return HeroBlocked("at the gate");
            var crate = Named("CRATE");
            Apply<DuplicationDefinition>(null, crate);
            yield return GameSeconds(0.7f);
            Apply<DuplicationDefinition>(null, CopyOf(crate));
            yield return Finished();
        }

        // ---------------------------------------------------------------- chapter 3

        [UnityTest]
        public IEnumerator Level07_StopThePlatform()
        {
            yield return Load("Level_07_StopThePlatform");
            var platform = Named("SHUTTLE_PLATFORM");
            yield return WaitFor(() => hero.State == HeroState.Blocked && AtStart(Mover(platform)), 40f, "hero at the pit, platform lined up");
            Apply<FreezeDefinition>(null, platform);
            yield return Finished();
        }

        [UnityTest]
        public IEnumerator Level08_TwoPistons()
        {
            yield return Load("Level_08_TwoPistons");
            var a = Named("PISTON_A");
            var b = Named("PISTON_B");
            yield return WaitFor(() => hero.State == HeroState.Blocked && AtStart(Mover(a), 0.1f), 40f, "hero at piston A, A pulled back");
            Apply<FreezeDefinition>(null, a);
            yield return Objectives(1, "past piston A");
            yield return WaitFor(() => hero.State == HeroState.Blocked && AtStart(Mover(b), 0.1f), 40f, "hero at piston B, B pulled back");
            Apply<FreezeDefinition>(null, b);
            yield return Finished();
        }

        // ---------------------------------------------------------------- finale

        [UnityTest]
        public IEnumerator Level09_TheBuild()
        {
            yield return Load("Level_09_TheBuild");
            yield return HeroBlocked("at the firewall");
            Apply<CollisionTransferDefinition>(Named("FIREWALL"), Named("HOLO_BRIDGE"));
            yield return Objectives(1, "across the gap");
            Apply<DuplicationDefinition>(null, Named("CRATE"));
            yield return Objectives(2, "through the gate");
            var piston = Named("CRUSHER_PISTON");
            yield return WaitFor(() => hero.State == HeroState.Blocked && AtStart(Mover(piston), 0.1f), 40f, "hero at the piston, pulled back");
            Apply<FreezeDefinition>(null, piston);
            yield return Finished();
        }
    }
}
#endif
