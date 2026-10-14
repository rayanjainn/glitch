using System.Collections.Generic;
using System.IO;
using BugReport.Level;
using BugReport.Progress;
using UnityEditor;
using static BugReport.EditorTools.Setup.TestRoomBuilder;

namespace BugReport.EditorTools.Setup
{
    /// <summary>
    /// The level list (play order, stable save ids) and the Build Settings order derived from it:
    /// main menu first (what a build launches), then the levels in catalog order, then dev scenes.
    /// Add new levels to <see cref="Levels"/>; never change an id once players have saves.
    /// </summary>
    public static class LevelCatalogBuilder
    {
        public const string CatalogPath = "Assets/_Project/ScriptableObjects/Levels/LC_Main.asset";
        public const string ScenesFolder = "Assets/_Project/Scenes";
        public static string MainMenuPath => ScenesFolder + "/" + LevelFlow.MainMenuScene + ".unity";

        public static readonly LevelCatalog.Entry[] Levels =
        {
            new LevelCatalog.Entry("training_lab", "Level_01_TrainingLab", SliceLevelBuilder.LevelTitle, "1"),
        };

        public static string ScenePath(LevelCatalog.Entry entry) => ScenesFolder + "/" + entry.sceneName + ".unity";

        /// <summary>Creates or refreshes LC_Main from <see cref="Levels"/>. Call after NewScene.</summary>
        public static LevelCatalog LoadOrCreate()
        {
            var catalog = LoadOrCreate<LevelCatalog>(CatalogPath, c => c.Configure(Levels));
            catalog.Configure(Levels);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);
            return catalog;
        }

        /// <summary>Main menu, then levels in catalog order, then any other scenes (TestRoom) at the end.</summary>
        public static void SyncBuildSettings()
        {
            var ordered = new List<string>();
            if (File.Exists(MainMenuPath)) ordered.Add(MainMenuPath);
            foreach (var level in Levels)
            {
                var path = ScenePath(level);
                if (File.Exists(path)) ordered.Add(path);
            }

            var scenes = new List<EditorBuildSettingsScene>();
            foreach (var path in ordered) scenes.Add(new EditorBuildSettingsScene(path, true));
            foreach (var existing in EditorBuildSettings.scenes)
                if (!ordered.Contains(existing.path) && File.Exists(existing.path)) scenes.Add(existing);
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
