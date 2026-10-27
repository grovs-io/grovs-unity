using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GrovsSDK.Editor
{
    /// <summary>Warns once per Editor session when External Dependency Manager for Unity is missing.</summary>
    [InitializeOnLoad]
    internal static class GrovsEdm4uCheck
    {
        public const string Warning =
            "Grovs needs External Dependency Manager for Unity: https://github.com/googlesamples/unity-jar-resolver";
        const string WarnedKey = "GrovsSDK.Edm4uWarned";

        static GrovsEdm4uCheck()
        {
            if (Present || SessionState.GetBool(WarnedKey, false)) return;
            SessionState.SetBool(WarnedKey, true);
            Debug.LogWarning(Warning);
        }

        public static bool Present => FindType("Google.VersionHandler") != null;

        /// <summary>Runs the EDM4U Android Resolver now. Returns false when EDM4U is missing or resolution fails.</summary>
        public static bool ResolveAndroid()
        {
            var resolveSync = FindType("GooglePlayServices.PlayServicesResolver")?.GetMethod("ResolveSync", new[] { typeof(bool) });
            if (resolveSync == null) return false;
            return (bool)resolveSync.Invoke(null, new object[] { false });
        }

        static Type FindType(string fullName) =>
            AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType(fullName))
                .FirstOrDefault(t => t != null);
    }
}
