#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BugReport.Core.Entities;
using BugReport.Core.GameFlow;
using BugReport.Glitches;
using BugReport.Hero;
using BugReport.Level;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace BugReport.Tests.PlayMode
{
    /// <summary>
    /// Every demo level is finished with its intended solution, through the player's workflow
    /// (equip, select source, aim, apply), from a fresh load, at 3× speed, with no errors logged.
    /// A failing test names the step that timed out. Levels that haven't been built are skipped.
    /// </summary>
    public class DemoLevelPlaythroughTests : PlaythroughBase
    {
        // ---------------------------------------------------------------- chapter 1

        [UnityTest]
        public IEnumerator Level02_BridgeOut()
        {
            yield return Load("Level_02_BridgeOut");
            yield return HeroBlocked("at the gap");
            Apply<CollisionTransferDefinition>(Named("OLD_BARRIER"), Named("HOLO_BRIDGE"));
            yield return Finished();
        }

        [UnityTest]
        public IEnumerator Level03_TwoDoors()
        {
            yield return Load("Level_03_TwoDoors");
            yield return HeroBlocked("at the first firewall");
            Apply<CollisionTransferDefinition>(Named("FIREWALL_A"), Named("SHADOW_DECAL"));
            yield return Objectives(1, "past the first firewall");
            yield return HeroBlocked("at the second firewall");
            // The shadow is still leased by the first transfer: undo it (the hero is through), then reuse it.
            controller.RevertLatest();
            Assert.That(controller.LastFeedback.Kind, Is.EqualTo(GlitchFeedbackKind.Reverted), controller.LastFeedback.Message);
            yield return GameSeconds(0.7f);
            Apply<CollisionTransferDefinition>(Named("FIREWALL_B"), Named("SHADOW_DECAL"));
            yield return Finished();
        }

        [UnityTest]
        public IEnumerator Level04_TwoBirds()
        {
            yield return Load("Level_04_TwoBirds");
            yield return HeroBlocked("at the firewall");
            Apply<CollisionTransferDefinition>(Named("FIREWALL"), Named("HOLO_BRIDGE"));
            yield return Finished();
        }

        [UnityTest]
        public IEnumerator Level04_TwoBirds_TheTrapBlocks_AndUndoRecovers()
        {
            yield return Load("Level_04_TwoBirds");
            yield return HeroBlocked("at the firewall");
            Apply<CollisionTransferDefinition>(Named("FIREWALL"), Named("HOLO_DOOR"));
            yield return Objectives(1, "past the firewall (wrong solution still opens it)");
            yield return HeroBlocked("at the gap");
            controller.RevertLatest();
            Assert.That(controller.LastFeedback.Kind, Is.EqualTo(GlitchFeedbackKind.Reverted), controller.LastFeedback.Message);
            yield return GameSeconds(0.7f);
            // The firewall is solid again, but the hero is already past it: give its solidity to the bridge.
            Apply<CollisionTransferDefinition>(Named("FIREWALL"), Named("HOLO_BRIDGE"));
            yield return Finished();
        }

        // ---------------------------------------------------------------- chapter 2

        [UnityTest]
        public IEnumerator Level05_ThreePlates()
        {
            yield return Load("Level_05_ThreePlates");
            yield return HeroBlocked("at the gate");
            var crate = Named("CRATE");
            Apply<DuplicationDefinition>(null, crate);
            yield return GameSeconds(0.7f);
            Apply<DuplicationDefinition>(null, crate);
            yield return Finished();
        }

        [UnityTest]
        public IEnumerator Level06_CopyTheCopy()
        {
            yield return Load("Level_06_CopyTheCopy");
            yield return HeroBlocked("at the gate");
            var crate = Named("CRATE");
            Apply<DuplicationDefinition>(null, crate);
            yield return GameSeconds(0.7f);
            Apply<DuplicationDefinition>(null, CopyOf(crate));
            yield return Finished();
        }

        // ---------------------------------------------------------------- chapter 3

        [UnityTest]
        public IEnumerator Level07_StopThePlatform()
        {
            yield return Load("Level_07_StopThePlatform");
            var platform = Named("SHUTTLE_PLATFORM");
            yield return WaitFor(() => hero.State == HeroState.Blocked && AtStart(Mover(platform)), 40f, "hero at the pit, platform lined up");
            Apply<FreezeDefinition>(null, platform);
            yield return Finished();
        }

        [UnityTest]
        public IEnumerator Level08_TwoPistons()
        {
            yield return Load("Level_08_TwoPistons");
            var a = Named("PISTON_A");
            var b = Named("PISTON_B");
            yield return WaitFor(() => hero.State == HeroState.Blocked && AtStart(Mover(a), 0.1f), 40f, "hero at piston A, A pulled back");
            Apply<FreezeDefinition>(null, a);
            yield return Objectives(1, "past piston A");
            yield return WaitFor(() => hero.State == HeroState.Blocked && AtStart(Mover(b), 0.1f), 40f, "hero at piston B, B pulled back");
            Apply<FreezeDefinition>(null, b);
            yield return Finished();
        }

        // ---------------------------------------------------------------- finale

        [UnityTest]
        public IEnumerator Level09_TheBuild()
        {
            yield return Load("Level_09_TheBuild");
            yield return HeroBlocked("at the firewall");
            Apply<CollisionTransferDefinition>(Named("FIREWALL"), Named("HOLO_BRIDGE"));
            yield return Objectives(1, "across the gap");
            Apply<DuplicationDefinition>(null, Named("CRATE"));
            yield return Objectives(2, "through the gate");
            var piston = Named("CRUSHER_PISTON");
            yield return WaitFor(() => hero.State == HeroState.Blocked && AtStart(Mover(piston), 0.1f), 40f, "hero at the piston, pulled back");
            Apply<FreezeDefinition>(null, piston);
            yield return Finished();
        }
    }
}
#endif
