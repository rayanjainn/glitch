#if UNITY_EDITOR
using System.Collections;
using BugReport.Core.Glitches;
using BugReport.Glitches;
using BugReport.Guards;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace BugReport.Tests.PlayMode
{
    /// <summary>
    /// Chapters 4–7 finished with their intended solutions through the player's workflow (see
    /// FullGameLevels for each level's written solution). Levels that haven't been built are skipped.
    /// </summary>
    public class FullGamePlaythroughTests : PlaythroughBase
    {
        // ---------------------------------------------------------------- chapter 4: Property Swap

        [UnityTest]
        public IEnumerator Level10_HeavyLifting()
        {
            yield return Load("Level_10_HeavyLifting");
            yield return HeroBlocked("at the gate");
            Apply<PropertySwapDefinition>(Named("CRATE"), Named("ANVIL"));
            yield return Finished();
        }

        [UnityTest]
        public IEnumerator Level11_TwinScales()
        {
            yield return Load("Level_11_TwinScales");
            yield return HeroBlocked("at the gate");
            var crate = Named("CRATE");
            Apply<DuplicationDefinition>(null, crate);
            yield return GameSeconds(0.7f);
            var copy = CopyOf(crate);
            Apply<PropertySwapDefinition>(crate, Named("ANVIL"));
            yield return GameSeconds(0.7f);
            Apply<PropertySwapDefinition>(copy, Named("BOULDER"));
            yield return Finished();
        }

        [UnityTest]
        public IEnumerator Level12_WeighStation()
        {
            yield return Load("Level_12_WeighStation");
            yield return HeroBlocked("at the gate");
            Apply<PropertySwapDefinition>(Named("CRATE"), Named("ANVIL"));
            yield return Objectives(1, "through the gate");
            yield return HeroBlocked("at the gap");
            Apply<CollisionTransferDefinition>(Named("OLD_BARRIER"), Named("HOLO_BRIDGE"));
            yield return Finished();
        }

        // ---------------------------------------------------------------- chapter 5: AI Override

        [UnityTest]
        public IEnumerator Level13_NightWatch()
        {
            yield return Load("Level_13_NightWatch");
            yield return HeroBlocked("at the guard");
            Apply<AiOverrideDefinition>(Named("SKELETON_GUARD"), Named("DECOY_BARREL"));
            yield return Finished();
        }

        [UnityTest]
        public IEnumerator Level14_DoubleShift()
        {
            yield return Load("Level_14_DoubleShift");
            yield return HeroBlocked("at the first guard");
            Apply<AiOverrideDefinition>(Named("GUARD_A"), Named("DECOY_BARREL"));
            yield return Objectives(1, "past the first guard");

            // The decoy holds one lure at a time: a second one is refused until the first is undone.
            yield return HeroBlocked("at the second guard");
            services.Selection.ClearSelection();
            Assert.That(services.Selection.TrySelect(Named("GUARD_B").Id).IsValid, Is.True);
            services.Selection.SetHovered(Named("DECOY_BARREL").Id);
            controller.ApplyCurrent();
            Assert.That(controller.LastFeedback.Kind, Is.Not.EqualTo(GlitchFeedbackKind.Applied), "decoy already in use");
            controller.RevertLatest();
            yield return GameSeconds(0.7f);
            Apply<AiOverrideDefinition>(Named("GUARD_B"), Named("DECOY_BARREL"));
            yield return Finished();
        }

        [UnityTest]
        public IEnumerator Level15_FrozenStiff()
        {
            yield return Load("Level_15_FrozenStiff");
            yield return HeroBlocked("at the guard");
            Apply<AiOverrideDefinition>(Named("SKELETON_GUARD"), Named("DECOY_BARREL"));
            yield return Objectives(1, "past the guard");
            var piston = Named("PISTON");
            yield return WaitFor(() => hero.State == BugReport.Hero.HeroState.Blocked && AtStart(Mover(piston), 0.1f), 40f, "hero at the piston, pulled back");
            Apply<FreezeDefinition>(null, piston);
            yield return Finished();
        }

        // ---------------------------------------------------------------- chapter 6: Desync

        [UnityTest]
        public IEnumerator Level16_FalseWall()
        {
            yield return Load("Level_16_FalseWall");
            yield return HeroBlocked("at the wall");
            Apply<DesyncDefinition>(null, Named("FALSE_WALL"));
            yield return Finished();
        }

        [UnityTest]
        public IEnumerator Level17_FalseFloor()
        {
            yield return Load("Level_17_FalseFloor");
            yield return HeroBlocked("at the pit");
            Apply<DesyncDefinition>(null, Named("MAINTENANCE_PLATE"));
            yield return Finished();
        }

        [UnityTest]
        public IEnumerator Level18_Searchlight()
        {
            yield return Load("Level_18_Searchlight");
            yield return HeroBlocked("at the wall");
            Apply<DesyncDefinition>(null, Named("FALSE_WALL"));
            yield return Finished();
        }

        [UnityTest]
        public IEnumerator Level19_StackTheOrder()
        {
            yield return Load("Level_19_StackTheOrder");
            yield return HeroBlocked("at the pit");
            // The sacrifice first: the system patches the oldest glitch, so the bridge must not be it.
            Apply<FreezeDefinition>(null, Named("CRATE"));
            yield return GameSeconds(0.7f);
            Apply<CollisionTransferDefinition>(Named("OLD_BARRIER"), Named("HOLO_BRIDGE"));
            yield return Finished();
        }

        [UnityTest]
        public IEnumerator Level19_StackTheOrder_WrongOrderDropsTheHero()
        {
            yield return Load("Level_19_StackTheOrder");
            yield return HeroBlocked("at the pit");
            Apply<CollisionTransferDefinition>(Named("OLD_BARRIER"), Named("HOLO_BRIDGE"));
            yield return GameSeconds(0.7f);
            Apply<FreezeDefinition>(null, Named("CRATE"));
            yield return WaitFor(() => hero.RespawnCount >= 1, 30f, "the system patching the bridge under the hero");
        }

        [UnityTest]
        public IEnumerator Level21_ForkInTheRoad()
        {
            yield return Load("Level_21_ForkInTheRoad");
            yield return HeroBlocked("at the first firewall");
            Apply<CollisionTransferDefinition>(Named("FIREWALL_SOUTH"), Named("SHADOW_DECAL"));
            yield return Finished();
        }

        // ---------------------------------------------------------------- chapter 7: finale

        [UnityTest]
        public IEnumerator Level20_ReportClosed()
        {
            yield return Load("Level_20_ReportClosed");
            yield return HeroBlocked("at the gap");
            Apply<CollisionTransferDefinition>(Named("OLD_BARRIER"), Named("HOLO_BRIDGE"));
            yield return Objectives(1, "across the gap");
            yield return HeroBlocked("at the gate");
            Apply<PropertySwapDefinition>(Named("CRATE"), Named("ANVIL"));
            yield return Objectives(2, "through the gate");
            yield return HeroBlocked("at the guard");
            Apply<AiOverrideDefinition>(Named("SKELETON_GUARD"), Named("DECOY_BARREL"));
            yield return Objectives(3, "past the guard");
            var piston = Named("PISTON");
            yield return WaitFor(() => hero.State == BugReport.Hero.HeroState.Blocked && AtStart(Mover(piston), 0.1f), 40f, "hero at the piston, pulled back");
            Apply<FreezeDefinition>(null, piston);
            yield return Objectives(4, "past the piston");
            yield return HeroBlocked("at the wall");
            Apply<DesyncDefinition>(null, Named("FALSE_WALL"));
            yield return Finished();
        }
    }
}
#endif
