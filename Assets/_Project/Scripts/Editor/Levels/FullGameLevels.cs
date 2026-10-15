using BugReport.EditorTools.Setup;
using BugReport.Glitches;
using BugReport.Level;
using UnityEngine;
using static BugReport.EditorTools.Levels.DemoLevels;

namespace BugReport.EditorTools.Levels
{
    /// <summary>
    /// Chapters 4–7: Property Swap (weights), AI Override (guards), Desync (false walls and floors),
    /// security drones, and the finale. Same conventions as <see cref="DemoLevels"/>: the hero walks
    /// along z = 0 towards +x, the intended solution is written in the summary and replayed by
    /// FullGamePlaythroughTests, and glitches not yet taught are simply not loaded.
    /// </summary>
    internal static class FullGameLevels
    {
        private static Vector3 V(float x) => new Vector3(x, 0f, 0f);

        // ================================================================ chapter 4: Property Swap

        /// <summary>
        /// HEAVY LIFTING. A plate that needs 25 kg, a 10 kg crate on it, a 60 kg anvil lying about.
        /// Solution: Mass Swap CRATE ↔ ANVIL. Lesson: a swap trades the numbers, not the objects.
        /// </summary>
        public static bool HeavyLifting(out string error)
        {
            var kit = LevelKit.Begin(out error, 4);
            if (kit == null) return false;
            kit.Hall(18f, 6f);
            var plate = kit.HeavyPlate("Plate", new Vector3(-1f, 0f, -3f), 25f);
            kit.Crate("Crate", "CRATE", new Vector3(-1f, 0f, -3f), "Light. The plate wants something heavier.");
            var anvil = kit.Weight("Anvil", "ANVIL", new Vector3(-10f, 0f, 4f), 60f, 1.2f, "A 60 kg anvil.");
            kit.Gate("Gate", 4f, new[] { plate });
            kit.Exit(15f);
            kit.HeroOnRoute(V(-15f), false, V(2f), V(6f), V(15f));
            var objectives = kit.Objectives(
                new ObjectiveDirector.Entry("gate", "OPEN THE GATE", "The plate needs more weight than the crate has. Something else here has plenty.",
                    kit.ZoneBetween("Zone_Beyond", 4.5f, 13.5f), anvil),
                new ObjectiveDirector.Entry("exit", "REACH THE EXIT", "Off you go.", kit.ZoneAtExit(15f)));
            return kit.Finish(PathOf("heavy_lifting"), TitleOf("heavy_lifting"), new GlitchDefinition[] { kit.MassSwap },
                objectives, new Vector3(-16f, 2.2f, -4f), out error);
        }

        /// <summary>
        /// TWIN SCALES. Two 25 kg plates, one crate, an anvil (60) and a boulder (40). Solution: Duplication of the
        /// crate puts a copy on the second plate; Mass Swap CRATE ↔ ANVIL and COPY ↔ BOULDER. Lesson: glitches combine.
        /// </summary>
        public static bool TwinScales(out string error)
        {
            var kit = LevelKit.Begin(out error, 4);
            if (kit == null) return false;
            kit.Hall(18f, 6f);
            var plates = new[]
            {
                kit.HeavyPlate("Plate_1", new Vector3(-1f, 0f, -3f), 25f),
                kit.HeavyPlate("Plate_2", new Vector3(0.1f, 0f, -3f), 25f),
            };
            var crate = kit.Crate("Crate", "CRATE", new Vector3(-1f, 0f, -3f), "Light, and only one of it. For now.");
            kit.Weight("Anvil", "ANVIL", new Vector3(-12f, 0f, 4.2f), 60f, 1.2f, "The heaviest thing in the room.");
            kit.Weight("Boulder", "BOULDER", new Vector3(-8f, 0f, 4.2f), 40f, 1.4f, "Heavy enough for a plate.");
            kit.Gate("Gate", 4f, plates);
            kit.Exit(15f);
            kit.HeroOnRoute(V(-15f), false, V(2f), V(6f), V(15f));
            var objectives = kit.Objectives(
                new ObjectiveDirector.Entry("gate", "OPEN THE GATE", "Two plates, both hungry. One crate, two heavy things.",
                    kit.ZoneBetween("Zone_Beyond", 4.5f, 13.5f), crate),
                new ObjectiveDirector.Entry("exit", "REACH THE EXIT", "Off you go.", kit.ZoneAtExit(15f)));
            return kit.Finish(PathOf("twin_scales"), TitleOf("twin_scales"), new GlitchDefinition[] { kit.Duplication, kit.MassSwap },
                objectives, new Vector3(-16f, 2.2f, -4f), out error);
        }

