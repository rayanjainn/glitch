using System;
using BugReport.Core.Entities;
using BugReport.EditorTools.Setup;
using BugReport.Glitches;
using BugReport.Level;
using BugReport.Progress;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BugReport.EditorTools.Levels
{
    /// <summary>
    /// The demo's levels after TRAINING LAB (see DESIGN_ROADMAP §3). Each level is a short design
    /// note plus the code that builds it; the intended solution is written down and replayed by
    /// the automated playthrough tests (LevelPlaythroughTests), so a level that can't be finished
    /// fails a test instead of a player.
    ///
    /// Conventions: the hero walks along z = 0 towards +x; the hall is 12 m wide (|z| ≤ 6); every
    /// level ends at an exit; glitches not yet taught are simply not loaded.
    /// </summary>
    public static class DemoLevels
    {
        [MenuItem(ProjectSetup.MenuRoot + "5. Build ALL (menu + every level)", priority = 5)]
        public static void BuildAllMenu()
        {
            if (!GameLayers.AreConfigured(out var missing))
            {
                EditorUtility.DisplayDialog("BUG REPORT", "Missing layers: " + missing + ".\nRun 'Configure Project' first.", "OK");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            int failures = BuildAll(out var report);
            if (failures == 0) Debug.Log("[BUG REPORT] Built the main menu, the test room and every level:\n" + report);
            else Debug.LogError("[BUG REPORT] " + failures + " build(s) failed:\n" + report);
        }

        /// <summary>Builds everything in catalog order. Returns the number of failures.</summary>
        public static int BuildAll(out string report)
        {
            var lines = new System.Text.StringBuilder();
            int failures = 0;
            void Run(string what, Func<(bool ok, string error)> build)
            {
                (bool ok, string error) result;
                try
                {
                    result = build();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    result = (false, e.Message);
                }
                if (!result.ok) failures++;
                lines.Append(result.ok ? "  OK    " : "  FAIL  ").Append(what).Append(result.ok ? string.Empty : ": " + result.error).Append('\n');
            }

            Run("TestRoom", () => (TestRoomBuilder.Build(out var e), e));
            Run("Level_01_TrainingLab", () => (SliceLevelBuilder.Build(out var e), e));
            Run("Level_02_BridgeOut", () => (BridgeOut(out var e), e));
            Run("Level_03_TwoDoors", () => (TwoDoors(out var e), e));
            Run("Level_04_TwoBirds", () => (TwoBirds(out var e), e));
            Run("Level_05_ThreePlates", () => (ThreePlates(out var e), e));
            Run("Level_06_CopyTheCopy", () => (CopyTheCopy(out var e), e));
            Run("Level_07_StopThePlatform", () => (StopThePlatform(out var e), e));
            Run("Level_08_TwoPistons", () => (TwoPistons(out var e), e));
            Run("Level_09_TheBuild", () => (TheBuild(out var e), e));
            Run("Level_10_HeavyLifting", () => (FullGameLevels.HeavyLifting(out var e), e));
            Run("Level_11_TwinScales", () => (FullGameLevels.TwinScales(out var e), e));
            Run("Level_12_WeighStation", () => (FullGameLevels.WeighStation(out var e), e));
            Run("Level_13_NightWatch", () => (FullGameLevels.NightWatch(out var e), e));
            Run("Level_14_DoubleShift", () => (FullGameLevels.DoubleShift(out var e), e));
            Run("Level_15_FrozenStiff", () => (FullGameLevels.FrozenStiff(out var e), e));
            Run("Level_16_FalseWall", () => (FullGameLevels.FalseWall(out var e), e));
            Run("Level_17_FalseFloor", () => (FullGameLevels.FalseFloor(out var e), e));
            Run("Level_18_Searchlight", () => (FullGameLevels.Searchlight(out var e), e));
            Run("Level_19_PatchDay", () => (FullGameLevels.PatchDay(out var e), e));
            Run("Level_20_ReportClosed", () => (FullGameLevels.ReportClosed(out var e), e));
            Run("MainMenu", () => (MenuBuilder.Build(out var e), e)); // last, so it opens when done
            report = lines.ToString();
            return failures;
        }

        internal static string PathOf(string id) => LevelCatalogBuilder.ScenePath(LevelCatalogBuilder.Entry(id));
        internal static string TitleOf(string id) => LevelCatalogBuilder.Entry(id).title;

        // ================================================================ chapter 1: Collision Transfer

        /// <summary>
        /// BRIDGE OUT. A pit splits the hall; the bridge over it is a hologram (no floor for the hero).
        /// An old, useless barrier is solid. Solution: Collision Transfer OLD_BARRIER → HOLO_BRIDGE.
        /// Lesson: solidity can come from anything that has it.
        /// </summary>
        public static bool BridgeOut(out string error)
        {
            var kit = LevelKit.Begin(out error, 1);
            if (kit == null) return false;
            kit.Hall(18f, 6f, (-3f, 3f));
            kit.Barrier("OldBarrier", "OLD_BARRIER", new Vector3(-10f, 1f, 4.6f), new Vector3(3f, 2f, 0.4f),
                "Left over from an old build. Solid, and no longer needed.");
            var bridge = kit.HoloBridge("HoloBridge", "HOLO_BRIDGE", -3f, 3f, 2.4f);
            kit.Exit(15f);
            kit.HeroOnRoute(new Vector3(-15f, 0f, 0f), true, new Vector3(-4f, 0f, 0f), new Vector3(4f, 0f, 0f), new Vector3(15f, 0f, 0f));
            var objectives = kit.Objectives(
                new ObjectiveDirector.Entry("cross", "GET THE HERO ACROSS THE GAP",
                    "The bridge is only a hologram. What else in this room is solid?", kit.ZoneBetween("Zone_Across", 3.5f, 13.5f), bridge),
                new ObjectiveDirector.Entry("exit", "REACH THE EXIT", "Nothing left in the way.", kit.ZoneAtExit(15f)));
            kit.Dress(("barrel_large", -16.5f, 5f, 0f, 1.1f), ("crates_stacked", -16.5f, -5f, 90f, 1.3f),
                ("keg", 8f, 5.1f, 0f, 0.9f), ("chest_gold", 16.8f, -2.4f, -90f, 0.8f));
            return kit.Finish(PathOf("bridge_out"), TitleOf("bridge_out"), new GlitchDefinition[] { kit.CollisionTransfer },
                objectives, new Vector3(-16f, 2.2f, -4f), out error);
        }

        /// <summary>
        /// TWO DOORS, ONE SHADOW. Two firewalls, one shadow. FIREWALL_A → SHADOW_DECAL lets the hero through
        /// the first wall. The shadow is now "in use" (an object can only be part of one transfer), so once the
        /// hero is through, undo it with [R] (the first wall turns solid behind the hero) and transfer
        /// FIREWALL_B → SHADOW_DECAL. Lesson: undo is a tool, and timing matters.
        /// </summary>
        public static bool TwoDoors(out string error)
        {
            var kit = LevelKit.Begin(out error, 1);
            if (kit == null) return false;
            kit.Hall(20f, 6f);
            var wallA = kit.Firewall("FirewallA", "FIREWALL_A", -7f);
            var wallB = kit.Firewall("FirewallB", "FIREWALL_B", 7f);
            kit.Shadow("Shadow", "SHADOW_DECAL", new Vector3(-14f, 0.02f, 3.5f));
            kit.Exit(17f);
            kit.HeroOnRoute(new Vector3(-17f, 0f, 0f), false,
                new Vector3(-9f, 0f, 0f), new Vector3(-5f, 0f, 0f), new Vector3(5f, 0f, 0f), new Vector3(9f, 0f, 0f), new Vector3(17f, 0f, 0f));
            var objectives = kit.Objectives(
                new ObjectiveDirector.Entry("first", "GET PAST THE FIRST FIREWALL", "There is only one shadow in here. Use it.",
                    kit.ZoneBetween("Zone_Middle", -6.5f, 6.5f), wallA),
                new ObjectiveDirector.Entry("second", "GET PAST THE SECOND FIREWALL",
                    "The shadow is busy. [R] undoes your last glitch: the first wall doesn't matter any more.", kit.ZoneAtExit(17f), wallB));
            kit.Dress(("barrel_small_stack", -18f, 5f, 0f, 1f), ("box_stacked", -2f, -5f, 15f, 1.2f), ("barrel_large", 2f, 5f, 0f, 1.1f),
                ("chest_gold", 18.8f, -2.4f, -90f, 0.8f));
            return kit.Finish(PathOf("two_doors"), TitleOf("two_doors"), new GlitchDefinition[] { kit.CollisionTransfer },
                objectives, new Vector3(-18f, 2.2f, -4f), out error);
        }

        /// <summary>
        /// TWO BIRDS. A firewall, then a pit with a hologram bridge, then a hologram door in a doorway.
        /// No shadow: the only solidity is the firewall's. FIREWALL → HOLO_BRIDGE solves both obstacles at
        /// once. FIREWALL → HOLO_DOOR is the trap: the door becomes a wall and the gap stays open (undo).
        /// Lesson: think about where the solidity lands.
        /// </summary>
        public static bool TwoBirds(out string error)
        {
            var kit = LevelKit.Begin(out error, 1);
            if (kit == null) return false;
            kit.Hall(20f, 6f, (2f, 6f));
            var firewall = kit.Firewall("Firewall", "FIREWALL", -8f);
            var bridge = kit.HoloBridge("HoloBridge", "HOLO_BRIDGE", 2f, 6f, 2.4f);
            kit.Divider("DoorWall", 12f, 3.2f);
            kit.HoloDoor("HoloDoor", "HOLO_DOOR", 12f, 3.2f);
            kit.Exit(17f);
            kit.HeroOnRoute(new Vector3(-17f, 0f, 0f), true,
                new Vector3(-10f, 0f, 0f), new Vector3(-6f, 0f, 0f), new Vector3(1f, 0f, 0f), new Vector3(7f, 0f, 0f),
                new Vector3(11f, 0f, 0f), new Vector3(13f, 0f, 0f), new Vector3(17f, 0f, 0f));
            var objectives = kit.Objectives(
                new ObjectiveDirector.Entry("firewall", "GET PAST THE FIREWALL", "One transfer. Two problems.", kit.ZoneBetween("Zone_Lab", -7.5f, 1.5f), firewall),
                new ObjectiveDirector.Entry("gap", "CROSS THE GAP", "Where did that solidity go?", kit.ZoneBetween("Zone_FarSide", 6.5f, 11.5f), bridge),
                new ObjectiveDirector.Entry("exit", "REACH THE EXIT", "That door had better still be a hologram.", kit.ZoneAtExit(17f)));
            kit.Dress(("barrel_large", -18f, 5f, 0f, 1.1f), ("crates_stacked", -12f, -5f, 0f, 1.3f), ("keg", 9f, 5.1f, 0f, 0.9f),
                ("chest_gold", 18.8f, -2.4f, -90f, 0.8f));
            return kit.Finish(PathOf("two_birds"), TitleOf("two_birds"), new GlitchDefinition[] { kit.CollisionTransfer },
                objectives, new Vector3(-18f, 2.2f, -4f), out error);
        }

        // ================================================================ chapter 2: Duplication

        /// <summary>
        /// THREE PLATES. The gate needs three plates; one crate sits on the middle one. A copy appears
        /// beside its source (right first, then left): duplicating the crate twice covers both outer plates.
        /// Lesson: duplication can be repeated.
        /// </summary>
        public static bool ThreePlates(out string error)
        {
            var kit = LevelKit.Begin(out error, 2);
            if (kit == null) return false;
            kit.Hall(18f, 6f);
            var plates = new[]
            {
                kit.Plate("Plate_1", new Vector3(-2.1f, 0f, -3f)),
                kit.Plate("Plate_2", new Vector3(-1f, 0f, -3f)),
                kit.Plate("Plate_3", new Vector3(0.1f, 0f, -3f)),
            };
            var crate = kit.Crate("Crate", "CRATE", new Vector3(-1f, 0f, -3f), "Heavy enough for one plate.");
            kit.Gate("Gate", 4f, plates);
            kit.Exit(15f);
            kit.HeroOnRoute(new Vector3(-15f, 0f, 0f), false, new Vector3(2f, 0f, 0f), new Vector3(6f, 0f, 0f), new Vector3(15f, 0f, 0f));
            var objectives = kit.Objectives(
                new ObjectiveDirector.Entry("gate", "OPEN THE GATE", "Three plates. One crate. Copies can be made more than once.",
                    kit.ZoneBetween("Zone_Beyond", 4.5f, 13.5f), crate),
                new ObjectiveDirector.Entry("exit", "REACH THE EXIT", "Off you go.", kit.ZoneAtExit(15f)));
            kit.Dress(("barrel_large", -16.5f, 5f, 0f, 1.1f), ("shelf_small_candles", -8f, 5.3f, 180f, 1.6f), ("keg", 10f, -5.1f, 0f, 0.9f),
                ("chest_gold", 16.8f, -2.4f, -90f, 0.8f));
            return kit.Finish(PathOf("three_plates"), TitleOf("three_plates"),
                new GlitchDefinition[] { kit.CollisionTransfer, kit.Duplication }, objectives, new Vector3(-16f, 2.2f, -4f), out error);
        }

        /// <summary>
        /// COPY THE COPY. Three plates in a row; the crate is on the first and a pillar blocks the space to
        /// its left. The first copy lands on plate 2; copying the crate again puts the copy in front, off the
        /// plates. Copying the copy puts the new one on plate 3. Lesson: copies are full objects.
        /// </summary>
        public static bool CopyTheCopy(out string error)
        {
            var kit = LevelKit.Begin(out error, 2);
            if (kit == null) return false;
            kit.Hall(18f, 6f);
            var plates = new[]
            {
                kit.Plate("Plate_1", new Vector3(-1f, 0f, -3f)),
                kit.Plate("Plate_2", new Vector3(0.1f, 0f, -3f)),
                kit.Plate("Plate_3", new Vector3(1.2f, 0f, -3f)),
            };
            TestRoomBuilder.Block("Pillar", kit.Env, new Vector3(-2.1f, 0.75f, -3f), new Vector3(1f, 1.5f, 1f), kit.Mats.Wall, kit.World);
            var crate = kit.Crate("Crate", "CRATE", new Vector3(-1f, 0f, -3f), "Heavy enough for one plate. Copies are crates too.");
            kit.Gate("Gate", 4f, plates);
            kit.Exit(15f);
            kit.HeroOnRoute(new Vector3(-15f, 0f, 0f), false, new Vector3(2f, 0f, 0f), new Vector3(6f, 0f, 0f), new Vector3(15f, 0f, 0f));
            var objectives = kit.Objectives(
                new ObjectiveDirector.Entry("gate", "OPEN THE GATE", "Three plates in a row. A copy lands next to whatever was copied.",
                    kit.ZoneBetween("Zone_Beyond", 4.5f, 13.5f), crate),
                new ObjectiveDirector.Entry("exit", "REACH THE EXIT", "Off you go.", kit.ZoneAtExit(15f)));
            kit.Dress(("barrel_small_stack", -16.5f, 5f, 0f, 1f), ("crates_stacked", -9f, 5f, 0f, 1.3f), ("barrel_large", 10f, -5f, 0f, 1.1f),
                ("chest_gold", 16.8f, -2.4f, -90f, 0.8f));
            return kit.Finish(PathOf("copy_the_copy"), TitleOf("copy_the_copy"),
                new GlitchDefinition[] { kit.CollisionTransfer, kit.Duplication }, objectives, new Vector3(-16f, 2.2f, -4f), out error);
        }

        // ================================================================ chapter 3: Freeze

        /// <summary>
        /// STOP THE PLATFORM. A pit with a platform shuttling sideways across it. The hero won't step onto
        /// moving machinery and needs floor under every step: freeze the platform while it lines up with the
        /// path. Lesson: Freeze turns machinery into terrain.
        /// </summary>
        public static bool StopThePlatform(out string error)
        {
            var kit = LevelKit.Begin(out error, 3);
            if (kit == null) return false;
            kit.Hall(18f, 6f, (-2f, 2f));
            var platform = kit.MovingPlatform("Platform", "SHUTTLE_PLATFORM", -2f, 2f, 4f, 4f, 0f);
            kit.Exit(15f);
            kit.HeroOnRoute(new Vector3(-15f, 0f, 0f), true, new Vector3(-3f, 0f, 0f), new Vector3(3f, 0f, 0f), new Vector3(15f, 0f, 0f));
            var objectives = kit.Objectives(
                new ObjectiveDirector.Entry("cross", "GET THE HERO ACROSS", "The hero won't step onto something that moves. Catch it when it lines up.",
                    kit.ZoneBetween("Zone_Across", 2.5f, 13.5f), platform),
                new ObjectiveDirector.Entry("exit", "REACH THE EXIT", "Keep going.", kit.ZoneAtExit(15f)));
            kit.Dress(("barrel_large", -16.5f, 5f, 0f, 1.1f), ("keg", -10f, -5.1f, 0f, 0.9f), ("crates_stacked", 9f, 5f, 0f, 1.3f),
                ("chest_gold", 16.8f, -2.4f, -90f, 0.8f));
            return kit.Finish(PathOf("stop_the_platform"), TitleOf("stop_the_platform"),
                new GlitchDefinition[] { kit.CollisionTransfer, kit.Duplication, kit.Freeze }, objectives, new Vector3(-16f, 2.2f, -4f), out error);
        }

        /// <summary>
        /// TWO PISTONS. Two piston corridors out of step. Freeze the first while it is pulled back, let the
        /// hero through, then the second. A freeze lasts 8 seconds, so freezing both early does not work.
        /// Lesson: timing.
        /// </summary>
        public static bool TwoPistons(out string error)
        {
            var kit = LevelKit.Begin(out error, 3);
            if (kit == null) return false;
            kit.Hall(20f, 6f);
            var pistonA = kit.Piston("PistonA", "PISTON_A", -4f, 3f, 0f);
            var pistonB = kit.Piston("PistonB", "PISTON_B", 6f, 2.4f, 0.5f);
            kit.Exit(17f);
            kit.HeroOnRoute(new Vector3(-17f, 0f, 0f), false,
                new Vector3(-7f, 0f, 0f), new Vector3(-1f, 0f, 0f), new Vector3(3f, 0f, 0f), new Vector3(9f, 0f, 0f), new Vector3(17f, 0f, 0f));
            var objectives = kit.Objectives(
                new ObjectiveDirector.Entry("first", "GET PAST THE FIRST PISTON", "Freeze it while it is pulled back.", kit.ZoneBetween("Zone_Middle", -1.8f, 3.8f), pistonA),
                new ObjectiveDirector.Entry("second", "GET PAST THE SECOND PISTON", "Freezes don't last forever. One at a time.", kit.ZoneAtExit(17f), pistonB));
            kit.Dress(("barrel_large", -18f, 5f, 0f, 1.1f), ("box_stacked", -12f, -5f, 0f, 1.2f), ("keg", 13f, 5.1f, 0f, 0.9f),
                ("chest_gold", 18.8f, -2.4f, -90f, 0.8f));
            return kit.Finish(PathOf("two_pistons"), TitleOf("two_pistons"),
                new GlitchDefinition[] { kit.CollisionTransfer, kit.Duplication, kit.Freeze }, objectives, new Vector3(-18f, 2.2f, -4f), out error);
        }

        // ================================================================ finale

        /// <summary>
        /// THE BUILD. Everything once more: firewall → hologram bridge over a pit (Collision Transfer),
        /// a two-plate gate with one crate (Duplication), and a piston (Freeze).
        /// </summary>
        public static bool TheBuild(out string error)
        {
            var kit = LevelKit.Begin(out error, 3);
            if (kit == null) return false;
            kit.Hall(24f, 6f, (-12f, -8f));
            var firewall = kit.Firewall("Firewall", "FIREWALL", -16f);
            kit.HoloBridge("HoloBridge", "HOLO_BRIDGE", -12f, -8f, 2.4f);
            var plates = new[]
            {
                kit.Plate("Plate_1", new Vector3(-4.1f, 0f, -3f)),
                kit.Plate("Plate_2", new Vector3(-3f, 0f, -3f)),
            };
            var crate = kit.Crate("Crate", "CRATE", new Vector3(-4.1f, 0f, -3f), "One crate, two plates. You know this one.");
            kit.Gate("Gate", 0f, plates);
            var piston = kit.Piston("Piston", "CRUSHER_PISTON", 8f, 3f, 0f);
            kit.Exit(21f);
            kit.HeroOnRoute(new Vector3(-22f, 0f, 0f), true,
                new Vector3(-18f, 0f, 0f), new Vector3(-14f, 0f, 0f), new Vector3(-13f, 0f, 0f), new Vector3(-7f, 0f, 0f),
                new Vector3(-2f, 0f, 0f), new Vector3(2f, 0f, 0f), new Vector3(5f, 0f, 0f), new Vector3(11f, 0f, 0f), new Vector3(21f, 0f, 0f));
            var objectives = kit.Objectives(
                new ObjectiveDirector.Entry("gap", "CROSS THE GAP", "One wall. One bridge.", kit.ZoneBetween("Zone_Lab", -7.5f, -0.5f), firewall),
                new ObjectiveDirector.Entry("gate", "OPEN THE GATE", "Two plates. One crate.", kit.ZoneBetween("Zone_Machines", 0.5f, 5.8f), crate),
                new ObjectiveDirector.Entry("exit", "GET PAST THE PISTON", "Catch it pulled back.", kit.ZoneAtExit(21f), piston));
            kit.Dress(("barrel_large", -23f, 5f, 0f, 1.1f), ("crates_stacked", -20f, -5f, 90f, 1.3f), ("shelf_small_candles", -5f, 5.3f, 180f, 1.6f),
                ("keg", 14f, 5.1f, 0f, 0.9f), ("barrel_small_stack", 17f, -5f, 0f, 1f), ("chest_gold", 22.8f, -2.4f, -90f, 0.8f));
            return kit.Finish(PathOf("the_build"), TitleOf("the_build"),
                new GlitchDefinition[] { kit.CollisionTransfer, kit.Duplication, kit.Freeze }, objectives, new Vector3(-23f, 2.2f, -4f), out error);
        }
    }
}
