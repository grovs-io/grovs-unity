using System.Text.RegularExpressions;
using UnityEngine;

namespace GrovsSDK.Editor.Android
{
    /// <summary>
    /// The R8 bundled with Android Gradle plugin 7.x cannot dex current AndroidX and Kotlin 1.9 libraries,
    /// so the root build.gradle gets a newer R8 on its classpath.
    /// </summary>
    internal static class GrovsRootGradlePatcher
    {
        const string R8Artifact = "com.android.tools:r8:";
        const string R8Classpath = "classpath 'com.android.tools:r8:8.5.35'";

        const string BuildScript =
            "buildscript {\n" +
            "    repositories {\n" +
            "        google()\n" +
            "    }\n" +
            "    dependencies {\n" +
            "        " + R8Classpath + "\n" +
            "    }\n" +
            "}\n\n";

        public const string UnknownVersionWarning =
            "Grovs: could not read the Android Gradle plugin version from the root build.gradle. " +
            "If it is below 8, add classpath 'com.android.tools:r8:8.5.35' to its buildscript dependencies.";

        static readonly Regex PluginVersion = new Regex(
            @"id\s*\(?\s*['""]com\.android\.(?:application|library)['""]\s*\)?\s*version\s*['""](\d+)\.");

        static readonly Regex ClasspathVersion = new Regex(@"com\.android\.tools\.build:gradle:(\d+)\.");

        static readonly Regex PluginsBlock = new Regex(@"^[ \t]*plugins\s*\{", RegexOptions.Multiline);
        static readonly Regex BuildScriptBlock = new Regex(@"^[ \t]*buildscript\s*\{", RegexOptions.Multiline);
        static readonly Regex DependenciesBlock = new Regex(@"^[ \t]*dependencies\s*\{", RegexOptions.Multiline);
        static readonly Regex RepositoriesBlock = new Regex(@"^[ \t]*repositories\s*\{", RegexOptions.Multiline);

        public static string Apply(string text)
        {
            var patched = Apply(text, out var warning);
            if (warning != null) Debug.LogWarning(warning);
            return patched;
        }

        public static string Apply(string text, out string warning)
        {
            warning = null;
            if (text.Contains(R8Artifact)) return text;

            var buildScript = BuildScriptBlock.Match(text);
            var plugins = PluginsBlock.Match(text);
            if (!buildScript.Success && !plugins.Success) return text;

            var major = AgpMajorVersion(text);
            if (major == null)
            {
                warning = UnknownVersionWarning;
                return text;
            }
            if (major >= 8) return text;

            return buildScript.Success ? AddToBuildScript(text, buildScript) : text.Insert(plugins.Index, BuildScript);
        }

        static int? AgpMajorVersion(string text)
        {
            var match = PluginVersion.Match(text);
            if (!match.Success) match = ClasspathVersion.Match(text);
            return match.Success ? int.Parse(match.Groups[1].Value) : (int?)null;
        }

        static string AddToBuildScript(string text, Match buildScript)
        {
            var close = ClosingBrace(text, buildScript.Index + buildScript.Length - 1);
            if (close < 0) return text;
            var body = text.Substring(buildScript.Index, close - buildScript.Index);

            var dependencies = Inner(DependenciesBlock, body);
            body = dependencies.close < 0
                ? InsertBeforeClose(body, body.Length, "    dependencies {\n        " + R8Classpath + "\n    }")
                : InsertBeforeClose(body, dependencies.close, "    " + R8Classpath);

            var repositories = Inner(RepositoriesBlock, body);
            if (repositories.close < 0)
                body = body.Insert(body.IndexOf('{') + 1, "\n    repositories {\n        google()\n    }");
            else if (!body.Substring(repositories.open, repositories.close - repositories.open).Contains("google()"))
                body = InsertBeforeClose(body, repositories.close, "    google()");

            return text.Substring(0, buildScript.Index) + body + text.Substring(close);
        }

        // Finds a block directly inside the buildscript body and returns its brace positions, or -1.
        static (int open, int close) Inner(Regex block, string body)
        {
            foreach (Match match in block.Matches(body))
            {
                var open = match.Index + match.Length - 1;
                if (Depth(body, open) != 1) continue;
                return (open, ClosingBrace(body, open));
            }
            return (-1, -1);
        }

        static int Depth(string text, int index)
        {
            var depth = 0;
            for (var i = 0; i < index; i++)
            {
                if (text[i] == '{') depth++;
                else if (text[i] == '}') depth--;
            }
            return depth;
        }

        static int ClosingBrace(string text, int open)
        {
            var depth = 0;
            for (var i = open; i < text.Length; i++)
            {
                if (text[i] == '{') depth++;
                else if (text[i] == '}' && --depth == 0) return i;
            }
            return -1;
        }

        // Inserts lines before a closing brace, keeping the brace on its own line.
        static string InsertBeforeClose(string body, int close, string lines)
        {
            var lineStart = body.LastIndexOf('\n', close - 1) + 1;
            var prefix = body.Substring(lineStart, close - lineStart);
            return prefix.Trim().Length == 0
                ? body.Insert(close, lines + "\n" + prefix)
                : body.Insert(close, "\n" + lines + "\n");
        }
    }
}
