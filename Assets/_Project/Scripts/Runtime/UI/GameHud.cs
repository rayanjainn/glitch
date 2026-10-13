using System.Collections.Generic;
using BugReport.Containment;
using BugReport.Core.GameFlow;
using BugReport.Core.Interaction;
using BugReport.Core.Presentation;
using BugReport.Glitches;
using BugReport.Interaction;
using BugReport.Level;
using BugReport.Presentation;
using UnityEngine;
using UnityEngine.UIElements;

namespace BugReport.UI
{
    /// <summary>
    /// In-game HUD (GDD 9.2), kept small during normal play: containment bar and stage banner
    /// (top), reticle and short feedback messages (centre), current objective (bottom-left),
    /// glitch dock with cooldowns and the contextual prompt (bottom). Hidden outside gameplay.
    /// </summary>
    public sealed class GameHud : UiLayer
    {
        [SerializeField] private GlitchController glitches;
        [SerializeField] private SelectionController selection;
        [SerializeField] private ContainmentDirector containment;
        [SerializeField, OptionalReference] private ObjectiveDirector objectives;
        [SerializeField, OptionalReference] private VisualSettings settings;
        [SerializeField, Min(0.5f)] private float objectiveBannerSeconds = 2.5f;
        [SerializeField, Min(0.1f)] private float stageFlashSeconds = 1.2f;

        private readonly ToastQueue toasts = new ToastQueue();
        private readonly List<Toast> visibleToasts = new List<Toast>();
        private readonly List<Label> toastLabels = new List<Label>();
        private readonly List<VisualElement> slots = new List<VisualElement>();

        private VisualElement alert;
        private Label stageLabel;
        private VisualElement pressureFill;
        private Label alertDetail;
        private VisualElement reticle;
        private VisualElement toastStack;
        private Label banner;
        private VisualElement objectivePanel;
        private Label objectiveHeader;
        private Label objectiveTitle;
        private Label objectiveHint;
        private VisualElement dock;
        private VisualElement slotRow;
        private Label prompt;

        private int shownStage;
        private float stageFlashUntil;
        private float bannerUntil;
        private string bannerText = string.Empty;
        private bool subscribed;

        protected override string LayerName => "hud";
        protected override int SortKey => 10;

        public void Configure(GlitchController glitchController, SelectionController selectionController,
            ContainmentDirector containmentDirector, ObjectiveDirector objectiveDirector, VisualSettings visualSettings)
        {
            glitches = glitchController;
            selection = selectionController;
            containment = containmentDirector;
            objectives = objectiveDirector;
            settings = visualSettings;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (subscribed) return;
            if (glitches != null) glitches.Feedback += OnGlitchFeedback;
            if (selection != null) selection.Feedback += OnSelectionFeedback;
            if (objectives != null) objectives.ObjectiveCompleted += OnObjectiveCompleted;
            subscribed = true;
        }

        protected override void OnDisable()
        {
            if (subscribed)
            {
                if (glitches != null) glitches.Feedback -= OnGlitchFeedback;
                if (selection != null) selection.Feedback -= OnSelectionFeedback;
                if (objectives != null) objectives.ObjectiveCompleted -= OnObjectiveCompleted;
                subscribed = false;
            }
            toasts.Clear();
            slots.Clear();
            toastLabels.Clear();
            base.OnDisable();
        }

        // ---------------------------------------------------------------- build

