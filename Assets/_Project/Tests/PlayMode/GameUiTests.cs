using System.Collections;
using BugReport.Containment;
using BugReport.Core.Entities;
using BugReport.Core.GameFlow;
using BugReport.Glitches;
using BugReport.Interaction;
using BugReport.Presentation;
using BugReport.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace BugReport.Tests.PlayMode
{
    /// <summary>
    /// The UI Toolkit layers against real game services: visibility per game state, feedback
    /// messages, glitch dock, containment alert, menus, inspection tags. Reads element state,
    /// never pixels.
    /// </summary>
    public class GameUiTests : PlayModeTestBase
    {
        private GameServices services;
        private UIDocument document;
        private GameHud hud;
        private GameMenus menus;
        private InspectionOverlay inspection;
        private GlitchController glitches;
        private Camera cam;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            services = new GameServices();
            GameServices.Install(services);
            services.State.TryTransition(GameState.Playing);

            var systems = Track(new GameObject("Systems"));
            systems.SetActive(false);
            var selection = systems.AddComponent<SelectionController>();
            glitches = systems.AddComponent<GlitchController>();
            var transfer = Track(ScriptableObject.CreateInstance<CollisionTransferDefinition>());
            transfer.ApplyDefaults();
            var freeze = Track(ScriptableObject.CreateInstance<FreezeDefinition>());
            freeze.ApplyDefaults();
            glitches.Configure(new GlitchDefinition[] { transfer, freeze }, selection);
            var containment = systems.AddComponent<ContainmentDirector>();
            containment.Configure(Track(ScriptableObject.CreateInstance<ContainmentSettingsAsset>()));
            systems.SetActive(true);

            cam = Track(new GameObject("Camera")).AddComponent<Camera>();
            cam.transform.position = Vector3.zero;
            cam.transform.rotation = Quaternion.identity;

            var panel = Track(ScriptableObject.CreateInstance<PanelSettings>());
            panel.themeStyleSheet = Track(ScriptableObject.CreateInstance<ThemeStyleSheet>());
            var ui = Track(new GameObject("UI"));
            ui.SetActive(false);
            document = ui.AddComponent<UIDocument>();
            document.panelSettings = panel;
            // Added in reverse draw order on purpose: layers must sort themselves.
            menus = ui.AddComponent<GameMenus>();
            menus.Configure(null, "TEST LAB");
            hud = ui.AddComponent<GameHud>();
            hud.Configure(glitches, selection, containment, null, null);
            inspection = ui.AddComponent<InspectionOverlay>();
            inspection.Configure(cam, null, null);
            ui.SetActive(true);
            yield return null;
            RefreshAll();
        }

        private GameEntity Entity(string name, Vector3 position, EntityCapabilities capabilities)
        {
            var go = Box(name, position, Vector3.one);
            go.SetActive(false);
            var entity = go.AddComponent<GameEntity>();
            entity.Configure(name, EntityCategory.Prop, capabilities);
            go.SetActive(true);
            return entity;
        }

        private void RefreshAll()
        {
            hud.RefreshNow();
            menus.RefreshNow();
            inspection.RefreshNow();
        }

        [Test]
        public void Layers_AreOrderedByDrawOrder_NotComponentOrder()
        {
            var root = document.rootVisualElement;
            Assert.That(root.childCount, Is.EqualTo(3));
            Assert.That(root[0].name, Is.EqualTo("inspection"));
            Assert.That(root[1].name, Is.EqualTo("hud"));
            Assert.That(root[2].name, Is.EqualTo("menus"));
        }

        [Test]
        public void Hud_ShownDuringGameplay_HiddenWhenPaused()
        {
            Assert.That(hud.Visible, Is.True);
            services.State.TogglePause();
            RefreshAll();
            Assert.That(hud.Visible, Is.False);
            Assert.That(inspection.Visible, Is.False);
        }

        [Test]
        public void Dock_HasOneSlotPerGlitch_AndMarksTheEquippedOne()
        {
            var first = hud.Find<VisualElement>("slot-0");
            var second = hud.Find<VisualElement>("slot-1");
            Assert.That(first, Is.Not.Null);
            Assert.That(second, Is.Not.Null);
            Assert.That(first.ClassListContains("br-slot--active"), Is.True);
            Assert.That(first.Q<Label>("slot-name").text, Is.EqualTo("1 " + glitches.DefinitionAt(0).DisplayName));

            glitches.CycleGlitch();
            hud.RefreshNow();
            Assert.That(first.ClassListContains("br-slot--active"), Is.False);
            Assert.That(second.ClassListContains("br-slot--active"), Is.True);
        }

        [Test]
        public void RejectedGlitch_ShowsAMessage_AndRepeatsCollapse()
        {
            glitches.ApplyCurrent(); // Collision Transfer with nothing selected
            glitches.ApplyCurrent();
            hud.RefreshNow();
            Assert.That(hud.VisibleToasts.Count, Is.EqualTo(1));
            StringAssert.Contains("SELECT A", hud.VisibleToasts[0].Message);
            Assert.That(hud.VisibleToasts[0].Count, Is.EqualTo(2));
            StringAssert.EndsWith("x2", hud.Find<Label>("toast-0").text);
        }

        [Test]
        public void ContainmentAlert_HiddenAtZero_ShowsStageWhenRaised()
        {
            var alert = hud.Find<VisualElement>("alert");
            Assert.That(Ui.IsShown(alert), Is.False);

            services.Containment.ReportGlitchApplied(3f); // 30 pressure: stage 1
            hud.RefreshNow();
            Assert.That(Ui.IsShown(alert), Is.True);
            Assert.That(alert.ClassListContains("br-alert--stage1"), Is.True);
            StringAssert.Contains("ANOMALY DETECTED", hud.Find<Label>("alert-stage").text);
        }

        [Test]
        public void PauseMenu_OpensOnPause_ResumeClosesIt()
        {
            Assert.That(menus.PauseVisible, Is.False);
            services.State.TogglePause();
            menus.RefreshNow();
            Assert.That(menus.PauseVisible, Is.True);
            Assert.That(menus.Find<Label>("pause-caption").text, Is.EqualTo("TEST LAB"));

            menus.Resume();
            Assert.That(services.State.Current, Is.EqualTo(GameState.Playing));
            menus.RefreshNow();
            Assert.That(menus.PauseVisible, Is.False);
        }

        [Test]
        public void PauseSettings_ChangeAndPersistTheColourSplitSetting()
        {
            bool previous = ComfortSettings.ChromaticEffects;
            try
            {
                ComfortSettings.ChromaticEffects = true;
                services.State.TogglePause();
                menus.RefreshNow();
                menus.OpenSettings();
                Assert.That(menus.SettingsVisible, Is.True);
                Assert.That(menus.Find<Toggle>("setting-colour-split").value, Is.True);

                menus.Settings.SetColourSplit(false);
                Assert.That(ComfortSettings.ChromaticEffects, Is.False);
                menus.CloseSettings();
                Assert.That(menus.SettingsVisible, Is.False);
                Assert.That(Progress.SaveStore.Load().settings.colourSplitEffects, Is.False, "written to the save file on close");
            }
            finally
            {
                ComfortSettings.ChromaticEffects = previous;
            }
        }

        [Test]
        public void CompleteScreen_OffersNextLevel_OnlyWhenTheCatalogHasOne()
        {
            var catalog = Track(ScriptableObject.CreateInstance<Progress.LevelCatalog>());
            catalog.Configure(new[]
            {
                new Progress.LevelCatalog.Entry("this", menus.gameObject.scene.name, "THIS", "1"),
                new Progress.LevelCatalog.Entry("next", "SomeNextScene", "NEXT", "1"),
            });
            menus.Configure(null, "TEST LAB", catalog);
            Assert.That(menus.NextLevelScene(), Is.EqualTo("SomeNextScene"));
            services.State.TryTransition(GameState.Ending);
            menus.RefreshNow();
            Assert.That(menus.NextLevelAvailable, Is.True);

            catalog.Configure(new[] { new Progress.LevelCatalog.Entry("this", menus.gameObject.scene.name, "THIS", "1") });
            Assert.That(menus.NextLevelScene(), Is.Null, "last level: no next");
        }

        [Test]
        public void CompleteScreen_ShowsOnEnding()
        {
            services.State.TryTransition(GameState.Ending);
            RefreshAll();
            Assert.That(menus.CompleteVisible, Is.True);
            Assert.That(menus.PauseVisible, Is.False);
            Assert.That(hud.Visible, Is.False);
            StringAssert.Contains("HERO REACHED THE EXIT", menus.CompleteSummaryText);
        }

        [Test]
        public void InspectionTags_OnlyInInspectionMode_AndOnlyInFrontOfTheCamera()
        {
            Entity("FRONT", new Vector3(0f, 0f, 5f), EntityCapabilities.Inspectable | EntityCapabilities.Freezable);
            Entity("BEHIND", new Vector3(0f, 0f, -5f), EntityCapabilities.Inspectable);

            inspection.RefreshNow();
            Assert.That(inspection.VisibleTagCount, Is.EqualTo(0), "no tags in normal play");

            services.State.ToggleInspection();
            inspection.RefreshNow();
            Assert.That(inspection.VisibleTagCount, Is.EqualTo(1));
            Assert.That(inspection.Find<Label>("tag-name").text, Is.EqualTo("FRONT"));
        }

        [Test]
        public void SelectedEntity_GetsThePropertyPanel()
        {
            var entity = Entity("CRATE", new Vector3(0f, 0f, 5f), EntityCapabilities.Inspectable | EntityCapabilities.Duplicable);
            inspection.RefreshNow();
            Assert.That(inspection.PanelVisible, Is.False);

            Assert.That(services.Selection.TrySelect(entity.Id).IsValid, Is.True);
            inspection.RefreshNow();
            Assert.That(inspection.PanelVisible, Is.True);
            Assert.That(inspection.Find<Label>("inspect-name").text, Is.EqualTo("CRATE"));
            Assert.That(Ui.IsShown(inspection.Find<Label>("chip-duplicate")), Is.True);
            Assert.That(Ui.IsShown(inspection.Find<Label>("chip-freeze")), Is.False);
        }

        [UnityTest]
        public IEnumerator Layers_RebuildWhenTheDocumentIsReEnabled()
        {
            document.enabled = false;
            yield return null;
            document.enabled = true;
            yield return null;
            RefreshAll();
            Assert.That(document.rootVisualElement.Q("hud"), Is.Not.Null);
            Assert.That(document.rootVisualElement.Q("menus"), Is.Not.Null);
            Assert.That(hud.Find<VisualElement>("dock"), Is.Not.Null);
        }
    }
}