        /// <summary>
        /// WEIGH STATION. A heavy-plate gate, then a pit with a hologram bridge. Solution: Mass Swap CRATE ↔ ANVIL
        /// for the gate; Collision Transfer OLD_BARRIER → HOLO_BRIDGE for the gap. Lesson: pick the right tool per obstacle.
        /// </summary>
        public static bool WeighStation(out string error)
        {
            var kit = LevelKit.Begin(out error, 4);
            if (kit == null) return false;
            kit.Hall(20f, 6f, (8f, 12f));
            var plate = kit.HeavyPlate("Plate", new Vector3(-8f, 0f, -3f), 25f);
            kit.Crate("Crate", "CRATE", new Vector3(-8f, 0f, -3f), "Light. The plate wants more.");
            var anvil = kit.Weight("Anvil", "ANVIL", new Vector3(-15f, 0f, 4.2f), 60f, 1.2f, "Heavy. Obviously.");
            kit.Gate("Gate", -3f, new[] { plate });
            kit.Barrier("OldBarrier", "OLD_BARRIER", new Vector3(2f, 1f, 4.6f), new Vector3(3f, 2f, 0.4f), "Solid, and in nobody's way.");
            var bridge = kit.HoloBridge("HoloBridge", "HOLO_BRIDGE", 8f, 12f, 2.4f);
            kit.Exit(17f);
            kit.HeroOnRoute(V(-17f), true, V(-5f), V(-1f), V(7f), V(13f), V(17f));
            var objectives = kit.Objectives(
                new ObjectiveDirector.Entry("gate", "OPEN THE GATE", "The plate is too demanding for the crate.", kit.ZoneBetween("Zone_Gate", -2.5f, 7.5f), anvil),
                new ObjectiveDirector.Entry("gap", "CROSS THE GAP", "A hologram bridge.", kit.ZoneBetween("Zone_Far", 12.5f, 16f), bridge),
                new ObjectiveDirector.Entry("exit", "REACH THE EXIT", "Almost.", kit.ZoneAtExit(17f)));
            return kit.Finish(PathOf("weigh_station"), TitleOf("weigh_station"),
                new GlitchDefinition[] { kit.CollisionTransfer, kit.MassSwap }, objectives, new Vector3(-18f, 2.2f, -4f), out error);
        }

        // ================================================================ chapter 5: AI Override

        /// <summary>
        /// NIGHT WATCH. A skeleton guard paces the doorway and the hero will not walk into it. Solution: AI Override
        /// SKELETON_GUARD → DECOY_BARREL. Lesson: guards can be talked into leaving.
        /// </summary>
        public static bool NightWatch(out string error)
        {
            var kit = LevelKit.Begin(out error, 5);
            if (kit == null) return false;
            kit.Hall(16f, 6f);
            kit.Decoy("Decoy", "DECOY_BARREL", new Vector3(-6f, 0f, 4.5f));
            kit.HeroOnRoute(V(-13f), false, V(-3f), V(3f), V(13f));
            var guard = kit.GuardPost("Guard", "SKELETON_GUARD", 0f).GetComponent<GameEntity>();
            kit.Exit(13f);
            var objectives = kit.Objectives(
                new ObjectiveDirector.Entry("guard", "GET PAST THE GUARD", "The guard won't leave its post.",
                    kit.ZoneBetween("Zone_Beyond", 3.5f, 11.5f), guard),
                new ObjectiveDirector.Entry("exit", "REACH THE EXIT", "Quietly.", kit.ZoneAtExit(13f)));
            return kit.Finish(PathOf("night_watch"), TitleOf("night_watch"), new GlitchDefinition[] { kit.AiOverride },
                objectives, new Vector3(-14f, 2.2f, -4f), out error);
        }

