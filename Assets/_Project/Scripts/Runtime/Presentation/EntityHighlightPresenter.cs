using System;
using System.Collections.Generic;
using BugReport.Core.Entities;
using BugReport.Core.GameFlow;
using BugReport.Glitches;
using UnityEngine;

namespace BugReport.Presentation
{
    /// <summary>
    /// Placeholder targeting feedback: wireframe bounding boxes.
    /// - Inspection mode: every inspectable entity gets a faint outline.
    /// - Hovered entity: white if valid, red if not.
    /// - Selected entity: pulsing cyan.
    /// Reads state only; never changes gameplay. Final outline shaders replace
    /// this in the art pass (Milestone 6).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EntityHighlightPresenter : MonoBehaviour
    {
        [SerializeField] private Material lineMaterial;
        [SerializeField, Min(0.001f)] private float lineWidth = 0.025f;
        [SerializeField, Min(0f)] private float padding = 0.04f;
        [SerializeField] private Color inspectColor = new Color(0.35f, 0.9f, 1f, 0.45f);
        [SerializeField] private Color hoverValidColor = new Color(1f, 1f, 1f, 0.95f);
        [SerializeField] private Color hoverInvalidColor = new Color(1f, 0.25f, 0.3f, 0.95f);
        [SerializeField] private Color selectedColor = new Color(0.2f, 0.95f, 1f, 1f);
        [Tooltip("Outline for entities currently changed by a glitch (electric blue = collision).")]
        [SerializeField] private Color glitchedColor = new Color(0.15f, 0.5f, 1f, 0.9f);
        [Tooltip("Supplies each glitch's colour for the outlines of what it changed or created.")]
        [SerializeField] private GlitchController glitches;

        private readonly List<BoundsOutline> pool = new List<BoundsOutline>();
        private readonly List<IEntityDescriptor> buffer = new List<IEntityDescriptor>();
        private GameServices services;
        private Action<GameServices> bindCallback;
        private int used;

        private void OnEnable()
        {
            bindCallback = s => services = s;
            GameServices.WhenReady(bindCallback);
        }

        private void OnDisable()
        {
            GameServices.CancelWhenReady(bindCallback);
            services = null;
            for (int i = 0; i < pool.Count; i++) pool[i].Hide();
        }

        private void LateUpdate()
        {
            used = 0;
            if (services != null && !services.IsDisposed && lineMaterial != null) DrawAll();
            for (int i = used; i < pool.Count; i++) pool[i].Hide();
        }

        private void DrawAll()
        {
            var selection = services.Selection;
            var hovered = selection.Hovered;
            var selected = selection.Selected;
            var state = services.State.Current;

            if (state == GameState.Inspection)
            {
                services.Registry.GetAll(buffer);
                for (int i = 0; i < buffer.Count; i++)
                {
                    var entity = buffer[i] as GameEntity;
                    if (entity == null || entity.Id == hovered || entity.Id == selected) continue;
                    if (services.Glitches.IsInvolved(entity.Id)) continue;
                    if (entity.Category == EntityCategory.Anomaly) continue;
                    if (!entity.Capabilities.HasAll(EntityCapabilities.Inspectable)) continue;
                    Draw(entity, inspectColor);
                }
                buffer.Clear();
            }

            // Every entity currently changed by a glitch stays outlined, so the player can see what they altered.
            var manager = services.Glitches;
            for (int i = 0; i < manager.ActiveCount; i++)
            {
                var op = manager.GetActive(i);
                var color = glitches != null ? glitches.ColorFor(op) : glitchedColor;
                color.a = glitchedColor.a;
                for (int t = 0; t < op.TargetCount + op.ProductCount; t++)
                {
                    var id = t < op.TargetCount ? op.Target(t) : op.Product(t - op.TargetCount);
                    if (id == hovered || id == selected) continue;
                    if (services.Registry.TryGet(id, out var target) && target is GameEntity glitchedEntity)
                        Draw(glitchedEntity, color);
                }
            }

            bool gameplay = state == GameState.Playing || state == GameState.Inspection;
            if (gameplay && hovered.IsValid && hovered != selected && selection.TryGetHovered(out var h) && h is GameEntity hoveredEntity)
            {
                Draw(hoveredEntity, selection.HoveredValidation.IsValid ? hoverValidColor : hoverInvalidColor);
            }

            if (selection.TryGetSelected(out var s) && s is GameEntity selectedEntity)
            {
                var c = selectedColor;
                c.a *= 0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * 6f);
                Draw(selectedEntity, c);
            }
        }

        private void Draw(GameEntity entity, Color color)
        {
            var bounds = entity.GetWorldBounds();
            bounds.Expand(padding * 2f);
            NextOutline().Show(bounds, color);
        }

        private BoundsOutline NextOutline()
        {
            if (used < pool.Count) return pool[used++];
            var go = new GameObject("Outline_" + pool.Count, typeof(LineRenderer), typeof(BoundsOutline));
            go.layer = GameLayers.IgnoreRaycastLayer;
            go.transform.SetParent(transform, false);
            var outline = go.GetComponent<BoundsOutline>();
            outline.Initialize(lineMaterial, lineWidth);
            pool.Add(outline);
            used++;
            return outline;
        }
    }
}
