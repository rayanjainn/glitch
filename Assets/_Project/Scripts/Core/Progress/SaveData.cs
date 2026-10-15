using System;
using System.Collections.Generic;

namespace BugReport.Core.Progress
{
    /// <summary>
    /// Everything persisted between sessions, as plain serializable data (no Unity references).
    /// Levels are identified by their authored id, never by build index, so reordering or adding
    /// levels never corrupts progress.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public List<string> completedLevels = new List<string>();
        public List<LevelRecord> records = new List<LevelRecord>();
        public GameSettingsData settings = new GameSettingsData();
        /// <summary>Input System binding overrides (its own JSON format); empty = default keys.</summary>
        public string bindingOverrides = string.Empty;

        public bool IsCompleted(string levelId) => completedLevels != null && completedLevels.Contains(levelId);

        /// <returns>True if this completion is new.</returns>
        public bool MarkCompleted(string levelId)
        {
            if (string.IsNullOrEmpty(levelId)) return false;
            if (completedLevels == null) completedLevels = new List<string>();
            if (completedLevels.Contains(levelId)) return false;
            completedLevels.Add(levelId);
            return true;
        }

        public LevelRecord RecordFor(string levelId)
        {
            if (records == null) return null;
            foreach (var r in records) if (r != null && r.id == levelId) return r;
            return null;
        }

        public int StarsFor(string levelId) => RecordFor(levelId)?.stars ?? (IsCompleted(levelId) ? 1 : 0);

        /// <summary>
        /// Records a finished run: marks the level complete and keeps the best time, fewest glitch
        /// uses and best stars. Returns the stars earned by this run.
        /// </summary>
        public int SubmitRun(string levelId, float seconds, int glitchUses, int maxContainmentStage, int parUses)
        {
            if (string.IsNullOrEmpty(levelId)) return 0;
            MarkCompleted(levelId);
            if (records == null) records = new List<LevelRecord>();
            var record = RecordFor(levelId);
            if (record == null)
            {
                record = new LevelRecord { id = levelId, bestSeconds = float.MaxValue, fewestUses = int.MaxValue };
                records.Add(record);
            }
            int stars = StarRating.Compute(glitchUses, parUses, maxContainmentStage);
            record.stars = Math.Max(record.stars, stars);
            if (seconds > 0f && seconds < record.bestSeconds) record.bestSeconds = seconds;
            if (glitchUses >= 0 && glitchUses < record.fewestUses) record.fewestUses = glitchUses;
            record.plays++;
            return stars;
        }

        /// <summary>Repairs anything a hand-edited or older file could contain.</summary>
        public void Sanitize()
        {
            if (records == null) records = new List<LevelRecord>();
            records.RemoveAll(r => r == null || string.IsNullOrEmpty(r.id));
            foreach (var r in records)
            {
                r.stars = Math.Max(0, Math.Min(3, r.stars));
                if (float.IsNaN(r.bestSeconds) || r.bestSeconds <= 0f) r.bestSeconds = float.MaxValue;
                if (r.fewestUses < 0) r.fewestUses = int.MaxValue;
            }
            if (completedLevels == null) completedLevels = new List<string>();
            completedLevels.RemoveAll(string.IsNullOrEmpty);
            if (settings == null) settings = new GameSettingsData();
            settings.Sanitize();
            if (bindingOverrides == null) bindingOverrides = string.Empty;
            if (version < 1) version = CurrentVersion;
        }
    }

    /// <summary>Best result per level.</summary>
    [Serializable]
    public sealed class LevelRecord
    {
        public string id;
        public int stars;
        public float bestSeconds = float.MaxValue;
        public int fewestUses = int.MaxValue;
        public int plays;
    }

    /// <summary>
    /// 1 star: finished. 2 stars: finished using no more glitches than the level's par. 3 stars: also
    /// never let containment get past stage 1 (the system barely noticed you).
    /// </summary>
    public static class StarRating
    {
        public static int Compute(int glitchUses, int parUses, int maxContainmentStage)
        {
            int stars = 1;
            if (parUses > 0 && glitchUses <= parUses)
            {
                stars = 2;
                if (maxContainmentStage <= 1) stars = 3;
            }
            return stars;
        }
    }

    /// <summary>Player settings. Ranges are enforced by <see cref="Sanitize"/>.</summary>
    [Serializable]
    public sealed class GameSettingsData
    {
        public const float MinSensitivity = 0.25f;
        public const float MaxSensitivity = 3f;

        public float masterVolume = 0.8f;
        public float mouseSensitivity = 1f;
        public bool invertY;
        public bool colourSplitEffects = true;
        public bool fullscreen = true;

        public void Sanitize()
        {
            masterVolume = Clamp(masterVolume, 0f, 1f, 0.8f);
            mouseSensitivity = Clamp(mouseSensitivity, MinSensitivity, MaxSensitivity, 1f);
        }

        private static float Clamp(float value, float min, float max, float fallback)
            => float.IsNaN(value) || float.IsInfinity(value) ? fallback : Math.Max(min, Math.Min(max, value));
    }

    /// <summary>
    /// Level order and unlocking: the first level is always open, every other level opens when the
    /// one before it is completed. "Continue" goes to the first open level not yet completed.
    /// </summary>
    public static class LevelProgress
    {
        public static bool IsUnlocked(IReadOnlyList<string> levelIds, SaveData save, int index)
        {
            if (levelIds == null || index < 0 || index >= levelIds.Count) return false;
            return index == 0 || (save != null && save.IsCompleted(levelIds[index - 1]));
        }

        /// <summary>Index to continue from, or -1 when nothing has been played yet.</summary>
        public static int ContinueIndex(IReadOnlyList<string> levelIds, SaveData save)
        {
            if (levelIds == null || levelIds.Count == 0 || save == null || save.completedLevels.Count == 0) return -1;
            for (int i = 0; i < levelIds.Count; i++)
                if (IsUnlocked(levelIds, save, i) && !save.IsCompleted(levelIds[i])) return i;
            return levelIds.Count - 1; // everything done: replay the last level
        }

        /// <summary>Index after <paramref name="levelId"/>, or -1 if it is the last (or unknown).</summary>
        public static int NextIndex(IReadOnlyList<string> levelIds, string levelId)
        {
            if (levelIds == null) return -1;
            for (int i = 0; i < levelIds.Count; i++)
                if (levelIds[i] == levelId) return i + 1 < levelIds.Count ? i + 1 : -1;
            return -1;
        }
    }
}
