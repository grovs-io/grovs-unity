using System.IO;
using NUnit.Framework;
using UnityEngine;
using GrovsSDK.Editor;
#if UNITY_IOS || UNITY_EDITOR_OSX
using System.Linq;
using UnityEditor.iOS.Xcode;
using GrovsSDK.Editor.iOS;
#endif

namespace GrovsSDK.Tests
{
    public class GrovsIOSWritersTests
    {
        const string BasePlist =
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
            "<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">\n" +
            "<plist version=\"1.0\">\n<dict>\n<key>CFBundleIdentifier</key>\n<string>io.grovs.sample</string>\n</dict>\n</plist>\n";

        GrovsSettings _settings;

        static string GoldenPath(string name) =>
            Path.GetFullPath("Packages/com.grovs.sdk/Tests/Editor/Golden/" + name);

        static string Normalize(string text) => text.Replace("\r\n", "\n").Trim();

        [SetUp]
        public void SetUp()
        {
            _settings = ScriptableObject.CreateInstance<GrovsSettings>();
            _settings.apiKey = "test-key";
            _settings.environment = GrovsEnvironment.Production;
            _settings.clipboardDomains = new[] { "a.sqd.link", "b.sqd.link" };
            _settings.uriScheme = "grovssample";
            _settings.linkDomains = new[] { "a.sqd.link", "a.test-sqd.link" };
            _settings.logLevel = GrovsLogLevel.Warn;
            _settings.trackScenes = true;
            _settings.iosOwnNotificationDelegate = false;
        }

        [TearDown]
        public void TearDown()
        {
            if (_settings != null) Object.DestroyImmediate(_settings);
        }

#if UNITY_IOS || UNITY_EDITOR_OSX
        GrovsBuildConfig Config(bool developmentBuild = false) => GrovsBuildConfig.Resolve(_settings, developmentBuild);

        static PlistDocument Plist(string xml = BasePlist)
        {
            var plist = new PlistDocument();
            plist.ReadFromString(xml);
            return plist;
        }

        [Test]
        public void Plist_WritesKeysAndUrlType()
        {
            var plist = Plist();
            GrovsPlistWriter.Apply(plist, Config());
            Assert.AreEqual(Normalize(File.ReadAllText(GoldenPath("Info.expected.plist"))),
                Normalize(plist.WriteToString()));
        }

        [Test]
        public void Plist_BaseUrl_WrittenOnlyWhenSet()
        {
            _settings.baseUrl = " https://api.example.com ";
            var plist = Plist();
            GrovsPlistWriter.Apply(plist, Config());
            Assert.AreEqual("https://api.example.com", plist.root["GrovsBaseURL"].AsString());

            _settings.baseUrl = "";
            GrovsPlistWriter.Apply(plist, Config());
            Assert.IsFalse(plist.root.values.ContainsKey("GrovsBaseURL"));
        }

        [Test]
        public void Plist_MergesExistingUrlTypes_NoDuplicates()
        {
            var plist = Plist();
            var fb = plist.root.CreateArray("CFBundleURLTypes").AddDict();
            fb.SetString("CFBundleURLName", "facebook");
            fb.CreateArray("CFBundleURLSchemes").AddString("fb123");

            GrovsPlistWriter.Apply(plist, Config());
            var first = plist.WriteToString();
            GrovsPlistWriter.Apply(plist, Config());
            Assert.AreEqual(first, plist.WriteToString());

            var types = plist.root["CFBundleURLTypes"].AsArray().values.Select(v => v.AsDict()).ToList();
            Assert.AreEqual(2, types.Count);
            Assert.AreEqual("fb123", types[0]["CFBundleURLSchemes"].AsArray().values[0].AsString());
            Assert.AreEqual(1, types.Count(t => t.values.ContainsKey("CFBundleURLName") &&
                                                t["CFBundleURLName"].AsString() == "io.grovs"));
        }

