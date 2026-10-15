using System.Collections;
using BugReport.Anomaly;
using BugReport.Core.GameFlow;
using BugReport.Level;
using BugReport.Progress;
using BugReport.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace BugReport.Tests.PlayMode
{
    /// <summary>Main menu, level unlocking, completion recording and settings application.</summary>
    public class FrontEndTests : PlayModeTestBase
    {
        private LevelCatalog Catalog(string firstScene = "Level_A")
        {
            var catalog = Track(ScriptableObject.CreateInstance<LevelCatalog>());
            catalog.Configure(new[]
            {
                new LevelCatalog.Entry("a", firstScene, "LEVEL A", "1"),
                new LevelCatalog.Entry("b", "Level_B", "LEVEL B", "1"),
            });
            return catalog;
        }

        private IEnumerator Menu(LevelCatalog catalog, System.Action<MainMenu> use)
        {
            var panel = Track(ScriptableObject.CreateInstance<PanelSettings>());
            panel.themeStyleSheet = Track(ScriptableObject.CreateInstance<ThemeStyleSheet>());
            var go = Track(new GameObject("Menu"));
            go.SetActive(false);
            go.AddComponent<UIDocument>().panelSettings = panel;
            var menu = go.AddComponent<MainMenu>();
            menu.Configure(catalog);
            go.SetActive(true);
            yield return null;
            menu.RefreshNow();
            use(menu);
        }

        [UnityTest]
        public IEnumerator FreshSave_NoContinue_OnlyTheFirstLevelOpen()
        {
            yield return Menu(Catalog(), menu =>
            {
                Assert.That(menu.Showing, Is.EqualTo("home"));
                Assert.That(menu.ContinueVisible, Is.False);
                menu.ShowLevels();
                Assert.That(menu.LevelList[0].enabledSelf, Is.True);
                Assert.That(menu.LevelList[1].enabledSelf, Is.False, "locked until LEVEL A is done");
            });
        }

        [UnityTest]
        public IEnumerator CompletingALevel_UnlocksTheNext_AndOffersContinue()
        {
            SaveStore.MarkLevelCompleted("a");
            yield return Menu(Catalog(), menu =>
            {
                Assert.That(menu.ContinueVisible, Is.True);
                menu.ShowLevels();
                Assert.That(menu.LevelList[1].enabledSelf, Is.True);
                StringAssert.Contains("\u2605", ((Button)menu.LevelList[0]).text, "a finished level shows its stars");
                menu.ShowSettings();
                Assert.That(menu.Showing, Is.EqualTo("settings"));
                menu.ShowHome();
                Assert.That(menu.Showing, Is.EqualTo("home"));
            });
        }

        [Test]
        public void FinishingTheLevel_RecordsItInTheSave()
        {
            var services = new GameServices();
            GameServices.Install(services);
            services.State.TryTransition(GameState.Playing);

            var go = Track(new GameObject("Systems"));
            go.SetActive(false);
            var condition = go.AddComponent<ManualCondition>();
            var objectives = go.AddComponent<ObjectiveDirector>();
            objectives.Configure(new[] { new ObjectiveDirector.Entry("only", "ONLY", "", condition) });
            var recorder = go.AddComponent<LevelCompletionRecorder>();
            recorder.Configure(objectives, Catalog(go.scene.name));
            go.SetActive(true);
            Assert.That(recorder.CurrentLevelId(), Is.EqualTo("a"));

            condition.met = true;
            objectives.Evaluate();
            Assert.That(SaveStore.Current.IsCompleted("a"), Is.True);
            Assert.That(SaveStore.Load().IsCompleted("a"), Is.True, "written to disk immediately");
        }

        [Test]
        public void Settings_ApplyToTheCameraAndVolume()
        {
            float previousVolume = AudioListener.volume;
            try
            {
                var camGo = Track(new GameObject("Cam", typeof(Camera)));
                var rig = camGo.AddComponent<AnomalyCameraRig>();
                var applierGo = Track(new GameObject("Settings"));
                applierGo.SetActive(false);
                applierGo.AddComponent<SettingsApplier>().Configure(rig);
                applierGo.SetActive(true);

                SaveStore.Settings.mouseSensitivity = 2f;
                SaveStore.Settings.invertY = true;
                SaveStore.Settings.masterVolume = 0.3f;
                SaveStore.ApplySettings();

                Assert.That(rig.SensitivityScale, Is.EqualTo(2f));
                Assert.That(rig.InvertY, Is.True);
                Assert.That(AudioListener.volume, Is.EqualTo(0.3f).Within(1e-4f));
            }
            finally
            {
                AudioListener.volume = previousVolume;
            }
        }

        [Test]
        public void Anomaly_ShrinksWhenTheCameraIsSqueezedClose()
        {
            var go = Track(new GameObject("Visual"));
            var visual = go.AddComponent<AnomalyVisual>();
            Assert.That(visual.ScaleForDistance(5f), Is.EqualTo(1f));
            Assert.That(visual.ScaleForDistance(0.5f), Is.EqualTo(0.4f).Within(1e-4f));
            Assert.That(visual.ScaleForDistance(1.8f), Is.InRange(0.4f, 1f));
        }
    }
}
