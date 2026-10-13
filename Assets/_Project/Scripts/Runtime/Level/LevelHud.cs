using System;
using BugReport.Core.GameFlow;
using BugReport.Glitches;
using BugReport.Presentation;
using UnityEngine;

namespace BugReport.Level
{
    /// <summary>
    /// Minimal in-game HUD (IMGUI placeholder until the UI pass): current objective + hint at
    /// top-left, equipped glitch with cooldown and a contextual prompt at the bottom. Kept small
    /// during normal play (GDD 9.2).
    /// </summary>
    public sealed class LevelHud : MonoBehaviour
    {
        [SerializeField, OptionalReference] private ObjectiveDirector objectives;
        [SerializeField] private GlitchController glitches;
        [SerializeField, Min(0.5f)] private float completeFlashSeconds = 2.5f;
        [SerializeField, OptionalReference] private VisualSettings settings;

        private GameServices services;
        private Action<GameServices> bindCallback;
        private GUIStyle small;
        private GUIStyle big;
        private GUIStyle center;
        private float flashUntil;
        private string flashText;

        private void OnEnable()
        {
            bindCallback = s => services = s;
            GameServices.WhenReady(bindCallback);
            if (objectives != null) objectives.ObjectiveCompleted += OnObjectiveCompleted;
        }

        private void OnDisable()
        {
            GameServices.CancelWhenReady(bindCallback);
            if (objectives != null) objectives.ObjectiveCompleted -= OnObjectiveCompleted;
            services = null;
        }

        private void OnObjectiveCompleted(int index)
        {
            flashText = "OBJECTIVE COMPLETE: " + objectives.Get(index).title;
            flashUntil = Time.unscaledTime + completeFlashSeconds;
        }

        private void OnGUI()
        {
            if (services == null || services.IsDisposed) return;
            if (services.State.Current != GameState.Playing && services.State.Current != GameState.Inspection) return;
            EnsureStyles();

            if (objectives != null && objectives.Tracker != null && !objectives.Tracker.AllComplete)
            {
                int i = objectives.Tracker.CurrentIndex;
                var entry = objectives.Get(i);
                if (VisualSettings.On(settings, x => x.hudPanels))
                    WorldLabel.Panel(new Rect(8, Screen.height - 146, 640, 106), new Color(0.02f, 0.05f, 0.08f, 0.6f));
                GUI.Label(new Rect(16, Screen.height - 140, 520, 24), "OBJECTIVE " + (i + 1) + "/" + objectives.Count, small);
                GUI.Label(new Rect(16, Screen.height - 118, 620, 28), entry.title, big);
                GUI.Label(new Rect(16, Screen.height - 88, 620, 40), entry.hint, small);
            }

            if (Time.unscaledTime < flashUntil)
                GUI.Label(new Rect(0, Screen.height * 0.22f, Screen.width, 32), flashText, center);

            if (glitches != null && glitches.CurrentDefinition != null)
            {
                var def = glitches.CurrentDefinition;
                if (VisualSettings.On(settings, x => x.hudPanels))
                    WorldLabel.Panel(new Rect(Screen.width * 0.5f - 230f, Screen.height - 70, 460f, 62f), new Color(0.02f, 0.05f, 0.08f, 0.6f));
                float cooldown = services.Glitches.CooldownRemaining(def.Id, Time.time);
                var previous = GUI.color;
                GUI.color = def.Color;
                string line = "[ " + def.DisplayName + " ]" + (cooldown > 0f ? "  " + cooldown.ToString("0.0") + "s" : string.Empty);
                GUI.Label(new Rect(0, Screen.height - 64, Screen.width, 28), line, center);
                GUI.color = previous;

                string prompt = glitches.HasPreview
                    ? (glitches.Preview.Succeeded ? "[LMB] APPLY   [R] UNDO   [Q] SWITCH" : glitches.Preview.Describe())
                    : (glitches.Prompt ?? "[E] SELECT   [Q] SWITCH GLITCH");
                GUI.Label(new Rect(0, Screen.height - 36, Screen.width, 24), prompt, center);
            }
        }

        private void EnsureStyles()
        {
            if (small != null) return;
            small = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
            small.normal.textColor = new Color(0.8f, 0.92f, 1f);
            big = new GUIStyle(GUI.skin.label) { fontSize = 19, fontStyle = FontStyle.Bold };
            big.normal.textColor = Color.white;
            center = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            center.normal.textColor = Color.white;
        }
    }
}