        protected override void Build(VisualElement layer)
        {
            alert = Ui.Add(layer, Ui.Element("alert", "br-alert"));
            stageLabel = Ui.Add(alert, Ui.Text("alert-stage", string.Empty, "br-alert__stage"));
            var track = Ui.Add(alert, Ui.Element("alert-track", "br-alert__track"));
            pressureFill = Ui.Add(track, Ui.Element("alert-fill", "br-alert__fill"));
            alertDetail = Ui.Add(alert, Ui.Text("alert-detail", string.Empty, "br-alert__detail"));

            reticle = Ui.Add(layer, Ui.Element("reticle", "br-reticle"));
            foreach (var side in new[] { "left", "right", "up", "down" })
                reticle.Add(Ui.Element("reticle-" + side, "br-reticle__tick", "br-reticle__tick--" + side));
            reticle.Add(Ui.Element("reticle-dot", "br-reticle__dot"));

            toastStack = Ui.Add(layer, Ui.Element("toasts", "br-toasts"));
            banner = Ui.Add(layer, Ui.Text("banner", string.Empty, "br-banner"));

            objectivePanel = Ui.Add(layer, Ui.Element("objective", "br-panel", "br-objective"));
            objectiveHeader = Ui.Add(objectivePanel, Ui.Text("objective-header", string.Empty, "br-caption"));
            objectiveTitle = Ui.Add(objectivePanel, Ui.Text("objective-title", string.Empty, "br-objective__title"));
            objectiveHint = Ui.Add(objectivePanel, Ui.Text("objective-hint", string.Empty, "br-objective__hint"));

            dock = Ui.Add(layer, Ui.Element("dock", "br-dock"));
            var row = Ui.Add(dock, Ui.Element("dock-row", "br-dock__row"));
            row.Add(Ui.Text("dock-key", "Q", "br-key"));
            slotRow = Ui.Add(row, Ui.Element("dock-slots", "br-dock__slots"));
            prompt = Ui.Add(dock, Ui.Text("prompt", string.Empty, "br-prompt"));

            slots.Clear();
            toastLabels.Clear();
            shownStage = 0;
        }

        // ---------------------------------------------------------------- refresh

        protected override void Refresh()
        {
            var state = HasServices ? Services.State.Current : GameState.Boot;
            bool gameplay = state == GameState.Playing || state == GameState.Inspection;
            Ui.Show(Layer, gameplay);
            if (!gameplay) return;

            Layer.EnableInClassList("br-hud--no-panels", !VisualSettings.On(settings, s => s.hudPanels));
            Layer.EnableInClassList("br-hud--inspecting", state == GameState.Inspection);
            float now = Time.unscaledTime;
            RefreshAlert(now);
            RefreshReticle();
            RefreshToasts(now);
            RefreshObjective(now);
            RefreshDock();
        }

        private void RefreshAlert(float now)
        {
            var model = containment != null ? containment.Model : null;
            if (model == null)
            {
                Ui.Show(alert, false);
                return;
            }

            if (model.Stage > shownStage) stageFlashUntil = now + stageFlashSeconds;
            if (model.Stage != shownStage || stageLabel.text.Length == 0)
                Ui.SetText(stageLabel, "// " + HudFormat.StageName(model.Stage) + " //");
            shownStage = model.Stage;

            Ui.Show(alert, model.Pressure > 0f || model.Stage > 0);
            pressureFill.style.width = Length.Percent(Mathf.Clamp01(model.Pressure01) * 100f);
            for (int i = 0; i < HudFormat.StageCount; i++) alert.EnableInClassList("br-alert--stage" + i, model.Stage == i);

            Ui.Show(stageLabel, model.Stage > 0);
            bool flashing = now < stageFlashUntil && Mathf.Repeat(now * 6f, 1f) < 0.5f;
            alert.EnableInClassList("br-alert--flash", flashing);

            string detail = HudFormat.ContainmentDetail(model.Stage, containment.NextPatchIn);
            Ui.Show(alertDetail, detail.Length > 0);
            Ui.SetText(alertDetail, detail);
        }

        private void RefreshReticle()
        {
            Ui.Show(reticle, Services.State.IsGameplayActive);
            var validation = Services.Selection.HoveredValidation;
            string tone;
            if (validation.Failure == TargetValidationFailure.NoTarget) tone = "neutral";
            else if (glitches != null && glitches.HasPreview) tone = glitches.Preview.Succeeded ? "valid" : "invalid";
            else tone = validation.IsValid ? "valid" : "invalid";
            reticle.EnableInClassList("br-reticle--neutral", tone == "neutral");
            reticle.EnableInClassList("br-reticle--valid", tone == "valid");
            reticle.EnableInClassList("br-reticle--invalid", tone == "invalid");
        }

        private void RefreshToasts(float now)
        {
            toasts.Visible(now, visibleToasts);
            while (toastLabels.Count < visibleToasts.Count)
                toastLabels.Add(Ui.Add(toastStack, Ui.Text("toast-" + toastLabels.Count, string.Empty, "br-toast")));

            for (int i = 0; i < toastLabels.Count; i++)
            {
                var label = toastLabels[i];
                bool on = i < visibleToasts.Count;
                Ui.Show(label, on);
                if (!on) continue;
                var toast = visibleToasts[i];
                Ui.SetText(label, toast.Count > 1 ? toast.Message + "  x" + toast.Count : toast.Message);
                label.style.color = Ui.Unpack(toast.Color);
                label.style.opacity = toasts.Opacity(toast, now);
                label.EnableInClassList("br-toast--negative", toast.Tone == ToastTone.Negative);
                label.EnableInClassList("br-toast--positive", toast.Tone == ToastTone.Positive);
            }
        }

