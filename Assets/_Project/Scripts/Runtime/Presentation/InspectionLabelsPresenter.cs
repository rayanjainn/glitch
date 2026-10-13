using System;
using System.Collections.Generic;
using System.Text;
using BugReport.Core.Entities;
using BugReport.Core.GameFlow;
using BugReport.Glitches;
using UnityEngine;

namespace BugReport.Presentation
{
    /// <summary>
    /// GDD 9.3 system inspection: in inspection mode every inspectable entity gets a floating
    /// diagnostic tag (name, id, one coloured dot per glitch that can affect it). The selected
    /// entity always gets a property panel: category, description, capabilities, current state.
    /// Shows gameplay-relevant information only; never internal components.
    /// </summary>
    public sealed class InspectionLabelsPresenter : MonoBehaviour
    {
        [SerializeField, OptionalReference] private VisualSettings settings;
        [SerializeField] private Camera viewCamera;
        [SerializeField, Min(1f)] private float maxDistance = 28f;

        private static readonly (EntityCapabilities cap, Color color, string name)[] Pips =
        {
            (EntityCapabilities.CollisionTransferable, CollisionTransferDefinition.ElectricBlue, "COLLISION"),
            (EntityCapabilities.Duplicable, DuplicationDefinition.Magenta, "DUPLICATE"),
            (EntityCapabilities.Freezable, FreezeDefinition.PaleCyan, "FREEZE"),
        };

        private readonly List<IEntityDescriptor> buffer = new List<IEntityDescriptor>();
        private readonly StringBuilder text = new StringBuilder(256);
        private GameServices services;
        private Action<GameServices> bindCallback;
        private GUIStyle tagStyle;
        private GUIStyle panelStyle;

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
            if (!VisualSettings.On(settings, s => s.inspectionLabels)) return;
            if (services == null || services.IsDisposed || viewCamera == null) return;
            if (Event.current.type != EventType.Repaint) return;
            var state = services.State.Current;
            if (state != GameState.Playing && state != GameState.Inspection) return;
            EnsureStyles();

            if (state == GameState.Inspection)
            {
                services.Registry.GetAll(buffer);
                foreach (var d in buffer)
                {
                    if (!(d is GameEntity e) || e.Category == EntityCategory.Anomaly) continue;
                    if (!e.Capabilities.HasAll(EntityCapabilities.Inspectable)) continue;
                    DrawTag(e);
                }
                buffer.Clear();
            }

            if (services.Selection.TryGetSelected(out var selected) && selected is GameEntity selectedEntity)
                DrawPanel(selectedEntity);
        }

        private void DrawTag(GameEntity e)
        {
            var bounds = e.GetWorldBounds();
            if (!WorldLabel.TryGetRect(viewCamera, bounds.center + Vector3.up * (bounds.extents.y + 0.25f), new Vector2(170f, 34f), maxDistance, out var rect)) return;
            WorldLabel.Panel(rect, new Color(0.02f, 0.05f, 0.08f, 0.72f));
            GUI.Label(new Rect(rect.x + 6f, rect.y + 2f, rect.width - 12f, 18f), e.DisplayName + "  <color=#7fa>" + e.Id + "</color>", tagStyle);

            float x = rect.x + 8f;
            foreach (var pip in Pips)
            {
                if (!e.Capabilities.HasAll(pip.cap)) continue;
                WorldLabel.Panel(new Rect(x, rect.y + 22f, 9f, 7f), pip.color);
                x += 13f;
            }
        }

        private void DrawPanel(GameEntity e)
        {
            text.Clear();
            text.Append("<b>").Append(e.DisplayName).Append("</b>  ").Append(e.Id).Append('\n');
            text.Append(e.Category.ToString().ToUpperInvariant()).Append('\n');
            if (!string.IsNullOrEmpty(e.Description)) text.Append("<i>").Append(e.Description).Append("</i>\n");

            foreach (var pip in Pips)
            {
                if (!e.Capabilities.HasAll(pip.cap)) continue;
                text.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(pip.color)).Append(">■ ").Append(pip.name).Append("</color>  ");
            }
            text.Append('\n');

            var collision = e.GetComponent<CollisionCapability>();
            if (collision != null) text.Append("COLLISION: ").Append(collision.EffectiveMode.ToString().ToUpperInvariant())
                .Append(collision.EffectiveMode != collision.AuthoredMode ? "  <color=#ff6070>(ALTERED)</color>" : string.Empty).Append('\n');
            var freeze = e.GetComponent<FreezeCapability>();
            if (freeze != null && freeze.IsFrozen) text.Append("<color=#9ef>TIME: FROZEN</color>\n");
            var copy = e.GetComponent<DuplicateMarker>();
            if (copy != null) text.Append("<color=#f6d>DUPLICATE (gen ").Append(copy.Generation).Append(")</color>\n");

            var content = new GUIContent(text.ToString());
            float w = 300f;
            float h = panelStyle.CalcHeight(content, w);
            var rect = new Rect(Screen.width - w - 16f, 16f, w, h);
            WorldLabel.Panel(rect, new Color(0.02f, 0.05f, 0.08f, 0.8f));
            GUI.Label(rect, content, panelStyle);
        }

        private void EnsureStyles()
        {
            if (tagStyle != null) return;
            tagStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, fontStyle = FontStyle.Bold, richText = true };
            tagStyle.normal.textColor = new Color(0.85f, 0.97f, 1f);
            panelStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, richText = true, wordWrap = true, padding = new RectOffset(10, 10, 8, 8) };
            panelStyle.normal.textColor = new Color(0.85f, 0.97f, 1f);
        }
    }
}
