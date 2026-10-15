using System.Collections.Generic;
using BugReport.Core.Entities;
using BugReport.Core.GameFlow;
using BugReport.Glitches;
using BugReport.Hero;
using BugReport.Presentation;
using BugReport.Tutorial;
using UnityEngine;
using UnityEngine.UIElements;

namespace BugReport.UI
{
    /// <summary>
    /// GDD 9.3 system inspection, drawn under the HUD: in inspection mode every inspectable
    /// entity gets a floating tag (name, id, one coloured dot per glitch that can affect it).
    /// The selected entity gets a property panel (category, description, capabilities, current
    /// state). Also shows the hero's state icon. Gameplay-relevant information only.
    /// </summary>
    public sealed class InspectionOverlay : UiLayer
    {
        private static readonly (EntityCapabilities cap, Color color, string name)[] Pips =
        {
            (EntityCapabilities.CollisionTransferable, CollisionTransferDefinition.ElectricBlue, "COLLISION"),
            (EntityCapabilities.Duplicable, DuplicationDefinition.Magenta, "DUPLICATE"),
            (EntityCapabilities.Freezable, FreezeDefinition.PaleCyan, "FREEZE"),
            (EntityCapabilities.PropertyMutable, PropertySwapDefinition.Amber, "SWAP"),
            (EntityCapabilities.AIControllable, Guards.AiOverrideDefinition.Violet, "AI"),
            (EntityCapabilities.Desyncable, DesyncDefinition.Lime, "DESYNC"),
        };

        [SerializeField] private Camera viewCamera;
        [SerializeField, OptionalReference] private HeroAnimator heroAnimator;
        [SerializeField, OptionalReference] private VisualSettings settings;
        [SerializeField, OptionalReference] private GuideDirector guide;
        [SerializeField, Min(1f)] private float maxDistance = 28f;

        private readonly List<IEntityDescriptor> buffer = new List<IEntityDescriptor>();
        private readonly List<VisualElement> tags = new List<VisualElement>();
        private VisualElement tagLayer;
        private Label heroIcon;
        private VisualElement guideMarker;
        private VisualElement panel;
        private Label panelName;
        private Label panelId;
        private Label panelCategory;
        private Label panelDescription;
        private VisualElement panelChips;
        private Label panelState;

        protected override string LayerName => "inspection";
        protected override int SortKey => 0;

        /// <summary>Tags currently placed (tests).</summary>
        public int VisibleTagCount { get; private set; }
        public bool PanelVisible => panel != null && Ui.IsShown(panel);
        public bool GuideMarkerVisible => guideMarker != null && Ui.IsShown(guideMarker);

        public void Configure(Camera cam, HeroAnimator hero, VisualSettings visualSettings, GuideDirector guideDirector = null)
        {
            guide = guideDirector;
            viewCamera = cam;
            heroAnimator = hero;
            settings = visualSettings;
        }

        protected override void Build(VisualElement layer)
        {
            tagLayer = Ui.Add(layer, Ui.Element("tags", "br-tags"));
            heroIcon = Ui.Add(layer, Ui.Text("hero-icon", string.Empty, "br-hero-icon"));
            guideMarker = Ui.Add(layer, Ui.Element("guide-marker", "br-guide-marker"));
            guideMarker.Add(Ui.Element("guide-marker-diamond", "br-guide-marker__diamond"));

            panel = Ui.Add(layer, Ui.Element("inspect-panel", "br-panel", "br-inspect"));
            var head = Ui.Add(panel, Ui.Element("inspect-head", "br-inspect__head"));
            panelName = Ui.Add(head, Ui.Text("inspect-name", string.Empty, "br-inspect__name"));
            panelId = Ui.Add(head, Ui.Text("inspect-id", string.Empty, "br-inspect__id"));
            panelCategory = Ui.Add(panel, Ui.Text("inspect-category", string.Empty, "br-caption"));
            panelDescription = Ui.Add(panel, Ui.Text("inspect-description", string.Empty, "br-inspect__description"));
            panelChips = Ui.Add(panel, Ui.Element("inspect-chips", "br-inspect__chips"));
            foreach (var pip in Pips)
            {
                var chip = Ui.Add(panelChips, Ui.Text("chip-" + pip.name.ToLowerInvariant(), pip.name, "br-chip"));
                chip.style.borderLeftColor = pip.color;
            }
            panelState = Ui.Add(panel, Ui.Text("inspect-state", string.Empty, "br-inspect__state"));

            tags.Clear();
            VisibleTagCount = 0;
        }

        protected override void Refresh()
        {
            var state = HasServices ? Services.State.Current : GameState.Boot;
            bool gameplay = state == GameState.Playing || state == GameState.Inspection;
            Ui.Show(Layer, gameplay && viewCamera != null);
            VisibleTagCount = 0;
            if (!gameplay || viewCamera == null) return;

            bool labels = VisualSettings.On(settings, s => s.inspectionLabels);
            if (labels && state == GameState.Inspection)
            {
                Services.Registry.GetAll(buffer);
                foreach (var d in buffer)
                {
                    if (!(d is GameEntity e) || e.Category == EntityCategory.Anomaly) continue;
                    if (!e.Capabilities.HasAll(EntityCapabilities.Inspectable)) continue;
                    var bounds = e.GetWorldBounds();
                    if (!TryPlace(bounds.center + Vector3.up * (bounds.extents.y + 0.25f), out var position)) continue;
                    FillTag(NextTag(), e, position);
                }
                buffer.Clear();
            }
            for (int i = VisibleTagCount; i < tags.Count; i++) Ui.Show(tags[i], false);

            RefreshHeroIcon();
            RefreshGuideMarker();
            RefreshPanel(labels);
        }