        /// <summary>
        /// DOUBLE SHIFT. Two guards, one decoy, and a decoy can hold only one lure at a time. Solution: lure the first guard,
        /// let the hero through, undo with [R], then lure the second. Lesson: undo hands the decoy back.
        /// </summary>
        public static bool DoubleShift(out string error)
        {
            var kit = LevelKit.Begin(out error, 5);
            if (kit == null) return false;
            kit.Hall(24f, 6f);
            kit.Decoy("Decoy", "DECOY_BARREL", new Vector3(0f, 0f, 4.5f));
            kit.HeroOnRoute(V(-21f), false, V(-11f), V(-5f), V(5f), V(11f), V(21f));
            var guardA = kit.GuardPost("GuardA", "GUARD_A", -8f).GetComponent<GameEntity>();
            var guardB = kit.GuardPost("GuardB", "GUARD_B", 8f).GetComponent<GameEntity>();
            kit.Exit(21f);
            var objectives = kit.Objectives(
                new ObjectiveDirector.Entry("first", "GET PAST THE FIRST GUARD", "One decoy holds one lure at a time. [R] hands it back.", kit.ZoneBetween("Zone_Middle", -6.5f, 6.5f), guardA),
                new ObjectiveDirector.Entry("second", "GET PAST THE SECOND GUARD", "The decoy is free again once the first lure is undone.", kit.ZoneAtExit(21f), guardB));
            return kit.Finish(PathOf("double_shift"), TitleOf("double_shift"), new GlitchDefinition[] { kit.AiOverride },
                objectives, new Vector3(-22f, 2.2f, -4f), out error);
        }

        /// <summary>
        /// FROZEN STIFF. A guard, then a piston corridor. Solution: AI Override for the guard, Freeze for the piston
        /// (while it is pulled back). Lesson: match the glitch to the problem; guards cannot be frozen.
        /// </summary>
        public static bool FrozenStiff(out string error)
        {
            var kit = LevelKit.Begin(out error, 5);
            if (kit == null) return false;
            kit.Hall(20f, 6f);
            kit.Decoy("Decoy", "DECOY_BARREL", new Vector3(-12f, 0f, 4.5f));
            kit.HeroOnRoute(V(-17f), false, V(-9f), V(-3f), V(3f), V(9f), V(17f));
            var guard = kit.GuardPost("Guard", "SKELETON_GUARD", -6f).GetComponent<GameEntity>();
            var piston = kit.Piston("Piston", "PISTON", 6f, 3f, 0f);
            kit.Exit(17f);
            var objectives = kit.Objectives(
                new ObjectiveDirector.Entry("guard", "GET PAST THE GUARD", "The guard is watching the door.", kit.ZoneBetween("Zone_Middle", -4.5f, 3.5f), guard),
                new ObjectiveDirector.Entry("exit", "GET PAST THE PISTON", "Catch it pulled back.", kit.ZoneAtExit(17f), piston));
            return kit.Finish(PathOf("frozen_stiff"), TitleOf("frozen_stiff"), new GlitchDefinition[] { kit.Freeze, kit.AiOverride },
                objectives, new Vector3(-18f, 2.2f, -4f), out error);
        }

        // ================================================================ chapter 6: Desync

