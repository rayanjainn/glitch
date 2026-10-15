using System;
using System.IO;
using BugReport.Core.Progress;
using UnityEngine;

namespace BugReport.Progress
{
    /// <summary>
    /// Loads and writes the single save file (progress + settings) as JSON in
    /// Application.persistentDataPath. Writes go to a temporary file first and then replace the
    /// old one, so a crash mid-write never leaves a broken save. An unreadable file is kept aside
    /// (".corrupt") and the game starts from defaults instead of failing.
    /// </summary>
    public static class SaveStore
    {
        public const string FileName = "bugreport_save.json";

        private static SaveData current;
        private static string pathOverride;
        private static bool dirty;

        /// <summary>Raised after settings change (sliders, toggles) so scenes can apply them.</summary>
        public static event Action SettingsChanged;

        public static string FilePath => pathOverride ?? Path.Combine(Application.persistentDataPath, FileName);
        public static SaveData Current => current ?? (current = Load());
        public static GameSettingsData Settings => Current.settings;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            current = null;
            dirty = false;
            SettingsChanged = null;
        }

        /// <summary>Tests use their own file so they never touch the player's save.</summary>
        public static void RedirectForTests(string path)
        {
            pathOverride = path;
            current = null;
            dirty = false;
        }

        public static SaveData Load()
        {
            SaveData data = null;
            var path = FilePath;
            try
            {
                if (File.Exists(path)) data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[BUG REPORT] Save file unreadable, starting fresh (" + e.Message + "). Kept as " + path + ".corrupt");
                TryCopy(path, path + ".corrupt");
                data = null;
            }
            if (data == null) data = new SaveData();
            data.Sanitize();
            return data;
        }

        public static void MarkLevelCompleted(string levelId)
        {
            if (Current.MarkCompleted(levelId)) Write();
        }

        /// <summary>Records a finished run (best time, fewest uses, stars) and writes the save. Returns the run's stars.</summary>
        public static int SubmitRun(string levelId, float seconds, int glitchUses, int maxStage, int parUses)
        {
            int stars = Current.SubmitRun(levelId, seconds, glitchUses, maxStage, parUses);
            Write();
            return stars;
        }

        /// <summary>Applies changed settings now; the file is written on <see cref="Flush"/>.</summary>
        public static void ApplySettings()
        {
            Settings.Sanitize();
            dirty = true;
            SettingsChanged?.Invoke();
        }

        public static void Flush()
        {
            if (dirty) Write();
        }

        public static void Write()
        {
            var path = FilePath;
            var temp = path + ".tmp";
            try
            {
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                File.WriteAllText(temp, JsonUtility.ToJson(Current, true));
                if (File.Exists(path)) File.Delete(path);
                File.Move(temp, path);
                dirty = false;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[BUG REPORT] Could not write the save file: " + e.Message);
            }
        }

        private static void TryCopy(string from, string to)
        {
            try
            {
                File.Copy(from, to, true);
            }
            catch (Exception)
            {
                // Best effort only.
            }
        }
    }
}
