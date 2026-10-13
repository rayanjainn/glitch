using System;
using System.Collections.Generic;
using BugReport.Containment;
using BugReport.Glitches;
using BugReport.Interaction;
using BugReport.Level;
using UnityEngine;

namespace BugReport.Audio
{
    /// <summary>
    /// The game's sound language (GDD 7.5) wired to gameplay events. Sounds carry information:
    /// select chirp, invalid buzz, per-glitch activation tone, undo, patch zap, escalating
    /// containment alerts, objective chime. Event-driven only; nothing loops.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class AudioFeedback : MonoBehaviour
    {
        [SerializeField, OptionalReference] private SelectionController selection;
        [SerializeField, OptionalReference] private GlitchController glitches;
        [SerializeField, OptionalReference] private ContainmentDirector containment;
        [SerializeField, OptionalReference] private ObjectiveDirector objectives;
        [SerializeField, OptionalReference] private Tutorial.GuideDirector guide;
        [SerializeField, Range(0f, 1f)] private float volume = 0.7f;
        [SerializeField, OptionalReference, Tooltip("Recorded objective sound; the synthesized chime is used when empty.")]
        private AudioClip objectiveClip;

        private readonly Dictionary<string, AudioClip> glitchTones = new Dictionary<string, AudioClip>();
        private readonly List<AudioClip> created = new List<AudioClip>();
        private AudioSource source;
        private AudioClip select, deselect, invalid, revert, info, patch, objective, levelComplete, guideStep;
        private AudioClip[] stageAlerts;
        private GameServices services;
        private Action<GameServices> bindCallback;

        public float Volume
        {
            get => volume;
            set => volume = Mathf.Clamp01(value);
        }

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;

            select = Make(ProceduralSfx.Sweep("sfx_select", 1200f, 1800f, 0.07f, ProceduralSfx.Wave.Sine, 0.35f));
            deselect = Make(ProceduralSfx.Sweep("sfx_deselect", 1500f, 900f, 0.07f, ProceduralSfx.Wave.Sine, 0.3f));
            invalid = Make(ProceduralSfx.Sweep("sfx_invalid", 140f, 110f, 0.18f, ProceduralSfx.Wave.Square, 0.25f));
            revert = Make(ProceduralSfx.Sweep("sfx_revert", 900f, 300f, 0.22f, ProceduralSfx.Wave.Sine, 0.4f, 0.1f));
            info = Make(ProceduralSfx.Sweep("sfx_info", 700f, 700f, 0.06f, ProceduralSfx.Wave.Sine, 0.25f));
            patch = Make(ProceduralSfx.Sweep("sfx_patch", 2000f, 200f, 0.35f, ProceduralSfx.Wave.Square, 0.3f, 0.3f));
            objective = Make(ProceduralSfx.Sequence("sfx_objective", new[] { 660f, 880f }, 0.12f, ProceduralSfx.Wave.Sine));
            guideStep = Make(ProceduralSfx.Sequence("sfx_guide_step", new[] { 988f, 1319f }, 0.06f, ProceduralSfx.Wave.Sine, 0.18f));
            levelComplete = Make(ProceduralSfx.Sequence("sfx_complete", new[] { 523f, 659f, 784f, 1047f }, 0.14f, ProceduralSfx.Wave.Sine));
            stageAlerts = new[]
            {
                null,
                Make(ProceduralSfx.Sequence("sfx_stage1", new[] { 880f }, 0.15f, ProceduralSfx.Wave.Square, 0.2f)),
                Make(ProceduralSfx.Sequence("sfx_stage2", new[] { 880f, 660f }, 0.15f, ProceduralSfx.Wave.Square, 0.22f)),
                Make(ProceduralSfx.Sequence("sfx_stage3", new[] { 880f, 660f, 880f }, 0.15f, ProceduralSfx.Wave.Square, 0.25f)),
                Make(ProceduralSfx.Sequence("sfx_stage4", new[] { 990f, 660f, 990f, 660f }, 0.12f, ProceduralSfx.Wave.Square, 0.28f)),
            };
        }

        private void OnEnable()
        {
            if (selection != null) selection.Feedback += OnSelection;
            if (glitches != null) glitches.Feedback += OnGlitch;
            if (containment != null) containment.Patched += OnPatched;
            if (objectives != null)
            {
                objectives.ObjectiveCompleted += OnObjective;
                objectives.LevelCompleted += OnLevelCompleted;
            }
            if (guide != null) guide.Advanced += OnGuideAdvanced;
            bindCallback = Bind;
            GameServices.WhenReady(bindCallback);
        }

        private void OnDisable()
        {
            if (selection != null) selection.Feedback -= OnSelection;
            if (glitches != null) glitches.Feedback -= OnGlitch;
            if (containment != null) containment.Patched -= OnPatched;
            if (objectives != null)
            {
                objectives.ObjectiveCompleted -= OnObjective;
                objectives.LevelCompleted -= OnLevelCompleted;
            }
            if (guide != null) guide.Advanced -= OnGuideAdvanced;
            GameServices.CancelWhenReady(bindCallback);
            if (services != null && !services.IsDisposed) services.Containment.StageChanged -= OnStageChanged;
            services = null;
        }

        private void OnDestroy()
        {
            foreach (var clip in created)
            {
                if (clip != null) Destroy(clip);
            }
        }

        private void Bind(GameServices s)
        {
            services = s;
            s.Containment.StageChanged += OnStageChanged;
        }

        private void OnSelection(SelectionFeedback f)
        {
            switch (f.Kind)
            {
                case SelectionFeedbackKind.Selected: Play(select); break;
                case SelectionFeedbackKind.Deselected: Play(deselect); break;
                default: Play(invalid); break;
            }
        }

        private void OnGlitch(GlitchFeedback f)
        {
            switch (f.Kind)
            {
                case GlitchFeedbackKind.Applied: Play(ToneFor(glitches.CurrentDefinition)); break;
                case GlitchFeedbackKind.Reverted: Play(revert); break;
                case GlitchFeedbackKind.Rejected: Play(invalid); break;
                default: Play(info); break;
            }
        }

        private void OnPatched(Core.Glitches.GlitchOperation _) => Play(patch);
        private void OnObjective(int _) => Play(objectiveClip != null ? objectiveClip : objective);
        private void OnLevelCompleted() => Play(levelComplete);
        private void OnGuideAdvanced() => Play(guideStep);

        private void OnStageChanged(int previous, int current)
        {
            if (current > previous && current < stageAlerts.Length) Play(stageAlerts[current]);
        }

        /// <summary>Each glitch gets its own activation tone, derived from its colour (stable, distinct).</summary>
        private AudioClip ToneFor(GlitchDefinition definition)
        {
            if (definition == null) return info;
            if (glitchTones.TryGetValue(definition.Id, out var clip)) return clip;
            Color.RGBToHSV(definition.Color, out var hue, out _, out _);
            float baseHz = 300f + hue * 900f;
            clip = Make(ProceduralSfx.Sweep("sfx_glitch_" + definition.Id, baseHz * 2f, baseHz, 0.25f, ProceduralSfx.Wave.Square, 0.3f, 0.15f));
            glitchTones[definition.Id] = clip;
            return clip;
        }

        private AudioClip Make(AudioClip clip)
        {
            created.Add(clip);
            return clip;
        }

        private void Play(AudioClip clip)
        {
            if (clip != null && source != null) source.PlayOneShot(clip, volume);
        }
    }
}
