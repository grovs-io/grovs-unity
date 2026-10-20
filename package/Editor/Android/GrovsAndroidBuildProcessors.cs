using System;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace GrovsSDK.Editor.Android
{
    /// <summary>Generates the GrovsConfig library before an Android build and patches the Gradle project after export.</summary>
    internal sealed class GrovsAndroidBuildProcessors : IPreprocessBuildWithReport, IPostGenerateGradleAndroidProject
    {
        public const string LibraryPath = "Assets/Plugins/Android/GrovsConfig.androidlib";
        const int MinTargetApi = 34;

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.Android) return;

            var config = GrovsBuildValidation.Run(report);
            var apiError = CheckTargetApi(PlayerSettings.Android.targetSdkVersion, HighestInstalledApi());
            if (apiError != null) throw new BuildFailedException(apiError);

            var pushModeError = CheckPushMode(config.AndroidPushMode, GrovsAndroidLibGenerator.FirebaseUnityPresent());
            if (pushModeError != null) throw new BuildFailedException(pushModeError);

            var pushMode = GrovsAndroidLibGenerator.ResolvePushMode(config.AndroidPushMode);
            var pushWarning = PushWarning(config, pushMode);
            if (pushWarning != null) Debug.LogWarning(pushWarning);
            GrovsFirebaseAarFilter.Register();

            GrovsAndroidLibGenerator.Generate(LibraryPath, config);
            AssetDatabase.ImportAsset(LibraryPath, ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
            SyncPushDependencies(config, pushMode);
        }

        // EDM4U resolves on build only when its auto-resolve setting is on, so a changed file is resolved here.
        static void SyncPushDependencies(GrovsBuildConfig config, GrovsAndroidPushMode pushMode)
        {
            var path = GrovsAndroidLibGenerator.PushDependenciesPath;
            if (!GrovsAndroidLibGenerator.WritePushDependencies(path, config, pushMode)) return;
            if (File.Exists(path)) AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            if (GrovsEdm4uCheck.Present && !GrovsEdm4uCheck.ResolveAndroid())
                throw new BuildFailedException("Grovs: EDM4U Android dependency resolution failed after updating " + path + ".");
        }

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            var config = GrovsBuildConfig.Resolve(GrovsSettings.LoadOrCreate(), EditorUserBuildSettings.development);

            var manifestPath = Path.Combine(path, "src", "main", "AndroidManifest.xml");
            var manifest = new XmlDocument();
            manifest.Load(manifestPath);
            GrovsLauncherIntentFilters.Apply(manifest, config);
            GrovsAndroidXml.Save(manifest, manifestPath);

            var root = Directory.GetParent(path).FullName;
            Rewrite(Path.Combine(root, "gradle.properties"), GrovsGradlePropertiesWriter.Apply);
            Rewrite(Path.Combine(root, "build.gradle"), GrovsRootGradlePatcher.Apply);
        }

        public static string CheckTargetApi(AndroidSdkVersions level, int highestInstalled)
        {
            var target = level == AndroidSdkVersions.AndroidApiLevelAuto ? highestInstalled : (int)level;
            if (target <= 0 || target >= MinTargetApi) return null;
            return "Grovs: Android target API level must be " + MinTargetApi + " or higher.";
        }

        public static string PushWarning(GrovsBuildConfig c, GrovsAndroidPushMode resolvedMode)
        {
            if (!c.PushEnabled || resolvedMode != GrovsAndroidPushMode.Standalone || c.GoogleServicesJsonPath.Length > 0)
                return null;
            return "Grovs: push is on in Standalone mode but the google-services.json path is empty " +
                   "(Project Settings > Grovs), so Firebase will not start.";
        }

        public static string CheckPushMode(GrovsAndroidPushMode configured, bool firebaseUnityPresent)
        {
            if (configured != GrovsAndroidPushMode.FirebaseUnity || firebaseUnityPresent) return null;
            return "Grovs: Android push mode is Firebase Unity but Firebase Messaging is not installed.";
        }

        // The Firebase Unity AAR extends a Firebase Unity class, so it only builds when Firebase Unity is there.
        public static bool ShouldIncludeFirebaseAar(GrovsAndroidPushMode resolvedMode) =>
            resolvedMode == GrovsAndroidPushMode.FirebaseUnity;

        static void Rewrite(string path, Func<string, string> transform)
        {
            var text = File.Exists(path) ? File.ReadAllText(path) : "";
            var updated = transform(text);
            if (updated != text) File.WriteAllText(path, updated);
        }

        // Returns 0 when the SDK cannot be found, which skips the check.
        static int HighestInstalledApi()
        {
            var platforms = Path.Combine(AndroidSdkRoot(), "platforms");
            if (!Directory.Exists(platforms)) return 0;
            return Directory.GetDirectories(platforms)
                .Select(d => Path.GetFileName(d).Replace("android-", ""))
                .Select(n => int.TryParse(n, out var api) ? api : 0)
                .DefaultIfEmpty(0)
                .Max();
        }

        static string AndroidSdkRoot()
        {
            var settings = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("UnityEditor.Android.AndroidExternalToolsSettings"))
                .FirstOrDefault(t => t != null);
            var configured = settings?.GetProperty("sdkRootPath")?.GetValue(null) as string;
            if (!string.IsNullOrEmpty(configured)) return configured;
            return Path.Combine(BuildPipeline.GetPlaybackEngineDirectory(BuildTarget.Android, BuildOptions.None), "SDK");
        }
    }
}