        // ---------------------------------------------------------------- world tags

        private bool TryPlace(Vector3 world, out Vector2 panelPosition)
        {
            panelPosition = default;
            var toPoint = world - viewCamera.transform.position;
            float depth = Vector3.Dot(toPoint, viewCamera.transform.forward);
            if (depth <= 0.1f || toPoint.magnitude > maxDistance || Layer.panel == null) return false;
            panelPosition = RuntimePanelUtils.CameraTransformWorldToPanel(Layer.panel, world, viewCamera);
            return true;
        }

        private VisualElement NextTag()
        {
            if (VisibleTagCount == tags.Count)
            {
                var tag = Ui.Add(tagLayer, Ui.Element("tag", "br-tag"));
                var line = Ui.Add(tag, Ui.Element("tag-line", "br-tag__line"));
                line.Add(Ui.Text("tag-name", string.Empty, "br-tag__name"));
                line.Add(Ui.Text("tag-id", string.Empty, "br-tag__id"));
                var pips = Ui.Add(tag, Ui.Element("tag-pips", "br-tag__pips"));
                foreach (var pip in Pips)
                {
                    var dot = Ui.Add(pips, Ui.Element("pip", "br-pip"));
                    dot.style.backgroundColor = pip.color;
                }
                tags.Add(tag);
            }
            return tags[VisibleTagCount++];
        }

        private static void FillTag(VisualElement tag, GameEntity e, Vector2 position)
        {
            Ui.Show(tag, true);
            tag.style.left = position.x;
            tag.style.top = position.y;
            Ui.SetText(tag.Q<Label>("tag-name"), e.DisplayName);
            Ui.SetText(tag.Q<Label>("tag-id"), e.Id.ToString());
            var pips = tag.Q("tag-pips");
            for (int i = 0; i < Pips.Length; i++) Ui.Show(pips[i], e.Capabilities.HasAll(Pips[i].cap));
        }

        private void RefreshHeroIcon()
        {
            string icon = heroAnimator != null && VisualSettings.On(settings, s => s.heroAnimation) ? heroAnimator.CurrentIcon : string.Empty;
            Vector2 position = default;
            bool on = !string.IsNullOrEmpty(icon) && TryPlace(heroAnimator.transform.position + Vector3.up * 1.6f, out position);
            Ui.Show(heroIcon, on);
            if (!on) return;
            heroIcon.style.left = position.x;
            heroIcon.style.top = position.y;
            Ui.SetText(heroIcon, icon);
        }

        private void RefreshGuideMarker()
        {
            var target = guide != null ? guide.CurrentTarget : null;
            Vector2 position = default;
            bool on = false;
            if (target != null)
            {
                var bounds = target.GetWorldBounds();
                on = TryPlace(bounds.center + Vector3.up * (bounds.extents.y + 0.6f), out position);
            }
            Ui.Show(guideMarker, on);
            if (!on) return;
            guideMarker.style.left = position.x;
            guideMarker.style.top = position.y;
            guideMarker.EnableInClassList("br-guide-marker--up", Mathf.Repeat(Time.unscaledTime, 1f) < 0.5f);
        }

        // ---------------------------------------------------------------- selected panel

        private void RefreshPanel(bool enabled)
        {
            GameEntity e = null;
            if (enabled && Services.Selection.TryGetSelected(out var selected)) e = selected as GameEntity;
            Ui.Show(panel, e != null);
            if (e == null) return;

            Ui.SetText(panelName, e.DisplayName);
            Ui.SetText(panelId, e.Id.ToString());
            Ui.SetText(panelCategory, e.Category.ToString().ToUpperInvariant());
            Ui.SetText(panelDescription, e.Description ?? string.Empty);
            Ui.Show(panelDescription, !string.IsNullOrEmpty(e.Description));
            for (int i = 0; i < Pips.Length; i++) Ui.Show(panelChips[i], e.Capabilities.HasAll(Pips[i].cap));

            string stateText = string.Empty;
            var collision = e.GetComponent<CollisionCapability>();
            if (collision != null)
                stateText += "COLLISION: " + collision.EffectiveMode.ToString().ToUpperInvariant()
                    + (collision.EffectiveMode != collision.AuthoredMode ? "  (ALTERED)" : string.Empty);
            var freeze = e.GetComponent<FreezeCapability>();
            if (freeze != null && freeze.IsFrozen) stateText += (stateText.Length > 0 ? "\n" : string.Empty) + "TIME: FROZEN";
            var copy = e.GetComponent<DuplicateMarker>();
            if (copy != null) stateText += (stateText.Length > 0 ? "\n" : string.Empty) + "DUPLICATE (GEN " + copy.Generation + ")";
            Ui.SetText(panelState, stateText);
            Ui.Show(panelState, stateText.Length > 0);
            bool altered = (collision != null && collision.EffectiveMode != collision.AuthoredMode)
                || (freeze != null && freeze.IsFrozen) || copy != null;
            panelState.EnableInClassList("br-inspect__state--altered", altered);
        }
    }
}
