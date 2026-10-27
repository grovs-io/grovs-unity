using System.Text.RegularExpressions;

namespace GrovsSDK.Editor.Android
{
    /// <summary>Makes sure gradle.properties turns on AndroidX and Jetifier.</summary>
    internal static class GrovsGradlePropertiesWriter
    {
        static readonly string[] RequiredKeys = { "android.useAndroidX", "android.enableJetifier" };

        public static string Apply(string text)
        {
            text = text ?? "";
            foreach (var key in RequiredKeys)
            {
                var line = key + "=true";
                var existing = new Regex(@"^[ \t]*" + Regex.Escape(key) + @"[ \t]*=.*$", RegexOptions.Multiline);
                if (existing.IsMatch(text))
                {
                    text = existing.Replace(text, line);
                    continue;
                }
                if (text.Length > 0 && !text.EndsWith("\n")) text += "\n";
                text += line + "\n";
            }
            return text;
        }
    }
}
