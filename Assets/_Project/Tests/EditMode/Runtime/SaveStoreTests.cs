using System.IO;
using BugReport.Progress;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace BugReport.Tests.EditMode
{
    public class SaveStoreTests
    {
        private string path;

        [SetUp]
        public void SetUp()
        {
            path = Path.Combine(Application.temporaryCachePath, "bugreport_savestore_test.json");
            foreach (var p in new[] { path, path + ".corrupt", path + ".tmp" })
                if (File.Exists(p)) File.Delete(p);
            SaveStore.RedirectForTests(path);
        }

        [TearDown]
        public void TearDown() => SaveStore.RedirectForTests(null);

        [Test]
        public void MissingFile_GivesDefaults()
        {
            var data = SaveStore.Load();
            Assert.That(data.completedLevels, Is.Empty);
            Assert.That(data.settings.masterVolume, Is.EqualTo(0.8f).Within(1e-4f));
        }

        [Test]
        public void ProgressAndSettings_SurviveAWriteAndReload()
        {
            SaveStore.MarkLevelCompleted("training_lab");
            SaveStore.Settings.invertY = true;
            SaveStore.Settings.mouseSensitivity = 2f;
            SaveStore.ApplySettings();
            SaveStore.Flush();

            SaveStore.RedirectForTests(path); // drop the cached copy
            Assert.That(SaveStore.Current.IsCompleted("training_lab"), Is.True);
            Assert.That(SaveStore.Settings.invertY, Is.True);
            Assert.That(SaveStore.Settings.mouseSensitivity, Is.EqualTo(2f));
            Assert.That(File.Exists(path + ".tmp"), Is.False, "temporary file cleaned up");
        }

        [Test]
        public void CorruptFile_IsKeptAside_AndTheGameStartsFresh()
        {
            File.WriteAllText(path, "{ this is not json");
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Save file unreadable"));
            var data = SaveStore.Load();
            Assert.That(data.completedLevels, Is.Empty);
            Assert.That(File.Exists(path + ".corrupt"), Is.True);
        }

        [Test]
        public void OutOfRangeValues_AreRepairedOnLoad()
        {
            File.WriteAllText(path, "{\"version\":1,\"completedLevels\":[\"a\",\"\"],\"settings\":{\"masterVolume\":9,\"mouseSensitivity\":-1}}");
            var data = SaveStore.Load();
            Assert.That(data.completedLevels, Is.EqualTo(new[] { "a" }));
            Assert.That(data.settings.masterVolume, Is.EqualTo(1f));
            Assert.That(data.settings.mouseSensitivity, Is.EqualTo(BugReport.Core.Progress.GameSettingsData.MinSensitivity));
        }
    }
}
