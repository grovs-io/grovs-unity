using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace GrovsSDK.Editor
{
    /// <summary>Resolves the settings for a build, logs warnings and fails the build on errors.</summary>
    internal static class GrovsBuildValidation
    {
        public static GrovsBuildConfig Run(BuildReport report)
        {
            var config = GrovsBuildConfig.Resolve(GrovsSettings.LoadOrCreate(), IsDevelopment(report));
            foreach (var warning in config.Warnings()) Debug.LogWarning(warning);
            var errors = config.Validate();
            if (errors.Count > 0) throw new BuildFailedException(string.Join("\n", errors));
            return config;
        }

        public static bool IsDevelopment(BuildReport report) =>
            (report.summary.options & BuildOptions.Development) != 0;
    }

    internal sealed class GrovsIOSPreprocess : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform == BuildTarget.iOS) GrovsBuildValidation.Run(report);
        }
    }
}
