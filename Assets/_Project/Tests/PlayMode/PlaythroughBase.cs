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
    /// <summary>Shared playthrough machinery: load a level, replay the player's workflow, wait on the hero and objectives.</summary>
    public abstract class PlaythroughBase
    {
        protected const string Folder = "Assets/_Project/Scenes/";
        protected float previousTimeScale = 1f;
        protected string loadedPath;

        protected GameServices services;
        protected GlitchController controller;
        protected HeroController hero;
        protected ObjectiveDirector objectives;

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

        protected IEnumerator Load(string sceneName)
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

        protected GameEntity Named(string displayName)
        {
            var all = new List<IEntityDescriptor>();
            services.Registry.GetAll(all);
            var match = all.OfType<GameEntity>().FirstOrDefault(e => e.DisplayName == displayName && e.GetComponent<DuplicateMarker>() == null);
            Assert.That(match, Is.Not.Null, "no entity named " + displayName);
            return match;
        }

        protected GameEntity CopyOf(GameEntity source)
        {
            var all = new List<IEntityDescriptor>();
            services.Registry.GetAll(all);
            var copy = all.OfType<GameEntity>().LastOrDefault(e => e.GetComponent<DuplicateMarker>() is DuplicateMarker m && m.Origin == source.Id);
            Assert.That(copy, Is.Not.Null, "no copy of " + source.DisplayName);
            return copy;
        }

        /// <summary>Player workflow: equip, select the source (two-slot glitches), aim at the target, apply.</summary>
        protected void Apply<T>(GameEntity source, GameEntity target) where T : GlitchDefinition
        {
            for (int i = 0; i < controller.LoadedCount && !(controller.CurrentDefinition is T); i++) controller.CycleGlitch();
            Assert.That(controller.CurrentDefinition, Is.InstanceOf<T>(), typeof(T).Name + " not available in this level");
            services.Selection.ClearSelection();
            if (source != null) Assert.That(services.Selection.TrySelect(source.Id).IsValid, Is.True, "cannot select " + source.DisplayName);
            services.Selection.SetHovered(target.Id);
            controller.ApplyCurrent();
            Assert.That(controller.LastFeedback.Kind, Is.EqualTo(GlitchFeedbackKind.Applied), controller.LastFeedback.Message);
        }

        protected static IEnumerator WaitFor(Func<bool> condition, float maxGameSeconds, string what)
        {
            float start = Time.time;
            while (!condition())
            {
                if (Time.time - start > maxGameSeconds) Assert.Fail("Timed out waiting for: " + what);
                yield return null;
            }
        }

        protected static IEnumerator GameSeconds(float seconds)
        {
            float end = Time.time + seconds;
            while (Time.time < end) yield return null;
        }

        protected IEnumerator HeroBlocked(string where) => WaitFor(() => hero.State == HeroState.Blocked, 40f, "hero waiting " + where);
        protected IEnumerator Objectives(int count, string what) => WaitFor(() => objectives.Tracker.CompletedCount >= count, 60f, what);

        protected IEnumerator Finished()
        {
            yield return WaitFor(() => objectives.Tracker.AllComplete, 60f, "all objectives");
            Assert.That(services.State.Current, Is.EqualTo(GameState.Ending));
            Assert.That(hero.RespawnCount, Is.EqualTo(0), "the hero should never fall on the intended solution");
        }

        protected static bool AtStart(PingPongMover mover, float tolerance = 0.05f)
            => Mathf.Abs(mover.Phase - Mathf.Round(mover.Phase)) < tolerance;

        protected static PingPongMover Mover(GameEntity e) => e.GetComponent<PingPongMover>();

    }
}
#endif