        private void RefreshObjective(float now)
        {
            Ui.Show(banner, now < bannerUntil);
            Ui.SetText(banner, bannerText);

            var tracker = objectives != null ? objectives.Tracker : null;
            bool show = tracker != null && !tracker.AllComplete && objectives.Count > 0;
            Ui.Show(objectivePanel, show);
            if (!show) return;
            int i = tracker.CurrentIndex;
            var entry = objectives.Get(i);
            Ui.SetText(objectiveHeader, HudFormat.ObjectiveHeader(i, objectives.Count));
            Ui.SetText(objectiveTitle, entry.title);
            Ui.SetText(objectiveHint, entry.hint);
            Ui.Show(objectiveHint, !string.IsNullOrEmpty(entry.hint));
        }

        private void RefreshDock()
        {
            int count = glitches != null ? glitches.LoadedCount : 0;
            Ui.Show(dock, count > 0);
            if (count == 0) return;

            if (slots.Count != count) BuildSlots(count);
            float time = Time.time;
            for (int i = 0; i < count; i++)
            {
                var def = glitches.DefinitionAt(i);
                var slot = slots[i];
                float cooldown = Services.Glitches.CooldownRemaining(def.Id, time);
                bool active = i == glitches.CurrentIndex;
                slot.EnableInClassList("br-slot--active", active);
                slot.EnableInClassList("br-slot--cooling", cooldown > 0f);
                slot.style.borderBottomColor = active ? def.Color : new Color(def.Color.r, def.Color.g, def.Color.b, 0.25f);
                slot.Q("slot-swatch").style.backgroundColor = def.Color;
                Ui.SetText(slot.Q<Label>("slot-name"), def.DisplayName);
                var timer = slot.Q<Label>("slot-timer");
                Ui.SetText(timer, HudFormat.Seconds(cooldown));
                Ui.Show(timer, cooldown > 0f);
            }

            string line = glitches.HasPreview
                ? (glitches.Preview.Succeeded ? "[LMB] APPLY    [R] UNDO" : glitches.Preview.Describe())
                : (glitches.Prompt ?? "[E] SELECT    [TAB] INSPECT");
            Ui.SetText(prompt, line);
            prompt.EnableInClassList("br-prompt--invalid", glitches.HasPreview && !glitches.Preview.Succeeded);
        }

        private void BuildSlots(int count)
        {
            slotRow.Clear();
            slots.Clear();
            for (int i = 0; i < count; i++)
            {
                var slot = Ui.Add(slotRow, Ui.Element("slot-" + i, "br-slot"));
                slot.Add(Ui.Element("slot-swatch", "br-slot__swatch"));
                slot.Add(Ui.Text("slot-name", string.Empty, "br-slot__name"));
                slot.Add(Ui.Text("slot-timer", string.Empty, "br-slot__timer"));
                slots.Add(slot);
            }
        }

        // ---------------------------------------------------------------- events

        private void OnGlitchFeedback(GlitchFeedback feedback)
        {
            var tone = feedback.Kind == GlitchFeedbackKind.Rejected ? ToastTone.Negative
                : feedback.Kind == GlitchFeedbackKind.Applied ? ToastTone.Positive
                : ToastTone.Neutral;
            toasts.Push(feedback.Message, tone, Ui.Pack(feedback.Color), Time.unscaledTime);
        }

        private void OnSelectionFeedback(SelectionFeedback feedback)
        {
            // Selecting is already visible on the object and in the inspection panel; only explain refusals.
            if (feedback.Kind != SelectionFeedbackKind.Rejected) return;
            toasts.Push(feedback.Message, ToastTone.Negative, Ui.Pack(new Color(1f, 0.3f, 0.35f)), Time.unscaledTime);
        }

        private void OnObjectiveCompleted(int index)
        {
            bannerText = "OBJECTIVE COMPLETE: " + objectives.Get(index).title;
            bannerUntil = Time.unscaledTime + objectiveBannerSeconds;
        }

        /// <summary>Messages currently on screen, newest first (tests).</summary>
        public IReadOnlyList<Toast> VisibleToasts => visibleToasts;
    }
}
