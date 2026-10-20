#if UNITY_IOS || UNITY_EDITOR_OSX
using System.Text.RegularExpressions;
using UnityEditor.iOS.Xcode;

namespace GrovsSDK.Editor.iOS
{
    /// <summary>Xcode settings the Swift bridge in UnityFramework needs.</summary>
    internal static class GrovsXcodeProjectWriter
    {
        static readonly Regex RemoteNotificationsDefine =
            new Regex(@"^(#define[ \t]+UNITY_USES_REMOTE_NOTIFICATIONS[ \t]+)0\b", RegexOptions.Multiline);

        public static void Apply(PBXProject proj)
        {
            var framework = proj.GetUnityFrameworkTargetGuid();
            foreach (var name in proj.BuildConfigNames())
            {
                var config = proj.BuildConfigByName(framework, name);
                if (config != null && string.IsNullOrEmpty(proj.GetBuildPropertyForConfig(config, "SWIFT_VERSION")))
                    proj.SetBuildPropertyForConfig(config, "SWIFT_VERSION", "5.0");
            }
            proj.SetBuildProperty(framework, "CLANG_ENABLE_MODULES", "YES");
            proj.SetBuildProperty(framework, "DEFINES_MODULE", "YES");
            proj.SetBuildProperty(framework, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "NO");
            proj.SetBuildProperty(proj.GetUnityMainTargetGuid(), "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES", "YES");
        }

        /// <summary>Turns on Unity's remote notification callbacks so the push token reaches the bridge.</summary>
        public static string EnableRemoteNotifications(string preprocessorHeader) =>
            RemoteNotificationsDefine.Replace(preprocessorHeader, "${1}1");
    }
}
#endif
