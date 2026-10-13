using System;
using System.Collections.Generic;
using BugReport.Core.GameFlow;
using BugReport.Core.Objectives;
using UnityEngine;

namespace BugReport.Level
{
    /// <summary>
    /// Owns the level's objective chain: authored entries (title, hint, condition) evaluated
    /// every frame through the core <see cref="ObjectiveTracker"/>. When the last objective
    /// completes the game enters the Ending state (level complete). Objectives describe what to
    /// achieve, not how, so alternative solutions count automatically.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ObjectiveDirector : MonoBehaviour
    {
        [Serializable]
        public struct Entry
        {
            public string id;
            public string title;
            [TextArea(1, 3)] public string hint;
            public ObjectiveCondition condition;

            public Entry(string id, string title, string hint, ObjectiveCondition condition)
            {
                this.id = id;
                this.title = title;
                this.hint = hint;
                this.condition = condition;
            }
        }

        [SerializeField] private Entry[] objectives = new Entry[0];

        private ObjectiveTracker tracker;
        private GameServices services;
        private Action<GameServices> bindCallback;

        public event Action<int> ObjectiveCompleted;
        public event Action LevelCompleted;

        public ObjectiveTracker Tracker => tracker;
        public int Count => objectives.Length;
        public Entry Get(int index) => objectives[index];
        public float CompletedTime { get; private set; } = -1f;

        public void Configure(Entry[] entries)
        {
            objectives = entries ?? new Entry[0];
            tracker = null;
            if (Application.isPlaying) BuildTracker();
        }

        private void Awake() => BuildTracker();

        private void BuildTracker()
        {
            if (objectives.Length == 0)
            {
                Debug.LogError("[BUG REPORT] ObjectiveDirector has no objectives.", this);
                return;
            }
            var ids = new List<string>();
            foreach (var e in objectives) ids.Add(e.id);
            tracker = new ObjectiveTracker(ids);
            tracker.Completed += i => ObjectiveCompleted?.Invoke(i);
            tracker.AllCompleted += OnAllCompleted;
        }

        private void OnEnable()
        {
            bindCallback = s => services = s;
            GameServices.WhenReady(bindCallback);
        }

        private void OnDisable()
        {
            GameServices.CancelWhenReady(bindCallback);
            services = null;
        }

        private void Update()
        {
            if (tracker == null || tracker.AllComplete) return;
            if (services != null && !services.IsDisposed && services.State.Current == GameState.Paused) return;
            Evaluate();
        }

        /// <summary>Evaluates conditions now. Public for tests.</summary>
        public void Evaluate()
        {
            tracker?.Evaluate(i => objectives[i].condition != null && objectives[i].condition.IsMet);
        }

        private void OnAllCompleted()
        {
            CompletedTime = Time.unscaledTime;
            if (services != null && !services.IsDisposed) services.State.TryTransition(GameState.Ending);
            LevelCompleted?.Invoke();
        }
    }
}
