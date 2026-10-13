#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
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
    /// Acceptance check: the vertical slice can be completed from a fresh load using the same
    /// player workflow (select → aim → apply), with no errors logged. Runs at 3× speed.
    /// </summary>
    public class SliceLevelPlaythroughTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Level_01_TrainingLab.unity";
        private float previousTimeScale = 1f;

        /// <summary>
        /// Unload the level so later PlayMode tests start clean (no level bootstrap/services,
        /// no level geometry under their test objects).
        /// </summary>
        [UnityTearDown]
        public IEnumerator UnloadLevel()
        {
            Time.timeScale = previousTimeScale;
            var level = SceneManager.GetSceneByPath(ScenePath);
            if (!level.IsValid() || !level.isLoaded) yield break;
            var empty = SceneManager.CreateScene("PlaythroughCleanup");
            SceneManager.SetActiveScene(empty);
            var unload = SceneManager.UnloadSceneAsync(level);
            while (unload != null && !unload.isDone) yield return null;
            Assert.That(GameServices.IsReady, Is.False, "level services should be disposed with the level");
        }

        [UnityTest]
        public IEnumerator Level01_IsCompletableWithTheIntendedSolution()
        {
            if (!System.IO.File.Exists(ScenePath)) Assert.Ignore("Level not built yet: BUG REPORT > Setup > 3. Build Vertical Slice Level.");

            var load = UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            while (!load.isDone) yield return null;
            yield return null;

            previousTimeScale = Time.timeScale;
            Time.timeScale = 3f;
            {
                var services = GameServices.Current;
                Assert.That(services, Is.Not.Null, "bootstrap missing");
                var controller = UnityEngine.Object.FindAnyObjectByType<GlitchController>();
                var hero = UnityEngine.Object.FindAnyObjectByType<HeroController>();
                var objectives = UnityEngine.Object.FindAnyObjectByType<ObjectiveDirector>();
                var piston = UnityEngine.Object.FindAnyObjectByType<PingPongMover>();
                var all = new System.Collections.Generic.List<Core.Entities.IEntityDescriptor>();
                services.Registry.GetAll(all);
                GameEntity Named(string n) => (GameEntity)all.Single(e => e.DisplayName == n);

                // 1. Firewall → shadow (Collision Transfer).
                yield return WaitFor(() => hero.State == HeroState.Blocked, 30f, "hero should wait at the firewall");
                Apply<CollisionTransferDefinition>(controller, services, Named("FIREWALL_01"), Named("SHADOW_DECAL_A"));
                yield return WaitFor(() => objectives.Tracker.CompletedCount >= 1, 40f, "objective 1 (firewall)");

                // 2. Duplicate the crate: the copy lands on plate 2, the gate opens.
                Apply<DuplicationDefinition>(controller, services, null, Named("CRATE_LAB"));
                yield return WaitFor(() => objectives.Tracker.CompletedCount >= 2, 60f, "objective 2 (gate)");

                // 3. Freeze the piston while it is pulled back.
                yield return WaitFor(() => hero.State == HeroState.Blocked && Retracted(piston), 30f, "hero waiting at the piston, piston retracted");
                Apply<FreezeDefinition>(controller, services, null, Named("CRUSHER_PISTON"));
                yield return WaitFor(() => objectives.Tracker.AllComplete, 30f, "objective 3 (piston)");

                Assert.That(services.State.Current, Is.EqualTo(GameState.Ending));
            }
        }

        private static bool Retracted(PingPongMover piston)
        {
            float distanceToCycleStart = Mathf.Abs(piston.Phase - Mathf.Round(piston.Phase));
            return distanceToCycleStart < 0.12f;
        }

        /// <summary>Uses the real player workflow: equip the glitch, select SOURCE (2-slot glitches), aim, apply.</summary>
        private static void Apply<TDefinition>(GlitchController controller, GameServices services, GameEntity source, GameEntity target)
            where TDefinition : GlitchDefinition
        {
            for (int i = 0; i < controller.LoadedCount && !(controller.CurrentDefinition is TDefinition); i++) controller.CycleGlitch();
            Assert.That(controller.CurrentDefinition, Is.InstanceOf<TDefinition>());
            services.Selection.ClearSelection();
            if (source != null) Assert.That(services.Selection.TrySelect(source.Id).IsValid, Is.True);
            services.Selection.SetHovered(target.Id); // same frame as the apply: the scanner cannot overwrite it
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
    }
}
#endif
