using System;
using BugReport.Containment;
using BugReport.Core.Presentation;
using BugReport.Glitches;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BugReport.Presentation
{
    /// <summary>
    /// Short, intentional camera bursts (GDD 7.3) on a runtime copy of the global volume profile:
    /// - glitch applied → brief chromatic aberration + slight lens warp;
    /// - containment patch → red vignette pulse.
    /// Nothing runs continuously at high intensity. Chromatic/warp effects respect the player's
    /// comfort setting; everything respects <see cref="VisualSettings"/>.
    /// </summary>
    [RequireComponent(typeof(Volume))]
    public sealed class GlitchCameraFx : MonoBehaviour
    {
        [SerializeField, OptionalReference] private VisualSettings settings;
        [SerializeField] private GlitchController glitches;
        [SerializeField, OptionalReference] private ContainmentDirector containment;
        [SerializeField, OptionalReference] private Camera viewCamera;
        [SerializeField, Range(0f, 1f)] private float chromaticPeak = 0.8f;
        [SerializeField, Range(-1f, 0f)] private float warpPeak = -0.22f;
        [SerializeField, Range(0f, 12f), Tooltip("Degrees the field of view narrows at the peak of a glitch burst (a punch-in).")]
        private float fovPunch = 5f;
        [SerializeField, Range(0f, 1f)] private float patchVignettePeak = 0.5f;
        [SerializeField] private Color patchVignetteColor = new Color(0.6f, 0.02f, 0.05f);

        private readonly BurstMixer glitchBurst = new BurstMixer(new BurstEnvelope(0.04f, 0.06f, 0.3f));
        private readonly BurstMixer patchBurst = new BurstMixer(new BurstEnvelope(0.05f, 0.15f, 0.6f));
        private Volume volume;
        private VolumeProfile runtimeProfile;
        private ChromaticAberration chromatic;
        private LensDistortion warp;
        private Vignette vignette;
        private float baseVignette;
        private Color baseVignetteColor = Color.black;
        private float baseFov;

        public float CurrentChromatic => chromatic != null ? chromatic.intensity.value : 0f;
        public float CurrentVignette => vignette != null ? vignette.intensity.value : 0f;

        private void Awake()
        {
            volume = GetComponent<Volume>();
            if (viewCamera == null) viewCamera = Camera.main;
            if (viewCamera != null) baseFov = viewCamera.fieldOfView;
            bool post = VisualSettings.On(settings, s => s.postProcessing);
            volume.enabled = post;
            var cam = viewCamera != null ? viewCamera : Camera.main;
            if (cam != null) cam.GetUniversalAdditionalCameraData().renderPostProcessing = post;
            if (!post || volume.sharedProfile == null) return;

            runtimeProfile = volume.profile; // instance: runtime changes never touch the asset
            chromatic = GetOrAdd<ChromaticAberration>(runtimeProfile);
            warp = GetOrAdd<LensDistortion>(runtimeProfile);
            vignette = GetOrAdd<Vignette>(runtimeProfile);
            baseVignette = vignette.intensity.value;
            baseVignetteColor = vignette.color.value;
        }

        private static T GetOrAdd<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (!profile.TryGet<T>(out var component)) component = profile.Add<T>(true);
            component.active = true;
            return component;
        }

        private void OnEnable()
        {
            if (glitches != null) glitches.Feedback += OnGlitchFeedback;
            if (containment != null) containment.Patched += OnPatched;
        }

        private void OnDisable()
        {
            if (viewCamera != null && baseFov > 0f) viewCamera.fieldOfView = baseFov;
            if (glitches != null) glitches.Feedback -= OnGlitchFeedback;
            if (containment != null) containment.Patched -= OnPatched;
        }

        private void OnDestroy()
        {
            if (runtimeProfile != null) Destroy(runtimeProfile);
        }

        private void OnGlitchFeedback(GlitchFeedback feedback)
        {
            if (feedback.Kind == GlitchFeedbackKind.Applied) TriggerGlitchBurst(Time.unscaledTime);
        }

        private void OnPatched(Core.Glitches.GlitchOperation _) => TriggerPatchBurst(Time.unscaledTime);

        public void TriggerGlitchBurst(float now) => glitchBurst.Trigger(now);
        public void TriggerPatchBurst(float now) => patchBurst.Trigger(now);

        private void Update() => Step(Time.unscaledTime);

        /// <summary>Applies the effect values for time <paramref name="now"/> (public for tests).</summary>
        public void Step(float now)
        {
            if (runtimeProfile == null) return;
            bool bursts = VisualSettings.On(settings, s => s.glitchCameraBursts);
            bool comfortable = bursts && ComfortSettings.ChromaticEffects;

            float g = glitchBurst.Evaluate(now);
            chromatic.intensity.Override(comfortable ? g * chromaticPeak : 0f);
            warp.intensity.Override(comfortable ? g * warpPeak : 0f);

            if (viewCamera != null && baseFov > 0f) viewCamera.fieldOfView = baseFov - (comfortable ? g * fovPunch : 0f);

            float p = bursts && !ComfortSettings.ReducedFlashes ? patchBurst.Evaluate(now) : 0f;
            vignette.intensity.Override(Mathf.Lerp(baseVignette, patchVignettePeak, p));
            vignette.color.Override(Color.Lerp(baseVignetteColor, patchVignetteColor, p));
        }
    }
}