        [Test]
        public void Entitlements_AssociatedDomains_Merged()
        {
            var ent = new PlistDocument();
            ent.Create();
            ent.root.CreateArray("com.apple.developer.associated-domains").AddString("applinks:old.com");

            GrovsEntitlementsWriter.Apply(ent, Config(), false);
            GrovsEntitlementsWriter.Apply(ent, Config(), false);

            var domains = ent.root["com.apple.developer.associated-domains"].AsArray().values
                .Select(v => v.AsString()).ToList();
            CollectionAssert.AreEqual(new[] { "applinks:old.com", "applinks:a.sqd.link", "applinks:a.test-sqd.link" },
                domains);
            Assert.IsFalse(ent.root.values.ContainsKey("aps-environment"));
            Assert.AreEqual(Normalize(File.ReadAllText(GoldenPath("app.expected.entitlements"))),
                Normalize(ent.WriteToString()));
        }

        [Test]
        public void Entitlements_Push()
        {
            _settings.pushEnabled = true;

            var dev = new PlistDocument();
            dev.Create();
            GrovsEntitlementsWriter.Apply(dev, Config(true), true);
            Assert.AreEqual("development", dev.root["aps-environment"].AsString());

            var release = new PlistDocument();
            release.Create();
            GrovsEntitlementsWriter.Apply(release, Config(), false);
            Assert.AreEqual("production", release.root["aps-environment"].AsString());

            var plist = Plist();
            plist.root.CreateArray("UIBackgroundModes").AddString("audio");
            GrovsPlistWriter.Apply(plist, Config());
            GrovsPlistWriter.Apply(plist, Config());
            CollectionAssert.AreEqual(new[] { "audio", "remote-notification" },
                plist.root["UIBackgroundModes"].AsArray().values.Select(v => v.AsString()).ToList());
        }

        [Test]
        public void Plist_PushOff_NoBackgroundMode()
        {
            var plist = Plist();
            GrovsPlistWriter.Apply(plist, Config());
            Assert.IsFalse(plist.root.values.ContainsKey("UIBackgroundModes"));
        }

        [Test]
        public void Xcode_SwiftSettings()
        {
            var proj = new PBXProject();
            proj.ReadFromFile(GoldenPath("project.pbxproj"));
            var framework = proj.GetUnityFrameworkTargetGuid();
            var main = proj.GetUnityMainTargetGuid();
            proj.SetBuildProperty(framework, "SWIFT_VERSION", "");
            proj.SetBuildProperty(framework, "CLANG_ENABLE_MODULES", "NO");

            GrovsXcodeProjectWriter.Apply(proj);

            foreach (var name in proj.BuildConfigNames())
            {
                var fw = proj.BuildConfigByName(framework, name);
                Assert.AreEqual("5.0", proj.GetBuildPropertyForConfig(fw, "SWIFT_VERSION"), name);
                Assert.AreEqual("YES", proj.GetBuildPropertyForConfig(fw, "CLANG_ENABLE_MODULES"), name);
                Assert.AreEqual("YES", proj.GetBuildPropertyForConfig(fw, "DEFINES_MODULE"), name);
                Assert.AreEqual("NO", proj.GetBuildPropertyForConfig(fw, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES"), name);

                var app = proj.BuildConfigByName(main, name);
                Assert.AreEqual("YES", proj.GetBuildPropertyForConfig(app, "ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES"), name);
            }

            proj.SetBuildProperty(framework, "SWIFT_VERSION", "5.9");
            GrovsXcodeProjectWriter.Apply(proj);
            Assert.AreEqual("5.9", proj.GetBuildPropertyForAnyConfig(framework, "SWIFT_VERSION"));
        }

        [Test]
        public void Preprocessor_EnablesRemoteNotifications()
        {
            const string header =
                "#define UNITY_USES_REMOTE_NOTIFICATIONS 0\n#define UNITY_USES_WEBCAM 0\n";
            var once = GrovsXcodeProjectWriter.EnableRemoteNotifications(header);
            Assert.AreEqual("#define UNITY_USES_REMOTE_NOTIFICATIONS 1\n#define UNITY_USES_WEBCAM 0\n", once);
            Assert.AreEqual(once, GrovsXcodeProjectWriter.EnableRemoteNotifications(once));
        }
#else
        [Test]
        public void IOSWriters_NeedXcodeApi()
        {
            Assume.That(false, "The iOS Xcode API is not available in this editor.");
        }
#endif
    }
}