        /// <summary>
        /// FALSE WALL. A wall that is only a picture: its collision is real and blocks the hero. Solution: Desync
        /// FALSE_WALL lifts the collision out of the way. Lesson: what you see is not always what is solid.
        /// </summary>
        public static bool FalseWall(out string error)
        {
            var kit = LevelKit.Begin(out error, 6);
            if (kit == null) return false;
            kit.Hall(16f, 6f);
            var wall = kit.FalseWall("FalseWall", "FALSE_WALL", 0f);
            kit.HeroOnRoute(V(-13f), false, V(-3f), V(3f), V(13f));
            kit.Exit(13f);
            var objectives = kit.Objectives(
                new ObjectiveDirector.Entry("wall", "GET PAST THE WALL", "Its collision and its looks have come apart. Pull them further apart.",
                    kit.ZoneBetween("Zone_Beyond", 1f, 11f), wall),
                new ObjectiveDirector.Entry("exit", "REACH THE EXIT", "Through the picture of a wall.", kit.ZoneAtExit(13f)));
            return kit.Finish(PathOf("false_wall"), TitleOf("false_wall"), new GlitchDefinition[] { kit.Desync },
                objectives, new Vector3(-14f, 2.2f, -4f), out error);
        }

        /// <summary>
        /// FALSE FLOOR. A pit, and a floor plate lying on this side of it. Solution: Desync MAINTENANCE_PLATE slides its
        /// collision across the pit. Lesson: the same glitch can build as well as remove.
        /// </summary>
        public static bool FalseFloor(out string error)
        {
            var kit = LevelKit.Begin(out error, 6);
            if (kit == null) return false;
            kit.Hall(18f, 6f, (-2f, 2f));
            var plate = kit.FalseFloor("FalseFloor", "MAINTENANCE_PLATE", -8f, 4.4f, 8f);
            kit.HeroOnRoute(V(-15f), true, V(-3f), V(3f), V(15f));
            kit.Exit(15f);
            var objectives = kit.Objectives(
                new ObjectiveDirector.Entry("cross", "GET THE HERO ACROSS", "The plate over there is solid. It just isn't where it looks.",
                    kit.ZoneBetween("Zone_Across", 2.5f, 13f), plate),
                new ObjectiveDirector.Entry("exit", "REACH THE EXIT", "Keep going.", kit.ZoneAtExit(15f)));
            return kit.Finish(PathOf("false_floor"), TitleOf("false_floor"), new GlitchDefinition[] { kit.Desync },
                objectives, new Vector3(-16f, 2.2f, -4f), out error);
        }

        /// <summary>
        /// SEARCHLIGHT. A false wall again, but a drone sweeps the hall and reports the anomaly to the system when it
        /// is caught in the light. Solution: Desync FALSE_WALL; Freeze blinds the drone (optional, earns the third star).
        /// Lesson: the system is watching, and containment is part of the puzzle.
        /// </summary>
        public static bool Searchlight(out string error)
        {
            var kit = LevelKit.Begin(out error, 6);
            if (kit == null) return false;
            kit.Hall(20f, 6f);
            var wall = kit.FalseWall("FalseWall", "FALSE_WALL", -2f);
            kit.Drone("Drone", "SECURITY_DRONE", 4.5f, new Vector2(2f, -3.5f), new Vector2(10f, -3.5f), new Vector2(10f, 3.5f), new Vector2(2f, 3.5f));
            kit.HeroOnRoute(V(-17f), false, V(-6f), V(2f), V(17f));
            kit.Exit(17f);
            var objectives = kit.Objectives(
                new ObjectiveDirector.Entry("wall", "GET PAST THE WALL", "The drone sweeps the room. Mind your containment.",
                    kit.ZoneBetween("Zone_Beyond", 0f, 14f), wall),
                new ObjectiveDirector.Entry("exit", "REACH THE EXIT", "Keep your containment low.", kit.ZoneAtExit(17f)));
            return kit.Finish(PathOf("searchlight"), TitleOf("searchlight"), new GlitchDefinition[] { kit.Desync, kit.Freeze },
                objectives, new Vector3(-18f, 2.2f, -4f), out error);
        }

