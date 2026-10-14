using System.Collections.Generic;
using BugReport.Core.Progress;
using NUnit.Framework;

namespace BugReport.Core.Tests
{
    public class ProgressTests
    {
        private static readonly IReadOnlyList<string> Levels = new[] { "lab", "bridge", "doors" };

        [Test]
        public void FirstLevelIsOpen_OthersOpenWhenThePreviousIsDone()
        {
            var save = new SaveData();
            Assert.That(LevelProgress.IsUnlocked(Levels, save, 0), Is.True);
            Assert.That(LevelProgress.IsUnlocked(Levels, save, 1), Is.False);
            save.MarkCompleted("lab");
            Assert.That(LevelProgress.IsUnlocked(Levels, save, 1), Is.True);
            Assert.That(LevelProgress.IsUnlocked(Levels, save, 2), Is.False);
            Assert.That(LevelProgress.IsUnlocked(Levels, save, 3), Is.False);
            Assert.That(LevelProgress.IsUnlocked(Levels, save, -1), Is.False);
        }

        [Test]
        public void Continue_IsTheFirstOpenUnfinishedLevel()
        {
            var save = new SaveData();
            Assert.That(LevelProgress.ContinueIndex(Levels, save), Is.EqualTo(-1), "nothing to continue yet");
            save.MarkCompleted("lab");
            Assert.That(LevelProgress.ContinueIndex(Levels, save), Is.EqualTo(1));
            save.MarkCompleted("bridge");
            save.MarkCompleted("doors");
            Assert.That(LevelProgress.ContinueIndex(Levels, save), Is.EqualTo(2), "all done: last level");
        }

        [Test]
        public void NextLevel_FollowsTheCatalogOrder()
        {
            Assert.That(LevelProgress.NextIndex(Levels, "lab"), Is.EqualTo(1));
            Assert.That(LevelProgress.NextIndex(Levels, "doors"), Is.EqualTo(-1));
            Assert.That(LevelProgress.NextIndex(Levels, "unknown"), Is.EqualTo(-1));
        }

        [Test]
        public void MarkCompleted_IsIdempotent_AndIgnoresEmptyIds()
        {
            var save = new SaveData();
            Assert.That(save.MarkCompleted("lab"), Is.True);
            Assert.That(save.MarkCompleted("lab"), Is.False);
            Assert.That(save.MarkCompleted(""), Is.False);
            Assert.That(save.completedLevels.Count, Is.EqualTo(1));
        }

        [Test]
        public void Sanitize_RepairsBrokenData()
        {
            var save = new SaveData { version = 0, completedLevels = null, settings = null };
            save.Sanitize();
            Assert.That(save.completedLevels, Is.Not.Null);
            Assert.That(save.settings, Is.Not.Null);
            Assert.That(save.version, Is.EqualTo(SaveData.CurrentVersion));

            var settings = new GameSettingsData { masterVolume = 7f, mouseSensitivity = float.NaN };
            settings.Sanitize();
            Assert.That(settings.masterVolume, Is.EqualTo(1f));
            Assert.That(settings.mouseSensitivity, Is.EqualTo(1f));
            settings.mouseSensitivity = 0f;
            settings.Sanitize();
            Assert.That(settings.mouseSensitivity, Is.EqualTo(GameSettingsData.MinSensitivity));
        }
    }
}
