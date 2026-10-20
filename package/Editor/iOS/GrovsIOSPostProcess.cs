#if UNITY_IOS || UNITY_EDITOR_OSX
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace GrovsSDK.Editor.iOS
{
    /// <summary>Writes the Grovs settings into the exported Xcode project. Runs before EDM4U.</summary>
    internal sealed class GrovsIOSPostProcess : IPostprocessBuildWithReport
    {
        const string DefaultEntitlementsPath = "Unity-iPhone/Unity-iPhone.entitlements";

        public int callbackOrder => 10;

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.iOS) return;

            var developmentBuild = (report.summary.options & BuildOptions.Development) != 0;
            var config = GrovsBuildConfig.Resolve(GrovsSettings.LoadOrCreate(), developmentBuild);
            foreach (var warning in config.Warnings()) Debug.LogWarning(warning);
            var errors = config.Validate();
            if (errors.Count > 0) throw new BuildFailedException(string.Join("\n", errors));

            var buildPath = report.summary.outputPath;
            WritePlist(buildPath, config);
            WriteProject(buildPath, config, developmentBuild);
            if (config.PushEnabled) EnableRemoteNotifications(buildPath);
        }

        static void WritePlist(string buildPath, GrovsBuildConfig config)
        {
            var path = Path.Combine(buildPath, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(path);
            GrovsPlistWriter.Apply(plist, config);
            plist.WriteToFile(path);
        }

        static void WriteProject(string buildPath, GrovsBuildConfig config, bool developmentBuild)
        {
            var projectPath = PBXProject.GetPBXProjectPath(buildPath);
            var proj = new PBXProject();
            proj.ReadFromFile(projectPath);
            GrovsXcodeProjectWriter.Apply(proj);
            if (GrovsEntitlementsWriter.HasChanges(config))
                WriteEntitlements(buildPath, proj, config, developmentBuild);
            proj.WriteToFile(projectPath);
        }

        static void WriteEntitlements(string buildPath, PBXProject proj, GrovsBuildConfig config, bool developmentBuild)
        {
            var main = proj.GetUnityMainTargetGuid();
            var relative = proj.GetBuildPropertyForAnyConfig(main, "CODE_SIGN_ENTITLEMENTS");
            if (string.IsNullOrEmpty(relative))
            {
                relative = DefaultEntitlementsPath;
                proj.SetBuildProperty(main, "CODE_SIGN_ENTITLEMENTS", relative);
                if (!proj.ContainsFileByProjectPath(relative)) proj.AddFile(relative, relative);
            }

            var path = Path.Combine(buildPath, relative.Replace("$(SRCROOT)/", "").Trim('"'));
            var ent = new PlistDocument();
            if (File.Exists(path)) ent.ReadFromFile(path);
            else ent.Create();
            GrovsEntitlementsWriter.Apply(ent, config, developmentBuild);
            ent.WriteToFile(path);
        }

        static void EnableRemoteNotifications(string buildPath)
        {
            var path = Path.Combine(buildPath, "Classes", "Preprocessor.h");
            if (!File.Exists(path))
            {
                Debug.LogWarning("[Grovs] Classes/Preprocessor.h was not found, so the push token may not reach Grovs.");
                return;
            }
            File.WriteAllText(path, GrovsXcodeProjectWriter.EnableRemoteNotifications(File.ReadAllText(path)));
        }
    }
}
#endif
