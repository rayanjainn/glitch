using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace BugReport.UI
{
    /// <summary>
    /// Base for one layer of the shared game UI document (UI Toolkit). Each layer builds its own
    /// subtree into the document root, ordered by <see cref="SortKey"/>, and rebuilds itself if
    /// the document recreates its root (UIDocument does on re-enable). Layers read game state
    /// and never change it, except through explicit button actions in menus.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public abstract class UiLayer : MonoBehaviour
    {
        [SerializeField] private StyleSheet styleSheet;
        [SerializeField, OptionalReference, Tooltip("Font for headings (elements with the br-display class). Theme font when empty.")]
        private Font displayFont;

        private UIDocument document;
        private VisualElement builtFor;
        private Action<GameServices> bindCallback;

        protected GameServices Services { get; private set; }
        protected VisualElement Layer { get; private set; }
        protected bool HasServices => Services != null && !Services.IsDisposed;

        /// <summary>True once built and not hidden by the layer itself (tests).</summary>
        public bool Visible => Layer != null && Ui.IsShown(Layer);

        /// <summary>Element name of this layer's container (also its USS hook).</summary>
        protected abstract string LayerName { get; }
        /// <summary>Lower keys draw first (underneath).</summary>
        protected abstract int SortKey { get; }
        protected abstract void Build(VisualElement layer);
        protected abstract void Refresh();
        protected virtual void OnBound(GameServices services) { }
        protected virtual void OnUnbound() { }

        public void Configure(StyleSheet sheet) => styleSheet = sheet;

        protected virtual void OnEnable()
        {
            bindCallback = s =>
            {
                Services = s;
                OnBound(s);
            };
            GameServices.WhenReady(bindCallback);
        }

        protected virtual void OnDisable()
        {
            GameServices.CancelWhenReady(bindCallback);
            if (Services != null) OnUnbound();
            Services = null;
            Layer?.RemoveFromHierarchy();
            Layer = null;
            builtFor = null;
        }

        private void LateUpdate() => RefreshNow();

        /// <summary>Builds if needed and updates from the current game state. Public for tests.</summary>
        public void RefreshNow()
        {
            if (!EnsureBuilt()) return;
            Refresh();
        }

        /// <summary>Finds a named element inside this layer (tests, debugging).</summary>
        public T Find<T>(string elementName) where T : VisualElement => EnsureBuilt() ? Layer.Q<T>(elementName) : null;

        private bool EnsureBuilt()
        {
            if (document == null) document = GetComponent<UIDocument>();
            var root = document != null ? document.rootVisualElement : null;
            if (root == null) return false;
            // UIDocument may keep its root but clear it when re-enabled, so check attachment too.
            if (root == builtFor && Layer != null && Layer.parent == root) return true;

            if (styleSheet != null && !root.styleSheets.Contains(styleSheet)) root.styleSheets.Add(styleSheet);
            root.AddToClassList("br-root");
            root.pickingMode = PickingMode.Ignore;

            Layer?.RemoveFromHierarchy();
            Layer = Ui.Element(LayerName, "br-layer");
            Layer.userData = SortKey;
            int index = 0;
            while (index < root.childCount && root[index].userData is int key && key <= SortKey) index++;
            root.Insert(index, Layer);
            builtFor = root;
            Build(Layer);
            if (displayFont != null)
            {
                var definition = new StyleFontDefinition(FontDefinition.FromFont(displayFont));
                Layer.Query(className: "br-display").ForEach(e => e.style.unityFontDefinition = definition);
            }
            return true;
        }
    }

    /// <summary>Small helpers for building UI Toolkit trees in code.</summary>
    public static class Ui
    {
        public static VisualElement Element(string name, params string[] classes)
        {
            var e = new VisualElement { name = name, pickingMode = PickingMode.Ignore };
            foreach (var c in classes) e.AddToClassList(c);
            return e;
        }

        public static Label Text(string name, string text, params string[] classes)
        {
            var l = new Label(text) { name = name, pickingMode = PickingMode.Ignore };
            l.AddToClassList("br-label");
            foreach (var c in classes) l.AddToClassList(c);
            return l;
        }

        public static T Add<T>(VisualElement parent, T child) where T : VisualElement
        {
            parent.Add(child);
            return child;
        }

        public static void Show(VisualElement e, bool visible)
        {
            var display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (e.style.display != display) e.style.display = display;
        }

        public static bool IsShown(VisualElement e) => e.style.display != DisplayStyle.None;

        /// <summary>
        /// Focus on the next UI update: an element shown this frame has no resolved style yet and
        /// would refuse focus now.
        /// </summary>
        public static void FocusSoon(Focusable e)
        {
            if (e is VisualElement element) element.schedule.Execute(() => e.Focus());
        }

        public static void SetText(Label label, string text)
        {
            if (label.text != text) label.text = text;
        }

        public static uint Pack(Color c)
        {
            Color32 c32 = c;
            return ((uint)c32.r << 24) | ((uint)c32.g << 16) | ((uint)c32.b << 8) | c32.a;
        }

        public static Color Unpack(uint rgba)
            => new Color32((byte)(rgba >> 24), (byte)(rgba >> 16), (byte)(rgba >> 8), (byte)rgba);
    }
}
