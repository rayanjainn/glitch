using System;
using BugReport.Core.Interaction;
using UnityEngine;

namespace BugReport.Presentation
{
    /// <summary>
    /// Minimal centre reticle (IMGUI placeholder). Neutral when nothing is under it,
    /// cyan for a valid target, red for an invalid one. Replaced by the final HUD
    /// in Milestone 5.
    /// </summary>
    public sealed class ReticlePresenter : MonoBehaviour
    {
        [SerializeField] private Color neutralColor = new Color(1f, 1f, 1f, 0.6f);
        [SerializeField] private Color validColor = new Color(0.25f, 0.95f, 1f, 1f);
        [SerializeField] private Color invalidColor = new Color(1f, 0.3f, 0.35f, 1f);
        [SerializeField, Min(2f)] private float size = 14f;
        [SerializeField, Min(1f)] private float thickness = 2f;

        private GameServices services;
        private Action<GameServices> bindCallback;

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

        private void OnGUI()
        {
            if (services == null || services.IsDisposed || !services.State.IsGameplayActive) return;
            if (Event.current.type != EventType.Repaint) return;

            var validation = services.Selection.HoveredValidation;
            var color = validation.Failure == TargetValidationFailure.NoTarget ? neutralColor
                : validation.IsValid ? validColor : invalidColor;

            var center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            float half = size * 0.5f;
            float gap = 3f;
            var previous = GUI.color;
            GUI.color = color;
            var tex = Texture2D.whiteTexture;
            GUI.DrawTexture(new Rect(center.x - half, center.y - thickness * 0.5f, half - gap, thickness), tex);
            GUI.DrawTexture(new Rect(center.x + gap, center.y - thickness * 0.5f, half - gap, thickness), tex);
            GUI.DrawTexture(new Rect(center.x - thickness * 0.5f, center.y - half, thickness, half - gap), tex);
            GUI.DrawTexture(new Rect(center.x - thickness * 0.5f, center.y + gap, thickness, half - gap), tex);
            GUI.color = previous;
        }
    }
}
