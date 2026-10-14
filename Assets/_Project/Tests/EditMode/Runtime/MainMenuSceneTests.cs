using System.IO;
using System.Linq;
using BugReport.EditorTools.Setup;
using BugReport.Progress;
using BugReport.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace BugReport.Tests.EditMode
{
    public class MainMenuSceneTests
    {
        [Test]
        public void Catalog_IsValid_AndEveryLevelSceneExists()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(LevelCatalogBuilder.CatalogPath);
            if (catalog == null) Assert.Ignore("Not built yet: run a level builder.");
            Assert.That(catalog.Validate(), Is.Null);
            for (int i = 0; i < catalog.Count; i++)
                Assert.That(File.Exists(LevelCatalogBuilder.ScenePath(catalog.Get(i))), Is.True, catalog.Get(i).sceneName);
        }

        [Test]
        public void BuildSettings_StartWithTheMainMenu_ThenTheLevelsInOrder()
        {
            if (!File.Exists(LevelCatalogBuilder.MainMenuPath)) Assert.Ignore("Main menu not built yet: BUG REPORT > Setup > 4. Build Main Menu.");
            var paths = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToList();
            Assert.That(paths.First(), Is.EqualTo(LevelCatalogBuilder.MainMenuPath));
            int previous = 0;
            foreach (var level in LevelCatalogBuilder.Levels)
            {
                int index = paths.IndexOf(LevelCatalogBuilder.ScenePath(level));
                Assert.That(index, Is.GreaterThan(previous), level.sceneName + " in order after the menu");
                previous = index;
            }
        }

        [Test]
        public void MenuScene_HasTheMenu_AndNoBrokenReferences()
        {
            if (!File.Exists(LevelCatalogBuilder.MainMenuPath)) Assert.Ignore("Main menu not built yet.");
            var scene = SceneManager.GetSceneByPath(LevelCatalogBuilder.MainMenuPath);
            bool opened = false;
            if (!scene.isLoaded)
            {
                scene = EditorSceneManager.OpenScene(LevelCatalogBuilder.MainMenuPath, OpenSceneMode.Additive);
                opened = true;
            }
            try
            {
                var roots = scene.GetRootGameObjects();
                Assert.That(roots.SelectMany(r => r.GetComponentsInChildren<MainMenu>(true)).Count(), Is.EqualTo(1));
                Assert.That(roots.SelectMany(r => r.GetComponentsInChildren<SettingsApplier>(true)).Count(), Is.EqualTo(1));
                Assert.That(TestRoomBuilder.FindUnassignedReferences(scene), Is.Empty);
            }
            finally
            {
                if (opened) EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
