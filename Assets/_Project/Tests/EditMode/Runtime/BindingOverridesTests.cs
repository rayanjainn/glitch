using System.IO;
using BugReport.EditorTools.Setup;
using BugReport.InputHandling;
using BugReport.Progress;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BugReport.Tests.EditMode
{
    public class BindingOverridesTests
    {
        private InputActionAsset project;
        private InputActionAsset copy;

        [SetUp]
        public void SetUp()
        {
            var path = Path.Combine(Application.temporaryCachePath, "bugreport_bindings_test.json");
            if (File.Exists(path)) File.Delete(path);
            SaveStore.RedirectForTests(path);
            project = AssetDatabase.LoadAssetAtPath<InputActionAsset>(TestRoomBuilder.InputActionsPath);
            Assume.That(project, Is.Not.Null);
            copy = Object.Instantiate(project);
        }

        [TearDown]
        public void TearDown()
        {
            if (copy != null) Object.DestroyImmediate(copy);
            SaveStore.RedirectForTests(null);
        }

        [Test]
        public void EveryRebindableKey_ExistsInTheControlsAsset()
        {
            foreach (var r in BindingOverrides.Rebindables)
            {
                var action = BindingOverrides.FindAction(copy, r);
                Assert.That(action, Is.Not.Null, r.Label + ": action " + r.Action);
                Assert.That(BindingOverrides.BindingIndex(action, r), Is.GreaterThanOrEqualTo(0), r.Label + ": keyboard binding");
            }
        }

        [Test]
        public void Overrides_AreSaved_AndAppliedToFreshCopies_AndReset()
        {
            var r = BindingOverrides.Rebindables[9]; // UNDO LAST GLITCH
            var action = BindingOverrides.FindAction(copy, r);
            int index = BindingOverrides.BindingIndex(action, r);
            action.ApplyBindingOverride(index, "<Keyboard>/f");
            BindingOverrides.SaveFrom(copy);

            var fresh = Object.Instantiate(project);
            try
            {
                BindingOverrides.ApplyTo(fresh);
                var freshAction = BindingOverrides.FindAction(fresh, r);
                Assert.That(freshAction.bindings[index].effectivePath, Is.EqualTo("<Keyboard>/f"));
                Assert.That(project.FindActionMap(BindingOverrides.MapName).FindAction(r.Action).bindings[index].effectivePath,
                    Is.EqualTo("<Keyboard>/r"), "the project asset itself is never changed");

                BindingOverrides.ResetAll(copy);
                BindingOverrides.ApplyTo(fresh);
                Assert.That(freshAction.bindings[index].effectivePath, Is.EqualTo("<Keyboard>/r"));
                Assert.That(SaveStore.Load().bindingOverrides, Is.Empty);
            }
            finally
            {
                Object.DestroyImmediate(fresh);
            }
        }

        [Test]
        public void BrokenSavedOverrides_FallBackToDefaults()
        {
            SaveStore.Current.bindingOverrides = "{ not json";
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("key bindings could not be loaded"));
            BindingOverrides.ApplyTo(copy);
            var r = BindingOverrides.Rebindables[7]; // SELECT
            var action = BindingOverrides.FindAction(copy, r);
            Assert.That(action.bindings[BindingOverrides.BindingIndex(action, r)].effectivePath, Is.EqualTo("<Keyboard>/e"));
        }
    }
}
