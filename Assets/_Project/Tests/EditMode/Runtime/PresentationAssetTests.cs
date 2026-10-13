using System.IO;
using System.Linq;
using BugReport.EditorTools.Setup;
using BugReport.Presentation;
using BugReport.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace BugReport.Tests.EditMode
{
    public class PresentationAssetTests
    {
        private const string ProfilePath = "Assets/_Project/Art/Rendering/VP_BugReport.asset";

        [Test]
        public void VolumeProfile_HasRequiredOverrides_SavedAsSubAssets()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile == null) Assert.Ignore("Not built yet: run a level builder.");
            Assert.That(profile.Has<Tonemapping>() && profile.Has<Bloom>() && profile.Has<Vignette>(), Is.True);
            Assert.That(profile.Has<ChromaticAberration>() && profile.Has<LensDistortion>(), Is.True, "glitch bursts need these");
            var subAssets = AssetDatabase.LoadAllAssetsAtPath(ProfilePath).OfType<VolumeComponent>().Count();
            Assert.That(subAssets, Is.EqualTo(profile.components.Count), "components must persist as sub-assets");
        }

        [Test]
        public void UiStyleAndTheme_AreInTheRepository()
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.StyleSheet>(PresentationBuilder.StyleSheetPath), Is.Not.Null, PresentationBuilder.StyleSheetPath);
            Assert.That(AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.ThemeStyleSheet>(PresentationBuilder.ThemePath), Is.Not.Null, PresentationBuilder.ThemePath);
        }

        [Test]
        public void PanelSettings_ScaleFrom1080p_WithTheme()
        {
            var panel = AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.PanelSettings>(PresentationBuilder.PanelSettingsPath);
            if (panel == null) Assert.Ignore("Not built yet: run a level builder.");
            Assert.That(panel.themeStyleSheet, Is.Not.Null);
            Assert.That(panel.scaleMode, Is.EqualTo(UnityEngine.UIElements.PanelScaleMode.ScaleWithScreenSize));
            Assert.That(panel.referenceResolution, Is.EqualTo(new Vector2Int(1920, 1080)));
        }

        [Test]
        public void SliceLevel_HasGlobalVolumeAndPostProcessingCamera()
        {
            if (!File.Exists(SliceLevelBuilder.ScenePath)) Assert.Ignore("Level not built yet.");
            var scene = SceneManager.GetSceneByPath(SliceLevelBuilder.ScenePath);
            bool opened = false;
            if (!scene.isLoaded)
            {
                scene = EditorSceneManager.OpenScene(SliceLevelBuilder.ScenePath, OpenSceneMode.Additive);
                opened = true;
            }
            try
            {
                var roots = scene.GetRootGameObjects();
                var volumes = roots.SelectMany(r => r.GetComponentsInChildren<Volume>(true)).ToList();
                Assert.That(volumes.Count(v => v.isGlobal && v.sharedProfile != null), Is.EqualTo(1));
                Assert.That(roots.SelectMany(r => r.GetComponentsInChildren<GlitchCameraFx>(true)).Count(), Is.EqualTo(1));
                var cam = roots.SelectMany(r => r.GetComponentsInChildren<Camera>(true)).Single(c => c.CompareTag("MainCamera"));
                Assert.That(cam.GetUniversalAdditionalCameraData().renderPostProcessing, Is.True);

                var documents = roots.SelectMany(r => r.GetComponentsInChildren<UnityEngine.UIElements.UIDocument>(true)).ToList();
                Assert.That(documents.Count, Is.EqualTo(1), "one shared UI document");
                Assert.That(documents[0].panelSettings, Is.Not.Null);
                Assert.That(documents[0].GetComponent<GameHud>() != null && documents[0].GetComponent<GameMenus>() != null
                    && documents[0].GetComponent<InspectionOverlay>() != null, Is.True, "all UI layers share the document");
            }
            finally
            {
                if (opened) EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
