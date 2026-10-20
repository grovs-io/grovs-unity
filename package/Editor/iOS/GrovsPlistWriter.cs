#if UNITY_IOS || UNITY_EDITOR_OSX
using System.Linq;
using UnityEditor.iOS.Xcode;

namespace GrovsSDK.Editor.iOS
{
    /// <summary>Writes the Grovs keys, URL type and push background mode into Info.plist.</summary>
    internal static class GrovsPlistWriter
    {
        const string UrlTypeName = "io.grovs";

        public static void Apply(PlistDocument plist, GrovsBuildConfig c)
        {
            var root = plist.root;
            root.SetString("GrovsApiKey", c.ApiKey);
            root.SetBoolean("GrovsUseTestEnvironment", c.UseTestEnvironment);
            if (c.BaseUrl.Length > 0) root.SetString("GrovsBaseURL", c.BaseUrl);
            else root.values.Remove("GrovsBaseURL");
            root.SetBoolean("GrovsEnabled", c.StartEnabled);
            var clipboard = root.CreateArray("GrovsClipboardDomains");
            foreach (var domain in c.ClipboardDomains) clipboard.AddString(domain);
            root.SetString("GrovsLogLevel", c.LogLevelName);
            root.SetBoolean("GrovsUnityTrackScenes", c.TrackScenes);
            root.SetBoolean("GrovsUnityOwnNotificationDelegate", c.IosOwnNotificationDelegate);

            WriteUrlType(root, c.UriScheme);
            if (c.PushEnabled) GrovsPlistMerge.AddUnique(root, "UIBackgroundModes", "remote-notification");
        }

        static void WriteUrlType(PlistElementDict root, string scheme)
        {
            var urlTypes = GrovsPlistMerge.GetOrCreateArray(root, "CFBundleURLTypes");
            urlTypes.values.RemoveAll(IsGrovsUrlType);
            var urlType = urlTypes.AddDict();
            urlType.SetString("CFBundleURLName", UrlTypeName);
            urlType.SetString("CFBundleTypeRole", "Editor");
            urlType.CreateArray("CFBundleURLSchemes").AddString(scheme);
        }

        static bool IsGrovsUrlType(PlistElement element)
        {
            var dict = element as PlistElementDict;
            return dict != null && dict.values.TryGetValue("CFBundleURLName", out var name) &&
                   name is PlistElementString text && text.value == UrlTypeName;
        }
    }

    internal static class GrovsPlistMerge
    {
        public static PlistElementArray GetOrCreateArray(PlistElementDict dict, string key)
        {
            return dict.values.TryGetValue(key, out var existing) && existing is PlistElementArray array
                ? array
                : dict.CreateArray(key);
        }

        public static void AddUnique(PlistElementDict dict, string key, string value)
        {
            var array = GetOrCreateArray(dict, key);
            if (!array.values.OfType<PlistElementString>().Any(v => v.value == value)) array.AddString(value);
        }
    }
}
#endif
