using System.Text;

namespace BugReport.Core.Tutorial
{
    /// <summary>
    /// Guide and hint strings are authored with key tokens such as "[E]" or "[LMB]". This turns
    /// each token into a highlighted key (UI Toolkit rich text) so instructions scan quickly.
    /// </summary>
    public static class GuideText
    {
        public static string HighlightKeys(string text, string colorHex)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            var sb = new StringBuilder(text.Length + 32);
            int i = 0;
            while (i < text.Length)
            {
                int open = text.IndexOf('[', i);
                int close = open >= 0 ? text.IndexOf(']', open + 1) : -1;
                if (open < 0 || close < 0 || close == open + 1 || text.IndexOf('[', open + 1, close - open - 1) >= 0)
                {
                    if (open < 0 || close < 0)
                    {
                        sb.Append(text, i, text.Length - i);
                        break;
                    }
                    sb.Append(text, i, open + 1 - i);
                    i = open + 1;
                    continue;
                }
                sb.Append(text, i, open - i);
                sb.Append("<b><color=").Append(colorHex).Append('>');
                sb.Append(text, open, close - open + 1);
                sb.Append("</color></b>");
                i = close + 1;
            }
            return sb.ToString();
        }
    }
}