        /// <summary>
        /// STACK THE ORDER. A long hologram bridge, and a system on edge: it undoes your OLDEST glitch a few seconds
        /// after you have two running. Solution: Freeze a crate first (a sacrifice), then Collision Transfer
        /// OLD_BARRIER → HOLO_BRIDGE; the patch eats the freeze, not the bridge. The wrong order drops the hero
        /// into the pit mid-bridge. Lesson: the system patches the oldest glitch, so order is a resource.
        /// </summary>
        public static bool StackTheOrder(out string error)
        {
            var kit = LevelKit.Begin(out error, 6);
            if (kit == null) return false;
            kit.Containment = TestRoomBuilder.LoadOrCreate<BugReport.Containment.ContainmentSettingsAsset>(
                "Assets/_Project/ScriptableObjects/Containment/CS_StackTheOrder.asset", a =>
                {
                    a.ConfigureStages(new[] { 4f, 8f, 12f, 90f }, new[] { 2f, 5f, 8f, 80f });
                    a.ConfigureResponses(new[] { 0f, 0f, 0f, 5f, 3f }, new[] { 1f, 1f, 1f, 1f, 1f });
                });
            kit.Hall(26f, 6f, (-6f, 8f));
            var barrier = kit.Barrier("OldBarrier", "OLD_BARRIER", new Vector3(-12f, 1f, 4.6f), new Vector3(3f, 2f, 0.4f), "Solid, and in nobody's way.");
            var bridge = kit.HoloBridge("HoloBridge", "HOLO_BRIDGE", -6f, 8f, 2.4f);
            kit.Crate("Crate", "CRATE", new Vector3(-16f, 0f, -3.5f), "Just a crate. Nobody will miss it.");
            kit.Exit(22f);
            kit.HeroOnRoute(V(-22f), true, V(-8f), V(10f), V(22f));
            var objectives = kit.Objectives(
                new ObjectiveDirector.Entry("bridge", "CROSS THE LONG BRIDGE",
                    "The system is on edge: with two glitches running it undoes the oldest one a few seconds later.",
                    kit.ZoneBetween("Zone_Across", 8.5f, 12f), bridge),
                new ObjectiveDirector.Entry("exit", "REACH THE EXIT", "Almost.", kit.ZoneAtExit(22f)));
            return kit.Finish(PathOf("stack_the_order"), TitleOf("stack_the_order"), new GlitchDefinition[] { kit.CollisionTransfer, kit.Freeze },
                objectives, new Vector3(-23f, 2.2f, -4f), out error);
        }

        // ================================================================ chapter 1: a fork

        /// <summary>
        /// FORK IN THE ROAD. Two lanes round a long wall; the hero takes the shortest road that is open. The south lane has
        /// one firewall, the north lane two, and there is one shadow. Solution: Collision Transfer FIREWALL_SOUTH → SHADOW_DECAL.
        /// Opening the north lane's first wall only sends the hero to the second; opening the south lane later makes him turn back.
        /// Lesson: you steer the hero by choosing which road you open.
        /// </summary>
        public static bool ForkInTheRoad(out string error)
        {
            var kit = LevelKit.Begin(out error, 1);
            if (kit == null) return false;
            kit.Hall(22f, 8f);
            kit.LaneDivider("LaneDivider", -6f, 6f, 0f);
            var south = kit.LaneWall("FirewallSouth", "FIREWALL_SOUTH", 0f, -4.1f, 3.9f);
            kit.LaneWall("FirewallNorthA", "FIREWALL_NORTH_A", -3f, 4.1f, 3.9f);
            kit.LaneWall("FirewallNorthB", "FIREWALL_NORTH_B", 3f, 4.1f, 3.9f);
            kit.Shadow("Shadow", "SHADOW_DECAL", new Vector3(-14f, 0.02f, 5f));
            kit.Exit(19f);
            kit.HeroOnGraph(false,
                new[] { new Vector3(-18f, 0f, 0f), new Vector3(-9f, 0f, 0f), new Vector3(-7f, 0f, -4f), new Vector3(7f, 0f, -4f),
                        new Vector3(9f, 0f, 0f), new Vector3(-7f, 0f, 4f), new Vector3(7f, 0f, 4f), new Vector3(19f, 0f, 0f) },
                new[] { (0, 1), (1, 2), (2, 3), (3, 4), (1, 5), (5, 6), (6, 4), (4, 7) });
            var objectives = kit.Objectives(
                new ObjectiveDirector.Entry("fork", "GET THE HERO ROUND THE WALL", "The road is the shortest one that is open. One shadow to spend.",
                    kit.ZoneBetween("Zone_Round", 4f, 8f), south),
                new ObjectiveDirector.Entry("exit", "REACH THE EXIT", "Off you go.", kit.ZoneAtExit(19f)));
            return kit.Finish(PathOf("fork_in_the_road"), TitleOf("fork_in_the_road"), new GlitchDefinition[] { kit.CollisionTransfer },
                objectives, new Vector3(-19f, 2.2f, -5f), out error);
        }

