using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>Batchmode build entry points. Each one exits with code 1 on failure.</summary>
public static class GrovsSampleBuild
{
    static string RepoRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));

    public static void BuildAndroid()
    {
        Run(() =>
        {
            ResolveAndroidDependencies();
            Build(BuildTarget.Android, Path.Combine(RepoRoot, "build", "android", "sample.apk"));
        });
    }

    public static void BuildIOS()
    {
        Run(() => Build(BuildTarget.iOS, Path.Combine(RepoRoot, "build", "ios")));
    }

    static void Build(BuildTarget target, string outputPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (scenes.Length == 0) throw new InvalidOperationException("No scenes are enabled in Build Settings.");

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = target,
            options = BuildOptions.None
        });

        var summary = report.summary;
        Debug.Log("[GrovsSampleBuild] " + target + " " + summary.result + ", " + summary.totalErrors +
                  " errors, output " + outputPath);
        if (summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException(target + " build " + summary.result + ".");
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
