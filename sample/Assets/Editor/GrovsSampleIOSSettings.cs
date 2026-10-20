#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.iOS.Xcode;

/// <summary>Sample-only Info.plist values for the smoke build.</summary>
public class GrovsSampleIOSSettings : IPostprocessBuildWithReport
{
    const string ApiKey = "GROVS_SAMPLE_PLACEHOLDER_KEY";
    const string UrlScheme = "grovssample";

    public int callbackOrder => 10;

    public void OnPostprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.iOS) return;

        var plistPath = Path.Combine(report.summary.outputPath, "Info.plist");
        var plist = new PlistDocument();
        plist.ReadFromFile(plistPath);
        var root = plist.root;
        root.SetString("GrovsApiKey", ApiKey);

        var urlTypes = root["CFBundleURLTypes"]?.AsArray() ?? root.CreateArray("CFBundleURLTypes");
        var urlType = urlTypes.AddDict();
        urlType.SetString("CFBundleURLName", PlayerSettings.applicationIdentifier);
        urlType.SetString("CFBundleTypeRole", "Editor");
        urlType.CreateArray("CFBundleURLSchemes").AddString(UrlScheme);
        plist.WriteToFile(plistPath);
    }
}
#endif