        // ================================================================ chapter 7: finale

        /// <summary>
        /// REPORT CLOSED. Every rule at once, in order: Collision Transfer over a pit, Mass Swap for a heavy gate,
        /// AI Override for a guard, Freeze for a piston, Desync for a false wall.
        /// </summary>
        public static bool ReportClosed(out string error)
        {
            var kit = LevelKit.Begin(out error, 7);
            if (kit == null) return false;
            kit.Hall(40f, 6f, (-28f, -24f));
            kit.Barrier("OldBarrier", "OLD_BARRIER", new Vector3(-32f, 1f, 4.6f), new Vector3(3f, 2f, 0.4f), "Solid, and in nobody's way.");
            var bridge = kit.HoloBridge("HoloBridge", "HOLO_BRIDGE", -28f, -24f, 2.4f);
            var plate = kit.HeavyPlate("Plate", new Vector3(-18f, 0f, -3f), 25f);
            kit.Crate("Crate", "CRATE", new Vector3(-18f, 0f, -3f), "Light. The plate wants more.");
            var anvil = kit.Weight("Anvil", "ANVIL", new Vector3(-22f, 0f, 4.2f), 60f, 1.2f, "Heavy. Obviously.");
            kit.Gate("Gate", -13f, new[] { plate });
            kit.Decoy("Decoy", "DECOY_BARREL", new Vector3(-9f, 0f, 4.5f));
            var piston = kit.Piston("Piston", "PISTON", 8f, 3f, 0f);
            var wall = kit.FalseWall("FalseWall", "FALSE_WALL", 20f);
            kit.HeroOnRoute(V(-37f), true, V(-30f), V(-22f), V(-15f), V(-11f), V(-8f), V(-2.5f), V(3f), V(13f), V(18f), V(22f), V(37f));
            var guard = kit.GuardPost("Guard", "SKELETON_GUARD", -5f).GetComponent<GameEntity>();
            kit.Exit(37f);
            var objectives = kit.Objectives(
                new ObjectiveDirector.Entry("gap", "CROSS THE GAP", "Start with the hologram.", kit.ZoneBetween("Zone_Gap", -23f, -19f), bridge),
                new ObjectiveDirector.Entry("gate", "OPEN THE GATE", "Heavy plate, light crate.", kit.ZoneBetween("Zone_Gate", -12f, -9f), anvil),
                new ObjectiveDirector.Entry("guard", "GET PAST THE GUARD", "Something in the room can draw the guard away.", kit.ZoneBetween("Zone_Guard", -3f, 3f), guard),
                new ObjectiveDirector.Entry("piston", "GET PAST THE PISTON", "Catch it pulled back.", kit.ZoneBetween("Zone_Piston", 11f, 16f), piston),
                new ObjectiveDirector.Entry("exit", "CLOSE THE REPORT", "One wall left, and it's only a picture.", kit.ZoneAtExit(37f), wall));
            return kit.Finish(PathOf("report_closed"), TitleOf("report_closed"),
                new GlitchDefinition[] { kit.CollisionTransfer, kit.MassSwap, kit.AiOverride, kit.Freeze, kit.Desync },
                objectives, new Vector3(-38f, 2.2f, -4f), out error);
        }
    }
}
