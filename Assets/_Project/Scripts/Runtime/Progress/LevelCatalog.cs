using System;
using System.Collections.Generic;
using UnityEngine;

namespace BugReport.Progress
{
    /// <summary>
    /// The game's levels in play order. Progress is stored by <see cref="Entry.id"/>; scenes are
    /// loaded by <see cref="Entry.sceneName"/> (they must be in Build Settings, which the level
    /// builders keep in this order).
    /// </summary>
    [CreateAssetMenu(menuName = "BUG REPORT/Level Catalog", fileName = "LC_Main")]
    public sealed class LevelCatalog : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            [Tooltip("Stable id stored in save files. Never rename once shipped.")]
            public string id;
            public string sceneName;
            public string title;
            public string chapter;

            public Entry(string id, string sceneName, string title, string chapter)
            {
                this.id = id;
                this.sceneName = sceneName;
                this.title = title;
                this.chapter = chapter;
            }
        }

        [SerializeField] private Entry[] levels = new Entry[0];

        private string[] ids;

        public int Count => levels.Length;
        public Entry Get(int index) => levels[index];

        public IReadOnlyList<string> Ids
        {
            get
            {
                if (ids == null || ids.Length != levels.Length)
                {
                    ids = new string[levels.Length];
                    for (int i = 0; i < levels.Length; i++) ids[i] = levels[i].id;
                }
                return ids;
            }
        }

        public void Configure(Entry[] entries)
        {
            levels = entries ?? new Entry[0];
            ids = null;
        }

        public int IndexOfScene(string sceneName)
        {
            for (int i = 0; i < levels.Length; i++)
                if (levels[i].sceneName == sceneName) return i;
            return -1;
        }

        /// <summary>Null when valid.</summary>
        public string Validate()
        {
            var seen = new HashSet<string>();
            for (int i = 0; i < levels.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(levels[i].id)) return "level " + i + " has no id";
                if (!seen.Add(levels[i].id)) return "duplicate level id '" + levels[i].id + "'";
                if (string.IsNullOrWhiteSpace(levels[i].sceneName)) return "level " + i + " has no scene";
            }
            return null;
        }
    }
}
