using UnityEngine;

namespace BugReport.Containment
{
    /// <summary>
    /// Placeholder containment HUD (IMGUI, red/white): a thin pressure bar while pressure > 0 and a
    /// stage banner from stage 1, which flashes when the stage rises. Replaced in the HUD pass (M5).
    /// </summary>
    public sealed class ContainmentPresenter : MonoBehaviour
    {
        private static readonly string[] StageNames =
        {
            "SYSTEM NOMINAL", "ANOMALY DETECTED", "LOCAL INVESTIGATION", "PATCH DEPLOYMENT", "EMERGENCY RECOVERY",
        };

        [SerializeField] private ContainmentDirector director;
        [SerializeField, Min(0.1f)] private float flashDuration = 1.2f;

        private int shownStage;
        private float flashUntil;
        private GUIStyle bannerStyle;

        private void OnGUI()
        {
            var model = director != null ? director.Model : null;
            if (model == null) return;

            if (model.Stage > shownStage) flashUntil = Time.unscaledTime + flashDuration;
            shownStage = model.Stage;
            if (Event.current.type != EventType.Repaint && Event.current.type != EventType.Layout) return;

            if (bannerStyle == null)
            {
                bannerStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 18, fontStyle = FontStyle.Bold };
            }

            float width = 320f;
            float x = (Screen.width - width) * 0.5f;
            if (model.Pressure > 0f)
            {
                var previous = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, 0.25f);
                GUI.DrawTexture(new Rect(x, 12f, width, 6f), Texture2D.whiteTexture);
                GUI.color = Color.Lerp(Color.white, new Color(1f, 0.2f, 0.25f), model.Stage / 4f);
                GUI.DrawTexture(new Rect(x, 12f, width * model.Pressure01, 6f), Texture2D.whiteTexture);
                GUI.color = previous;
            }

            if (model.Stage == 0) return;
            bool flashing = Time.unscaledTime < flashUntil && Mathf.Repeat(Time.unscaledTime * 6f, 1f) < 0.5f;
            bannerStyle.normal.textColor = flashing ? Color.white : new Color(1f, 0.25f, 0.3f);
            string text = "// " + StageNames[Mathf.Clamp(model.Stage, 0, StageNames.Length - 1)] + " //";
            if (model.Stage >= 3 && director.NextPatchIn > 0f) text += "\nPATCH IN " + director.NextPatchIn.ToString("0.0") + "s";
            GUI.Label(new Rect(x - 40f, 22f, width + 80f, 48f), text, bannerStyle);
        }
    }
}
