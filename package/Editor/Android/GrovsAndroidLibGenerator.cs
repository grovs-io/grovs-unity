using System;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEditor;
using UnityEditor.Build;

namespace GrovsSDK.Editor.Android
{
    /// <summary>Writes the GrovsConfig Android library that carries the Grovs meta-data and push wiring.</summary>
    internal static class GrovsAndroidLibGenerator
    {
        const string LibraryPackage = "io.grovs.unity.config";
        const string FirebaseUnityMessagingType = "Firebase.Messaging.FirebaseMessaging, Firebase.Messaging";

        public static void Generate(string dir, GrovsBuildConfig c) =>
            Generate(dir, c, PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android));

        public static void Generate(string dir, GrovsBuildConfig c, string packageName)
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
            Directory.CreateDirectory(dir);

            var pushMode = ResolvePushMode(c.AndroidPushMode);
            GrovsAndroidXml.Save(Manifest(c, pushMode), Path.Combine(dir, "AndroidManifest.xml"));
            File.WriteAllText(Path.Combine(dir, "project.properties"), "android.library=true\n");

            if (c.PushEnabled && pushMode == GrovsAndroidPushMode.Standalone && c.GoogleServicesJsonPath.Length > 0)
            {
                var values = Path.Combine(dir, "res", "values");
                Directory.CreateDirectory(values);
                var json = File.ReadAllText(c.GoogleServicesJsonPath);
                File.WriteAllText(Path.Combine(values, "google-services.xml"),
                    GrovsGoogleServicesConverter.Convert(json, packageName));
            }
        }

        public static GrovsAndroidPushMode ResolvePushMode(GrovsAndroidPushMode mode) =>
            ResolvePushMode(mode, FirebaseUnityPresent());

        public static GrovsAndroidPushMode ResolvePushMode(GrovsAndroidPushMode mode, bool firebaseUnityPresent)
        {
            if (mode != GrovsAndroidPushMode.Auto) return mode;
            return firebaseUnityPresent ? GrovsAndroidPushMode.FirebaseUnity : GrovsAndroidPushMode.Standalone;
        }

        static bool FirebaseUnityPresent() =>
            Type.GetType(FirebaseUnityMessagingType) != null ||
            AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetType("Firebase.Messaging.FirebaseMessaging") != null);

        static XmlDocument Manifest(GrovsBuildConfig c, GrovsAndroidPushMode pushMode)
        {
            var doc = new XmlDocument();
            doc.AppendChild(doc.CreateXmlDeclaration("1.0", "utf-8", null));
            var manifest = doc.CreateElement("manifest");
            manifest.SetAttribute("xmlns:android", GrovsAndroidXml.AndroidNs);
            manifest.SetAttribute("xmlns:tools", GrovsAndroidXml.ToolsNs);
            manifest.SetAttribute("package", LibraryPackage);
            doc.AppendChild(manifest);

            if (c.PushEnabled)
                manifest.AppendChild(GrovsAndroidXml.Element(doc, "uses-permission",
                    "name", "android.permission.POST_NOTIFICATIONS"));

            var application = doc.CreateElement("application");
            manifest.AppendChild(application);

            void MetaData(string name, string value) =>
                application.AppendChild(GrovsAndroidXml.Element(doc, "meta-data", "name", name, "value", value));

            MetaData("grovs_api_key", c.ApiKey);
            MetaData("grovs_use_test_environment", Bool(c.UseTestEnvironment));
            if (c.BaseUrl.Length > 0) MetaData("grovs_base_url", c.BaseUrl);
            MetaData("grovs_enabled", Bool(c.StartEnabled));
            if (c.ClipboardDomains.Count > 0) MetaData("grovs_clipboard_domains", string.Join(",", c.ClipboardDomains));
            MetaData("grovs_log_level", c.LogLevelName);
            MetaData("grovs_unity_track_scenes", Bool(c.TrackScenes));
            if (c.AndroidSmallIcon.Length > 0) MetaData("io.grovs.NotificationIconSmall", c.AndroidSmallIcon);

            if (c.PushEnabled && pushMode == GrovsAndroidPushMode.FirebaseUnity) AddFirebaseUnityServices(application);
            return doc;
        }

        // Android delivers FCM messages to one service only, so ours replaces both listeners.
        static void AddFirebaseUnityServices(XmlElement application)
        {
            var doc = application.OwnerDocument;
            foreach (var name in new[] { "com.google.firebase.messaging.cpp.ListenerService", "io.grovs.handlers.MessagingService" })
            {
                var removed = GrovsAndroidXml.Element(doc, "service", "name", name);
                GrovsAndroidXml.SetTools(removed, "node", "remove");
                application.AppendChild(removed);
            }

            var service = GrovsAndroidXml.Element(doc, "service",
                "name", "io.grovs.unity.GrovsFirebaseUnityService", "exported", "false");
            var filter = doc.CreateElement("intent-filter");
            filter.AppendChild(GrovsAndroidXml.Element(doc, "action", "name", "com.google.firebase.MESSAGING_EVENT"));
            service.AppendChild(filter);
            application.AppendChild(service);
        }

        static string Bool(bool value) => value ? "true" : "false";
    }
}
