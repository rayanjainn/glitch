using System.Linq;
using BugReport.EditorTools.Setup;
using BugReport.InputHandling;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.InputSystem;

namespace BugReport.Tests.EditMode
{
    public class InputActionsAssetTests
    {
        private InputActionAsset asset;

        [SetUp]
        public void SetUp()
        {
            asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(TestRoomBuilder.InputActionsPath);
            Assert.That(asset, Is.Not.Null, "Input actions asset missing at " + TestRoomBuilder.InputActionsPath);
        }

        [Test]
        public void HasAnomalyMapWithAllActions()
        {
            var map = asset.FindActionMap(InputReader.AnomalyMapName);
            Assert.That(map, Is.Not.Null);
            foreach (var actionName in InputReader.Actions.All)
            {
                Assert.That(map.FindAction(actionName), Is.Not.Null, "missing action " + actionName);
            }
        }

        [Test]
        public void EveryActionHasKeyboardMouseAndGamepadBindings()
        {
            var map = asset.FindActionMap(InputReader.AnomalyMapName);
            foreach (var actionName in InputReader.Actions.All)
            {
                var action = map.FindAction(actionName);
                var groups = action.bindings.Where(b => !b.isComposite).Select(b => b.groups ?? string.Empty).ToList();
                Assert.That(groups.Any(g => g.Contains("Keyboard&Mouse")), Is.True, actionName + " has no Keyboard&Mouse binding");
                Assert.That(groups.Any(g => g.Contains("Gamepad")), Is.True, actionName + " has no Gamepad binding");
            }
        }

        [Test]
        public void DefinesBothControlSchemes()
        {
            var names = asset.controlSchemes.Select(s => s.name).ToList();
            Assert.That(names, Does.Contain("Keyboard&Mouse"));
            Assert.That(names, Does.Contain("Gamepad"));
        }
    }
}
