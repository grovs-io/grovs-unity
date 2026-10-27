#if UNITY_IOS || UNITY_EDITOR_OSX
using UnityEditor.iOS.Xcode;

namespace GrovsSDK.Editor.iOS
{
    /// <summary>Merges associated domains and the push environment into the app entitlements.</summary>
    internal static class GrovsEntitlementsWriter
    {
        public static bool HasChanges(GrovsBuildConfig c) => c.LinkDomains.Count > 0 || c.PushEnabled;

        public static void Apply(PlistDocument ent, GrovsBuildConfig c, bool developmentBuild)
        {
            foreach (var domain in c.LinkDomains)
                GrovsPlistMerge.AddUnique(ent.root, "com.apple.developer.associated-domains", "applinks:" + domain);
            if (c.PushEnabled)
                ent.root.SetString("aps-environment", developmentBuild ? "development" : "production");
        }
    }
}
#endif
