using System;
using BugReport.Core.Containment;
using BugReport.Core.Glitches;
using UnityEngine;

namespace BugReport.Containment
{
    /// <summary>
    /// Feeds the level's <see cref="ContainmentModel"/> from explicit glitch events and state, and
    /// carries out the system's responses:
    /// - glitches cost more (longer cooldowns) at higher stages;
    /// - from stage 3, "patches" periodically revert the OLDEST active glitch through the same
    ///   GlitchManager API the player uses. A patch that is blocked (e.g. the hero is inside a
    ///   wall that would turn solid) moves on to the next glitch: patches never trap anyone.
    /// It never edits objects directly. Presentation listens to <see cref="Patched"/> and the model's StageChanged.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ContainmentDirector : MonoBehaviour
    {
        [SerializeField] private ContainmentSettingsAsset settings;

        private GameServices services;
        private Action<GameServices> bindCallback;
        private float patchTimer;
        private int lastStage;

        /// <summary>Raised after a patch reverted <c>operation</c>.</summary>
        public event Action<GlitchOperation> Patched;

        public ContainmentModel Model => services != null && !services.IsDisposed ? services.Containment : null;
        public float NextPatchIn => patchTimer;

        public void Configure(ContainmentSettingsAsset newSettings) => settings = newSettings;

        private bool sighted;

        /// <summary>
        /// A guard or drone has the anomaly in view this frame (adds pressure each frame it is called).
        /// Consumed by the next <see cref="Step"/>.
        /// </summary>
        public void ReportSighted() => sighted = true;

        private void OnEnable()
        {
            bindCallback = Bind;
            GameServices.WhenReady(bindCallback);
        }

        private void OnDisable()
        {
            GameServices.CancelWhenReady(bindCallback);
            if (services != null && !services.IsDisposed) services.Glitches.OperationStarted -= OnGlitchApplied;
            services = null;
        }

        private void Bind(GameServices s)
        {
            services = s;
            if (settings != null)
            {
                var problem = settings.Validate();
                if (problem != null) Debug.LogError("[BUG REPORT] Containment settings invalid (" + problem + "); using defaults.", settings);
                else s.Containment.Configure(settings.ToSettings());
            }
            s.Glitches.OperationStarted += OnGlitchApplied;
            lastStage = s.Containment.Stage;
            patchTimer = PatchInterval(lastStage);
        }

        private void OnGlitchApplied(GlitchOperation operation) => services.Containment.ReportGlitchApplied(operation.Spec.ContainmentWeight);

        private void Update()
        {
            if (services == null || services.IsDisposed || !services.State.IsGameplayActive) return;
            Step(Time.deltaTime);
        }

        /// <summary>Advances containment by <paramref name="deltaTime"/> (public for tests).</summary>
        public void Step(float deltaTime)
        {
            if (services == null || services.IsDisposed) return;
            var glitches = services.Glitches;
            var model = services.Containment;
            model.Tick(deltaTime, new ContainmentInputs(glitches.ActiveContainmentWeight, glitches.ActiveProductCount, anomalySighted: sighted));
            sighted = false;

            int stage = model.Stage;
            glitches.CooldownScale = settings != null ? settings.CooldownScale(stage) : 1f;

            float interval = PatchInterval(stage);
            if (stage != lastStage)
            {
                // Entering a patching stage gives the player one full interval of warning.
                if (stage > lastStage) patchTimer = interval;
                lastStage = stage;
            }
            if (interval <= 0f) return;

            patchTimer -= deltaTime;
            if (patchTimer > 0f) return;
            patchTimer = interval;
            TryPatch();
        }

        /// <summary>Reverts the oldest glitch that can be reverted. Returns false if none could be.</summary>
        public bool TryPatch()
        {
            if (services == null || services.IsDisposed) return false;
            var glitches = services.Glitches;
            for (int i = 0; i < glitches.ActiveCount; i++)
            {
                var operation = glitches.GetActive(i);
                if (!glitches.Revert(operation.Id, GlitchEndReason.Patched).Succeeded) continue;
                services.Containment.ReportPatched();
                Patched?.Invoke(operation);
                return true;
            }
            return false;
        }

        private float PatchInterval(int stage) => settings != null ? settings.PatchInterval(stage) : (stage >= 3 ? 6f : 0f);
    }
}
