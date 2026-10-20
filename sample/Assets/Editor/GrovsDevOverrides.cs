using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Points the Grovs pod at a local checkout when GROVS_IOS_POD_PATH is set.
/// Runs after EDM4U writes the Podfile (40) and before it installs the pods (50).
/// </summary>
public class GrovsDevOverrides : IPostprocessBuildWithReport
{
    const string PodPathVariable = "GROVS_IOS_POD_PATH";
    static readonly Regex GrovsPodLine = new Regex(@"^(\s*)pod 'Grovs'(,.*)?$", RegexOptions.Multiline);

    public int callbackOrder => 45;

    public void OnPostprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.iOS) return;
        var podPath = Environment.GetEnvironmentVariable(PodPathVariable);
        if (string.IsNullOrEmpty(podPath)) return;

        var podfile = Path.Combine(report.summary.outputPath, "Podfile");
        if (!File.Exists(podfile)) throw new BuildFailedException(PodPathVariable + " is set but no Podfile was generated.");

        var text = File.ReadAllText(podfile);
        if (!GrovsPodLine.IsMatch(text)) throw new BuildFailedException("The Podfile has no pod 'Grovs' line.");

        var fullPath = Path.GetFullPath(podPath).Replace("'", "\\'");
        File.WriteAllText(podfile, GrovsPodLine.Replace(text, "$1pod 'Grovs', :path => '" + fullPath + "'"));
        Debug.Log("[GrovsDevOverrides] pod 'Grovs' now uses " + fullPath);
    }
}
