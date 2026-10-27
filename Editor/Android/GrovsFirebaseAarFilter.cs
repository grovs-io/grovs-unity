using System.IO;
using System.Linq;
using UnityEditor;

namespace GrovsSDK.Editor.Android
{
    /// <summary>
    /// Decides at build time whether the Firebase Unity AAR goes into the build.
    /// Nothing is written to its importer, so this works for read-only packages too.
    /// </summary>
    [InitializeOnLoad]
    internal static class GrovsFirebaseAarFilter
    {
        const string AarName = "grovs-unity-firebase.aar";

        static GrovsFirebaseAarFilter() => Register();

        public static void Register()
        {
            var importer = PluginImporter.GetAllImporters()
                .FirstOrDefault(i => Path.GetFileName(i.assetPath) == AarName);
            importer?.SetIncludeInBuildDelegate(_ => GrovsAndroidBuildProcessors.ShouldIncludeFirebaseAar(
                GrovsAndroidLibGenerator.ResolvePushMode(ConfiguredPushMode())));
        }

        static GrovsAndroidPushMode ConfiguredPushMode()
        {
            var settings = AssetDatabase.LoadAssetAtPath<GrovsSettings>(GrovsSettings.AssetPath);
            return settings != null ? settings.androidPushMode : GrovsAndroidPushMode.Auto;
        }
    }
}
