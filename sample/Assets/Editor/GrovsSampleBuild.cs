using System;
using System.IO;
using System.Linq;
using GrovsSDK.Editor;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Batchmode build entry points. Each one exits with code 1 on failure.
/// These values, when set, change the settings for that build only and are never saved:
/// GROVS_TEST_KEY, GROVS_PROD_KEY, GROVS_URI_SCHEME, GROVS_LINK_DOMAINS (comma list), GROVS_BASE_URL,
/// GROVS_CLIPBOARD_DOMAINS (comma list),
/// GROVS_ANDROID_BACKEND (IL2CPP or Mono) and GROVS_ANDROID_MINIFY (true or 1).
/// Each is read from the environment, or else from a "-NAME value" command line argument.
/// </summary>
public static class GrovsSampleBuild
{
    const BuildOptions Options = BuildOptions.None;

    static string RepoRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));

    public static void BuildAndroid()
    {
        // EDM4U writes ABI exclusions into mainTemplate.gradle, so it resolves after the overrides are applied.
        Run(() => WithAndroidPlayerOverrides(() =>
        {
            ResolveAndroidDependencies();
            WithUnity6MainTemplate(() =>
                Build(BuildTarget.Android, Path.Combine(RepoRoot, "build", "android", "sample.apk")));
        }));
    }

    const string MainTemplatePath = "Assets/Plugins/Android/mainTemplate.gradle";
    const string DefaultConfigToken = "**DEFAULT_CONFIG_SETUP**";

    // Unity 6 needs this token in defaultConfig, and Unity 2022.3 does not know it, so it is added for the build only.
    static void WithUnity6MainTemplate(Action build)
    {
#if UNITY_6000_0_OR_NEWER
        var original = File.ReadAllText(MainTemplatePath);
        if (original.Contains(DefaultConfigToken))
        {
            build();
            return;
        }
        File.WriteAllText(MainTemplatePath, WithDefaultConfigToken(original));
        try
        {
            build();
        }
        finally
        {
            File.WriteAllText(MainTemplatePath, original);
        }
#else
        build();
#endif
    }

    static string WithDefaultConfigToken(string template)
    {
        var lines = template.Split('\n').ToList();
        var index = lines.FindIndex(l => l.Contains("consumerProguardFiles"));
        if (index < 0) throw new InvalidOperationException("mainTemplate.gradle has no consumerProguardFiles line.");
        var indent = lines[index].Substring(0, lines[index].Length - lines[index].TrimStart().Length);
        lines.Insert(index + 1, indent + DefaultConfigToken);
        return string.Join("\n", lines);
    }

    public static void BuildIOS()
    {
        Run(() => Build(BuildTarget.iOS, IOSOutputPath));
    }

    public static void BuildIOSSimulator()
    {
        Run(() =>
        {
            var previous = PlayerSettings.iOS.sdkVersion;
            PlayerSettings.iOS.sdkVersion = iOSSdkVersion.SimulatorSDK;
            try
            {
                Build(BuildTarget.iOS, IOSOutputPath);
            }
            finally
            {
                PlayerSettings.iOS.sdkVersion = previous;
            }
        });
    }

    static string IOSOutputPath => Path.Combine(RepoRoot, "build", "ios");

    static void Build(BuildTarget target, string outputPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (scenes.Length == 0) throw new InvalidOperationException("No scenes are enabled in Build Settings.");

        BuildReport report = null;
        WithGrovsSettingsOverrides(() => report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = target,
            options = Options
        }));

        var summary = report.summary;
        Debug.Log("[GrovsSampleBuild] " + target + " " + summary.result + ", " + summary.totalErrors +
                  " errors, output " + outputPath);
        if (summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException(target + " build " + summary.result + ".");
    }

    static void WithGrovsSettingsOverrides(Action build)
    {
        var settings = GrovsSettings.LoadOrCreate();
        var saved = (settings.apiKey, settings.uriScheme, settings.linkDomains, settings.baseUrl,
            settings.clipboardDomains, settings.hideFlags);
        // The build unloads unused assets, which would reload the settings from disk and drop the overrides.
        settings.hideFlags |= HideFlags.DontUnloadUnusedAsset;
        try
        {
            var development = (Options & BuildOptions.Development) != 0;
            var useTest = settings.environment == GrovsEnvironment.Test ||
                          (settings.environment == GrovsEnvironment.TestForDevelopmentBuilds && development);
            var key = Env(useTest ? "GROVS_TEST_KEY" : "GROVS_PROD_KEY");
            if (key != null) settings.apiKey = key;
            var scheme = Env("GROVS_URI_SCHEME");
            if (scheme != null) settings.uriScheme = scheme;
            var domains = Env("GROVS_LINK_DOMAINS");
            if (domains != null) settings.linkDomains = SplitList(domains);
            var baseUrl = Env("GROVS_BASE_URL");
            if (baseUrl != null) settings.baseUrl = baseUrl;
            var clipboardDomains = Env("GROVS_CLIPBOARD_DOMAINS");
            if (clipboardDomains != null) settings.clipboardDomains = SplitList(clipboardDomains);
            build();
        }
        finally
        {
            (settings.apiKey, settings.uriScheme, settings.linkDomains, settings.baseUrl,
                settings.clipboardDomains, settings.hideFlags) = saved;
        }
    }

    static string[] SplitList(string value) =>
        value.Split(',').Select(d => d.Trim()).Where(d => d.Length > 0).ToArray();

    static void WithAndroidPlayerOverrides(Action build)
    {
        var backend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android);
        var architectures = PlayerSettings.Android.targetArchitectures;
        var minify = PlayerSettings.Android.minifyRelease;
        try
        {
            var requestedBackend = Env("GROVS_ANDROID_BACKEND");
            if (requestedBackend != null)
            {
                var mono = requestedBackend.Equals("Mono", StringComparison.OrdinalIgnoreCase);
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android,
                    mono ? ScriptingImplementation.Mono2x : ScriptingImplementation.IL2CPP);
                // Mono only builds 32-bit ARM.
                PlayerSettings.Android.targetArchitectures = mono ? AndroidArchitecture.ARMv7 : AndroidArchitecture.ARM64;
            }
            var requestedMinify = Env("GROVS_ANDROID_MINIFY");
            if (requestedMinify != null) PlayerSettings.Android.minifyRelease = requestedMinify == "1" ||
                requestedMinify.Equals("true", StringComparison.OrdinalIgnoreCase);
            Debug.Log("[GrovsSampleBuild] Android " + PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) +
                      ", minify " + PlayerSettings.Android.minifyRelease);
            build();
        }
        finally
        {
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, backend);
            PlayerSettings.Android.targetArchitectures = architectures;
            PlayerSettings.Android.minifyRelease = minify;
        }
    }

    static string Env(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(value))
        {
            var args = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(args, "-" + name);
            // An empty CI secret leaves the flag with no value, so the next flag must not be taken as one.
            value = index >= 0 && index + 1 < args.Length && !args[index + 1].StartsWith("-") ? args[index + 1] : null;
        }
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    // EDM4U writes the Maven dependencies into the custom Gradle templates.
    static void ResolveAndroidDependencies()
    {
        var resolver = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType("GooglePlayServices.PlayServicesResolver"))
            .FirstOrDefault(t => t != null);
        if (resolver == null) throw new InvalidOperationException("EDM4U Android Resolver is not loaded.");

        var resolveSync = resolver.GetMethod("ResolveSync", new[] { typeof(bool) });
        if (resolveSync == null) throw new InvalidOperationException("EDM4U ResolveSync(bool) was not found.");
        if (!(bool)resolveSync.Invoke(null, new object[] { true }))
            throw new InvalidOperationException("EDM4U Android dependency resolution failed.");
    }

    static void Run(Action build)
    {
        try
        {
            build();
        }
        catch (Exception e)
        {
            Debug.LogError("[GrovsSampleBuild] " + e);
            EditorApplication.Exit(1);
        }
    }
}
