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

        private static LevelCatalog.Entry L(string id, string scene, string title, string chapter, int chapterIndex, int par, string intro, string outro)
            => new LevelCatalog.Entry(id, scene, title, chapter, par, chapterIndex, intro, outro);

        public static readonly LevelCatalog.Entry[] Levels =
        {
            L("training_lab", "Level_01_TrainingLab", SliceLevelBuilder.LevelTitle, "1: HELLO, WORLD", 1, 6,
                null,
                "The knight reached the exit and has no idea anything happened. The system files a note: ANOMALY, LOW PRIORITY."),
            L("bridge_out", "Level_02_BridgeOut", "BRIDGE OUT", "1: HELLO, WORLD", 1, 1,
                "The bridge is a hologram. The old barrier is solid. Solidity is just a setting.",
                "Cross-referencing: 'bridge' now has collision. Cause unknown."),
            L("two_doors", "Level_03_TwoDoors", "TWO DOORS, ONE SHADOW", "1: HELLO, WORLD", 1, 2,
                "Two firewalls, one shadow. If a power is in use, [R] undoes it and gives the object back.",
                "Firewall breach logged twice. Both marked 'resolved'. Neither was."),
            L("two_birds", "Level_04_TwoBirds", "TWO BIRDS", "1: HELLO, WORLD", 1, 1,
                "One transfer can fix two problems, if you think about where the solidity lands.",
                "The designers will notice that one. They won't understand it."),
            L("fork_in_the_road", "Level_21_ForkInTheRoad", "FORK IN THE ROAD", "1: HELLO, WORLD", 1, 1,
                "Two roads. The knight takes the shortest one that is open. You decide which one that is.",
                "He never noticed there was a choice."),
            L("three_plates", "Level_05_ThreePlates", "THREE PLATES", "2: DUPLICATE OBJECTIVE", 2, 2,
                "Three plates, one crate. The game never said you only get one.",
                "ITEM COUNT: +2. Nobody authorised that."),
            L("copy_the_copy", "Level_06_CopyTheCopy", "COPY THE COPY", "2: DUPLICATE OBJECTIVE", 2, 2,
                "A copy is a crate like any other. It can be copied too.",
                "Duplicates of duplicates. The inventory system has stopped counting."),
            L("stop_the_platform", "Level_07_StopThePlatform", "STOP THE PLATFORM", "3: TIME DOES NOT APPLY", 3, 1,
                "Time is just another rule. Pause the platform; the knight will take it from there.",
                "Timer anomaly detected. Timer anomaly ignored."),
            L("two_pistons", "Level_08_TwoPistons", "TWO PISTONS", "3: TIME DOES NOT APPLY", 3, 2,
                "A freeze does not last forever. Spend them one at a time.",
                "The pistons were never meant to stop. Neither was he."),
            L("the_build", "Level_09_TheBuild", "THE BUILD", "3: TIME DOES NOT APPLY", 3, 3,
                "A bit of everything you have learned so far. A recap, and a rehearsal.",
                "Build 1.0 passed internal testing. Your fingerprints are all over it."),
            L("heavy_lifting", "Level_10_HeavyLifting", "HEAVY LIFTING", "4: DEAD WEIGHT", 4, 1,
                "The system is checking weights now. A number is just a number.",
                "MASS ERROR: crate = 60 kg. Cannot reproduce."),
            L("twin_scales", "Level_11_TwinScales", "TWIN SCALES", "4: DEAD WEIGHT", 4, 3,
                "Two plates, two heavy demands. You have the tools to meet both.",
                "Scales calibrated. Probably."),
            L("weigh_station", "Level_12_WeighStation", "WEIGH STATION", "4: DEAD WEIGHT", 4, 2,
                "A gate that wants weight, a pit that wants floor. Different problems, different tools.",
                "Two systems failed on the same tick. Coincidence, says the log."),
            L("night_watch", "Level_13_NightWatch", "NIGHT WATCH", "5: NIGHT SHIFT", 5, 1,
                "Security has arrived. The guard is not stupid, only easily distracted.",
                "Guard reports: 'I was investigating a barrel.'"),
            L("double_shift", "Level_14_DoubleShift", "DOUBLE SHIFT", "5: NIGHT SHIFT", 5, 2,
                "Two guards, one decoy, fourteen seconds. Be patient.",
                "Both guards were later found near the same barrel."),
            L("frozen_stiff", "Level_15_FrozenStiff", "FROZEN STIFF", "5: NIGHT SHIFT", 5, 2,
                "A guard and a piston. Different problems; you know which glitch fits which.",
                "Staff morale is low. Maintenance is lower."),
            L("false_wall", "Level_16_FalseWall", "FALSE WALL", "6: FALSE FLOORS", 6, 1,
                "The system is losing track of what is where. A wall can be somewhere else than it looks.",
                "Collision and visuals disagree. Please do not tell anyone."),
            L("false_floor", "Level_17_FalseFloor", "FALSE FLOOR", "6: FALSE FLOORS", 6, 1,
                "The same trick builds as well as breaks. Slide a floor where it is needed.",
                "A floor has been found in a pit. Investigating."),
            L("searchlight", "Level_18_Searchlight", "SEARCHLIGHT", "6: FALSE FLOORS", 6, 2,
                "The system sends a drone. Anything in its light is reported. Mind your containment.",
                "Drone report: 'I saw nothing. I saw it very clearly.'"),
            L("stack_the_order", "Level_19_StackTheOrder", "STACK THE ORDER", "6: FALSE FLOORS", 6, 2,
                "The system is on edge today. Whatever you break first, it fixes first.",
                "Patch notes: 'Fixed a crate.' The bridge was not mentioned."),
            L("report_closed", "Level_20_ReportClosed", "REPORT CLOSED", "7: REPORT CLOSED", 7, 5,
                "The last room. Every rule you learned, in order. Close the report.",
                "BUG REPORT #1: CLOSED. Resolution: 'works as intended'. The knight walks home. You stay in the game."),
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

        /// <summary>Deletes level scenes that are no longer in the catalog (renamed or replaced levels).</summary>
        public static int DeleteStaleScenes()
        {
            var known = new HashSet<string>();
            foreach (var level in Levels) known.Add(ScenePath(level));
            int deleted = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Scene Level_", new[] { ScenesFolder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!Path.GetFileName(path).StartsWith("Level_") || known.Contains(path)) continue;
                if (AssetDatabase.DeleteAsset(path)) deleted++;
            }
            return deleted;
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
