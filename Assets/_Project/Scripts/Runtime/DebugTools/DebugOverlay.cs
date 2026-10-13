using System;
using System.Text;
using BugReport.Anomaly;
using BugReport.Core.Entities;
using BugReport.Core.GameFlow;
using BugReport.Glitches;
using BugReport.Hero;
using BugReport.Interaction;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace BugReport.DebugTools
{
    /// <summary>
    /// Developer overlay (IMGUI). F1 toggles it, F5 restarts the level.
    /// Debug keys read the keyboard directly on purpose: they are not gameplay
    /// actions, must not be remappable, and are stripped with this component.
    /// </summary>
    public sealed class DebugOverlay : MonoBehaviour
    {
        [SerializeField] private AnomalyMotor anomaly;
        [SerializeField] private SelectionController selection;
        [SerializeField] private HeroController hero;
        [SerializeField] private GlitchController glitches;
        [SerializeField] private bool visible = true;
        [SerializeField, Min(0.5f)] private float feedbackDuration = 2.5f;

        private readonly StringBuilder text = new StringBuilder(1024);
        private GameServices services;
        private Action<GameServices> bindCallback;
        private GUIStyle panelStyle;
        private GUIStyle bannerStyle;
        private float smoothedDelta = 1f / 60f;

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

        private void Update()
        {
            smoothedDelta = Mathf.Lerp(smoothedDelta, Time.unscaledDeltaTime, 0.05f);

            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.f1Key.wasPressedThisFrame) visible = !visible;
            if (keyboard.f5Key.wasPressedThisFrame) RestartLevel();
        }

        public static void RestartLevel()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.buildIndex >= 0) SceneManager.LoadScene(scene.buildIndex);
            else Debug.LogWarning("[BUG REPORT] Cannot restart: active scene is not in Build Settings.");
        }

        private void OnGUI()
        {
            EnsureStyles();
            // The pause screen itself is drawn by LevelMenus.
            if (!visible) return;
            BuildText();
            var content = new GUIContent(text.ToString());
            float width = 380f;
            float height = panelStyle.CalcHeight(content, width);
            GUI.Box(new Rect(10, 10, width, height), content, panelStyle);
        }

        private void BuildText()
        {
            text.Clear();
            text.Append("<b>BUG REPORT // DEBUG</b>  (F1 hide, F5 restart)\n");
            text.Append("FPS ").Append(Mathf.RoundToInt(1f / Mathf.Max(1e-4f, smoothedDelta)));

            if (services == null || services.IsDisposed)
            {
                text.Append("\n<color=#ff6070>NO GAME SERVICES (missing GameBootstrap?)</color>");
                return;
            }

            text.Append("   STATE ").Append(services.State.Current.ToString().ToUpperInvariant());
            text.Append("   ENTITIES ").Append(services.Registry.Count).Append('\n');

            if (anomaly != null)
            {
                var p = anomaly.transform.position;
                text.Append("ANOMALY  pos ").Append(p.x.ToString("0.0")).Append(", ").Append(p.y.ToString("0.0")).Append(", ").Append(p.z.ToString("0.0"))
                    .Append("  speed ").Append(anomaly.Speed.ToString("0.0")).Append('\n');
            }

            var sel = services.Selection;
            text.Append("HOVER    ");
            if (sel.TryGetHovered(out var hovered))
            {
                AppendEntity(hovered);
                text.Append(sel.HoveredValidation.IsValid ? "  <color=#5ff>VALID</color>" : "  <color=#ff6070>" + sel.HoveredValidation.Describe() + "</color>");
            }
            else text.Append("-");
            text.Append('\n');

            text.Append("SELECTED ");
            if (sel.TryGetSelected(out var selected)) AppendEntity(selected);
            else text.Append("-");
            text.Append('\n');

            if (selection != null && Time.unscaledTime - selection.LastFeedbackTime < feedbackDuration)
            {
                var fb = selection.LastFeedback;
                string color = fb.Kind == SelectionFeedbackKind.Rejected ? "#ff6070" : "#5ff";
                text.Append("> <color=").Append(color).Append('>').Append(fb.Message).Append("</color>\n");
            }

            if (glitches != null)
            {
                var def = glitches.CurrentDefinition;
                text.Append("GLITCH   ").Append(def != null ? def.DisplayName : "-").Append("  [Q] next  [LMB] apply  [R] revert\n");
                if (glitches.HasPreview)
                {
                    var p = glitches.Preview;
                    text.Append("         ").Append(p.Succeeded ? "<color=#5ff>READY</color>" : "<color=#ff6070>" + p.Describe() + "</color>").Append('\n');
                }
                else if (!string.IsNullOrEmpty(glitches.Prompt))
                {
                    text.Append("         ").Append(glitches.Prompt).Append('\n');
                }

                if (Time.unscaledTime - glitches.LastFeedbackTime < feedbackDuration)
                {
                    var gf = glitches.LastFeedback;
                    text.Append("> <color=#").Append(ColorUtility.ToHtmlStringRGB(gf.Color)).Append('>').Append(gf.Message).Append("</color>\n");
                }

                var manager = services.Glitches;
                for (int i = 0; i < manager.ActiveCount; i++)
                {
                    var op = manager.GetActive(i);
                    float remaining = op.Remaining(Time.time);
                    text.Append("  ").Append(op.Id.ToString()).Append(' ').Append(op.Spec.DisplayName)
                        .Append(float.IsPositiveInfinity(remaining) ? "  (until reverted)" : "  " + remaining.ToString("0.0") + "s").Append('\n');
                }
            }

            if (hero != null)
            {
                text.Append("HERO     ").Append(hero.State.ToString().ToUpperInvariant())
                    .Append("  route ").Append(hero.RouteIndex).Append('/').Append(hero.RouteLength)
                    .Append(hero.PathComplete ? "" : " (partial)").Append('\n');
            }

            var containment = services.Containment;
            text.Append("CONTAIN  stage ").Append(containment.Stage).Append("  pressure ").Append(containment.Pressure.ToString("0"))
                .Append("  cooldown x").Append(services.Glitches.CooldownScale.ToString("0.0")).Append('\n');

            text.Append("\nWASD move  Mouse look  Space/Ctrl up/down  Shift boost\n");
            text.Append("E select  RMB clear  LMB apply  R revert  Q glitch  Tab inspect  Esc pause");
        }

        private void AppendEntity(IEntityDescriptor entity)
        {
            text.Append(entity.DisplayName).Append(" [").Append(entity.Id.ToString()).Append("] ")
                .Append(entity.Category.ToString().ToUpperInvariant());
            if (entity.Capabilities != EntityCapabilities.None)
                text.Append("\n         caps: ").Append(entity.Capabilities.ToString());
        }

        private void EnsureStyles()
        {
            if (panelStyle != null) return;
            panelStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.UpperLeft,
                richText = true,
                wordWrap = true,
                fontSize = 13,
                padding = new RectOffset(10, 10, 8, 8),
            };
            panelStyle.normal.textColor = new Color(0.85f, 0.95f, 1f);
            bannerStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 22,
                fontStyle = FontStyle.Bold,
            };
            bannerStyle.normal.textColor = Color.white;
        }
    }
}
