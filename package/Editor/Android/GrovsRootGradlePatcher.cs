using System.Text.RegularExpressions;

namespace GrovsSDK.Editor.Android
{
    /// <summary>
    /// The R8 bundled with Android Gradle plugin 7.x cannot dex current AndroidX and Kotlin 1.9 libraries,
    /// so the root build.gradle gets a newer R8 on its classpath.
    /// </summary>
    internal static class GrovsRootGradlePatcher
    {
        const string R8Artifact = "com.android.tools:r8:";

        const string BuildScript =
            "buildscript {\n" +
            "    repositories {\n" +
            "        google()\n" +
            "    }\n" +
            "    dependencies {\n" +
            "        classpath 'com.android.tools:r8:8.5.35'\n" +
            "    }\n" +
            "}\n\n";

        static readonly Regex PluginVersion =
            new Regex(@"id\s*\(?\s*['""]com\.android\.(?:application|library)['""]\s*\)?\s*version\s*['""](\d+)\.");

        static readonly Regex PluginsBlock = new Regex(@"^[ \t]*plugins\s*\{", RegexOptions.Multiline);

        public static string Apply(string text)
        {
            if (text.Contains(R8Artifact)) return text;
            var version = PluginVersion.Match(text);
            if (!version.Success || int.Parse(version.Groups[1].Value) >= 8) return text;
            var plugins = PluginsBlock.Match(text);
            if (!plugins.Success) return text;
            return text.Insert(plugins.Index, BuildScript);
        }
    }
}
