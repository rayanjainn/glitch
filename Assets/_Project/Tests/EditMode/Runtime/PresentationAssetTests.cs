using System.IO;
using System.Linq;
using BugReport.EditorTools.Setup;
using BugReport.Presentation;
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
            }
            finally
            {
                if (opened) EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
