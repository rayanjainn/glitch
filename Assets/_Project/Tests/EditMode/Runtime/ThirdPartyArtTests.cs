using System.IO;
using System.Linq;
using BugReport.EditorTools.Art;
using BugReport.EditorTools.Setup;
using BugReport.Hero;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace BugReport.Tests.EditMode
{
    /// <summary>
    /// Third-party CC0 art: licences present, import rules applied, hero animation set up.
    /// Tests that need the binary files are ignored until Tools/Assets/fetch-cc0-assets.sh has run.
    /// </summary>
    public class ThirdPartyArtTests
    {
        private const string Root = "Assets/ThirdParty";

        [Test]
        public void EveryPack_HasItsLicenceFile_AndIsCredited()
        {
            if (!Directory.Exists(Root)) Assert.Ignore("No third-party assets yet.");
            var credits = File.Exists("CREDITS.md") ? File.ReadAllText("CREDITS.md") : string.Empty;
            foreach (var pack in Directory.GetDirectories(Root).SelectMany(d => d.EndsWith("Fonts") ? Directory.GetDirectories(d) : new[] { d }))
            {
                bool licence = Directory.GetFiles(pack, "*.txt").Any(f =>
                {
                    var n = Path.GetFileName(f).ToLowerInvariant();
                    return n.StartsWith("license") || n.StartsWith("licence") || n.StartsWith("ofl");
                });
                Assert.That(licence, Is.True, pack + " has no licence file");
            }
            StringAssert.Contains("KayKit", credits);
            StringAssert.Contains("Kenney", credits);
            StringAssert.Contains("Lilita One", credits);
        }

        [Test]
        public void Knight_ImportsAsGenericRig_WithLoopingWalkAndIdle()
        {
            if (!ArtBuilder.HeroAvailable) Assert.Ignore("Run Tools/Assets/fetch-cc0-assets.sh first.");
            var importer = (ModelImporter)AssetImporter.GetAtPath(ArtBuilder.KnightPath);
            Assert.That(importer.animationType, Is.EqualTo(ModelImporterAnimationType.Generic));
            Assert.That(importer.materialImportMode, Is.EqualTo(ModelImporterMaterialImportMode.None));
            var clips = importer.clipAnimations;
            foreach (var needed in ArtBuilder.HeroClips)
                Assert.That(clips.Any(c => ThirdPartyModelImport.BaseName(c.name) == needed), Is.True, "missing clip " + needed);
            Assert.That(clips.First(c => ThirdPartyModelImport.BaseName(c.name) == "Idle").loopTime, Is.True);
            Assert.That(clips.First(c => ThirdPartyModelImport.BaseName(c.name) == "Walking_A").loopTime, Is.True);
        }

        [Test]
        public void HeroController_HasOneStatePerHeroState()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ArtBuilder.HeroControllerPath);
            if (controller == null) Assert.Ignore("Not built yet: run a level builder after fetching the art.");
            var states = controller.layers[0].stateMachine.states.Select(s => s.state.name).ToArray();
            Assert.That(states, Is.EquivalentTo(System.Enum.GetNames(typeof(HeroState))), "HeroAnimator sets State = (int)HeroState");
            Assert.That(controller.parameters.Select(p => p.name), Is.SupersetOf(new[] { "State", "WalkSpeed" }));
        }

        [Test]
        public void CratePrefab_UsesTheDungeonModel_WhenAvailable()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TestRoomBuilder.CratePrefabPath);
            if (prefab == null || !ArtBuilder.DungeonAvailable) Assert.Ignore("Not built with the CC0 art yet.");
            Assert.That(prefab.transform.Find("Model"), Is.Not.Null);
            Assert.That(prefab.GetComponent<MeshRenderer>(), Is.Null, "the placeholder cube mesh is replaced, not doubled");
            Assert.That(prefab.GetComponent<BoxCollider>(), Is.Not.Null, "gameplay collider stays on the root");
        }
    }
}
