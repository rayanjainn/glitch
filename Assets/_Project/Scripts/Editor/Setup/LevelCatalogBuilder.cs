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
            new LevelCatalog.Entry("training_lab", "Level_01_TrainingLab", SliceLevelBuilder.LevelTitle, "TUTORIAL"),
            new LevelCatalog.Entry("bridge_out", "Level_02_BridgeOut", "BRIDGE OUT", "1: HELLO, WORLD"),
            new LevelCatalog.Entry("two_doors", "Level_03_TwoDoors", "TWO DOORS, ONE SHADOW", "1: HELLO, WORLD"),
            new LevelCatalog.Entry("two_birds", "Level_04_TwoBirds", "TWO BIRDS", "1: HELLO, WORLD"),
            new LevelCatalog.Entry("three_plates", "Level_05_ThreePlates", "THREE PLATES", "2: DUPLICATE OBJECTIVE"),
            new LevelCatalog.Entry("copy_the_copy", "Level_06_CopyTheCopy", "COPY THE COPY", "2: DUPLICATE OBJECTIVE"),
            new LevelCatalog.Entry("stop_the_platform", "Level_07_StopThePlatform", "STOP THE PLATFORM", "3: TIME DOES NOT APPLY"),
            new LevelCatalog.Entry("two_pistons", "Level_08_TwoPistons", "TWO PISTONS", "3: TIME DOES NOT APPLY"),
            new LevelCatalog.Entry("the_build", "Level_09_TheBuild", "THE BUILD", "FINALE"),
        };

        public static LevelCatalog.Entry Entry(string id)
        {
            foreach (var e in Levels) if (e.id == id) return e;
            throw new System.ArgumentException("Unknown level id " + id);
        }

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
