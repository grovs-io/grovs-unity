using System;
using System.IO;
using System.Linq;
using System.Xml;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using GrovsSDK.Editor;
using GrovsSDK.Editor.Android;

namespace GrovsSDK.Tests
{
    public class GrovsAndroidWritersTests
    {
        const string AndroidNs = "http://schemas.android.com/apk/res/android";
        const string ToolsNs = "http://schemas.android.com/tools";

        GrovsSettings _settings;
        string _dir;

        static string GoldenPath(string name) =>
            Path.GetFullPath("Packages/com.grovs.sdk/Tests/Editor/Golden/" + name);

        static string Golden(string name) => Normalize(File.ReadAllText(GoldenPath(name)));

        static string Normalize(string text) => text.Replace("\r\n", "\n").Trim();

        [SetUp]
        public void SetUp()
        {
            _settings = ScriptableObject.CreateInstance<GrovsSettings>();
            _settings.apiKey = "test-key";
            _settings.environment = GrovsEnvironment.Production;
            _settings.baseUrl = "https://api.example.com";
            _settings.clipboardDomains = new[] { "a.sqd.link", "b.sqd.link" };
            _settings.uriScheme = "grovssample";
            _settings.linkDomains = new[] { "a.sqd.link", "a.test-sqd.link" };
            _settings.logLevel = GrovsLogLevel.Warn;
            _settings.trackScenes = true;
            _settings.pushEnabled = true;
            _settings.androidPushMode = GrovsAndroidPushMode.Standalone;
            _settings.androidSmallIcon = "ic_grovs";
            _dir = Path.Combine(Path.GetTempPath(), "GrovsAndroidTests_" + Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            if (_settings != null) UnityEngine.Object.DestroyImmediate(_settings);
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        GrovsBuildConfig Config() => GrovsBuildConfig.Resolve(_settings, false);

        XmlDocument GenerateManifest()
        {
            GrovsAndroidLibGenerator.Generate(_dir, Config(), "io.grovs.unity.sample");
            var doc = new XmlDocument();
            doc.Load(Path.Combine(_dir, "AndroidManifest.xml"));
            return doc;
        }

        static XmlNamespaceManager Ns(XmlDocument doc)
        {
            var ns = new XmlNamespaceManager(doc.NameTable);
            ns.AddNamespace("android", AndroidNs);
            ns.AddNamespace("tools", ToolsNs);
            return ns;
        }

        [Test]
        public void LibManifest_MetaData()
        {
            GrovsAndroidLibGenerator.Generate(_dir, Config(), "io.grovs.unity.sample");

            Assert.AreEqual(Golden("GrovsConfig.AndroidManifest.expected.xml"),
                Normalize(File.ReadAllText(Path.Combine(_dir, "AndroidManifest.xml"))));
            StringAssert.Contains("android.library=true", File.ReadAllText(Path.Combine(_dir, "project.properties")));
        }

        [Test]
        public void LibManifest_OptionalEntries_OnlyWhenSet()
        {
            _settings.pushEnabled = false;
            _settings.androidSmallIcon = " ";
            _settings.baseUrl = "";
            _settings.clipboardDomains = new string[0];
            var doc = GenerateManifest();
            var ns = Ns(doc);

            Assert.IsNull(doc.SelectSingleNode("//meta-data[@android:name='io.grovs.NotificationIconSmall']", ns));
            Assert.IsNull(doc.SelectSingleNode("//uses-permission[@android:name='android.permission.POST_NOTIFICATIONS']", ns));
            Assert.IsNull(doc.SelectSingleNode("//meta-data[@android:name='grovs_base_url']", ns));
            Assert.IsNull(doc.SelectSingleNode("//meta-data[@android:name='grovs_clipboard_domains']", ns));
            Assert.IsNull(doc.SelectSingleNode("//service", ns));
            Assert.AreEqual("test-key",
                doc.SelectSingleNode("//meta-data[@android:name='grovs_api_key']/@android:value", ns).Value);
        }

        [Test]
        public void LibManifest_FirebaseUnityMode_RemovesServicesAndRegistersOurs()
        {
            _settings.androidPushMode = GrovsAndroidPushMode.FirebaseUnity;
            var doc = GenerateManifest();
            var ns = Ns(doc);

            foreach (var removed in new[] { "com.google.firebase.messaging.cpp.ListenerService", "io.grovs.handlers.MessagingService" })
            {
                var node = doc.SelectSingleNode("//service[@android:name='" + removed + "']", ns);
                Assert.IsNotNull(node, removed);
                Assert.AreEqual("remove", node.Attributes["node", ToolsNs].Value);
            }

            var ours = doc.SelectSingleNode("//service[@android:name='io.grovs.unity.GrovsFirebaseUnityService']", ns);
            Assert.IsNotNull(ours);
            Assert.AreEqual("false", ours.Attributes["exported", AndroidNs].Value);
            Assert.IsNotNull(ours.SelectSingleNode(
                "intent-filter/action[@android:name='com.google.firebase.MESSAGING_EVENT']", ns));
            Assert.IsFalse(File.Exists(Path.Combine(_dir, "res", "values", "google-services.xml")));
        }

        [Test]
        public void LibManifest_Standalone_WritesGoogleServices()
        {
            _settings.googleServicesJsonPath = GoldenPath("google-services.json");
            GenerateManifest();

            Assert.AreEqual(Golden("google-services.expected.xml"),
                Normalize(File.ReadAllText(Path.Combine(_dir, "res", "values", "google-services.xml"))));
        }

        [Test]
        public void PushDependencies_StandaloneOnly()
        {
            var path = Path.Combine(_dir, "Editor", "GrovsPushDependencies.xml");

            Assert.IsTrue(GrovsAndroidLibGenerator.WritePushDependencies(path, Config(), GrovsAndroidPushMode.Standalone));
            StringAssert.Contains("<androidPackage spec=\"com.google.firebase:firebase-messaging:24.0.3\" />",
                File.ReadAllText(path));
            Assert.IsFalse(GrovsAndroidLibGenerator.WritePushDependencies(path, Config(), GrovsAndroidPushMode.Standalone));

            Assert.IsTrue(GrovsAndroidLibGenerator.WritePushDependencies(path, Config(), GrovsAndroidPushMode.FirebaseUnity));
            Assert.IsFalse(File.Exists(path));
            Assert.IsFalse(GrovsAndroidLibGenerator.WritePushDependencies(path, Config(), GrovsAndroidPushMode.FirebaseUnity));

            _settings.pushEnabled = false;
            GrovsAndroidLibGenerator.WritePushDependencies(path, Config(), GrovsAndroidPushMode.Standalone);
            Assert.IsFalse(File.Exists(path));
        }

        [Test]
        public void PushMode_Auto_ResolvesByFirebaseUnityPresence()
        {
            Assert.AreEqual(GrovsAndroidPushMode.FirebaseUnity,
                GrovsAndroidLibGenerator.ResolvePushMode(GrovsAndroidPushMode.Auto, true));
            Assert.AreEqual(GrovsAndroidPushMode.Standalone,
                GrovsAndroidLibGenerator.ResolvePushMode(GrovsAndroidPushMode.Auto, false));
            Assert.AreEqual(GrovsAndroidPushMode.Standalone,
                GrovsAndroidLibGenerator.ResolvePushMode(GrovsAndroidPushMode.Standalone, true));
            Assert.AreEqual(GrovsAndroidPushMode.FirebaseUnity,
                GrovsAndroidLibGenerator.ResolvePushMode(GrovsAndroidPushMode.FirebaseUnity, false));
        }

        static XmlDocument LoadInput()
        {
            var doc = new XmlDocument();
            doc.Load(GoldenPath("unityLibrary.AndroidManifest.input.xml"));
            return doc;
        }

        [Test]
        public void IntentFilters_AddedToLauncherActivity()
        {
            var doc = LoadInput();
            GrovsLauncherIntentFilters.Apply(doc, Config());

            Assert.AreEqual(Golden("unityLibrary.AndroidManifest.expected.xml"),
                Normalize(GrovsAndroidXml.ToText(doc)));
        }

        [Test]
        public void IntentFilters_Idempotent()
        {
            var doc = LoadInput();
            GrovsLauncherIntentFilters.Apply(doc, Config());
            var first = GrovsAndroidXml.ToText(doc);

            var again = new XmlDocument();
            again.LoadXml(first);
            GrovsLauncherIntentFilters.Apply(again, Config());
            GrovsLauncherIntentFilters.Apply(again, Config());

            Assert.AreEqual(first, GrovsAndroidXml.ToText(again));
        }

        [Test]
        public void IntentFilters_ReplacesFiltersFromEarlierBuild_KeepsOwnFilters()
        {
            var doc = LoadInput();
            var launcher = (XmlElement)doc.SelectSingleNode("/manifest/application/activity[2]");
            var own = doc.CreateElement("intent-filter");
            own.AppendChild(GrovsAndroidXml.Element(doc, "action", "name", "android.intent.action.VIEW"));
            own.AppendChild(GrovsAndroidXml.Element(doc, "data", "scheme", "mygame"));
            launcher.AppendChild(own);
            GrovsLauncherIntentFilters.Apply(doc, Config());

            var reused = new XmlDocument();
            reused.LoadXml(GrovsAndroidXml.ToText(doc));
            _settings.uriScheme = "grovsbeta";
            _settings.linkDomains = new string[0];
            GrovsLauncherIntentFilters.Apply(reused, Config());

            var schemes = reused.SelectNodes("//intent-filter/data").Cast<XmlElement>()
                .Select(d => d.GetAttribute("scheme", AndroidNs)).ToArray();
            CollectionAssert.AreEquivalent(new[] { "mygame", "grovsbeta" }, schemes);
            Assert.AreEqual(3, reused.SelectNodes("//intent-filter").Count);
        }

        [Test]
        public void IntentFilters_NoLauncher_Throws()
        {
            var doc = new XmlDocument();
            doc.LoadXml("<manifest xmlns:android=\"" + AndroidNs + "\"><application>" +
                        "<activity android:name=\"com.game.MainActivity\" /></application></manifest>");

            var e = Assert.Throws<InvalidOperationException>(() => GrovsLauncherIntentFilters.Apply(doc, Config()));
            Assert.AreEqual("Grovs: no MAIN/LAUNCHER activity found in unityLibrary manifest.", e.Message);
        }

        [Test]
        public void GradleProps_AddsAndroidX()
        {
            var text = GrovsGradlePropertiesWriter.Apply("");
            StringAssert.Contains("android.useAndroidX=true", text);
            StringAssert.Contains("android.enableJetifier=true", text);

            var existing = "org.gradle.parallel=true\nandroid.useAndroidX=true\n";
            var applied = GrovsGradlePropertiesWriter.Apply(existing);
            Assert.AreEqual(1, applied.Split('\n').Count(l => l.Trim() == "android.useAndroidX=true"));
            StringAssert.Contains("android.enableJetifier=true", applied);
            StringAssert.StartsWith(existing, applied);
            Assert.AreEqual(applied, GrovsGradlePropertiesWriter.Apply(applied));

            var disabled = GrovsGradlePropertiesWriter.Apply("android.useAndroidX=false\n");
            StringAssert.Contains("android.useAndroidX=true", disabled);
            StringAssert.DoesNotContain("android.useAndroidX=false", disabled);
        }

        [Test]
        public void GoogleServices_Convert()
        {
            var json = File.ReadAllText(GoldenPath("google-services.json"));
            Assert.AreEqual(Golden("google-services.expected.xml"),
                Normalize(GrovsGoogleServicesConverter.Convert(json, "io.grovs.unity.sample")));
        }

        [Test]
        public void GoogleServices_NoMatchingClient_Throws()
        {
            var json = File.ReadAllText(GoldenPath("google-services.json"));
            var e = Assert.Throws<InvalidOperationException>(() => GrovsGoogleServicesConverter.Convert(json, "com.other"));
            StringAssert.Contains("com.other", e.Message);
        }

        [Test]
        public void ApiLevelCheck()
        {
            const string error = "Grovs: Android target API level must be 34 or higher.";
            Assert.AreEqual(error, GrovsAndroidBuildProcessors.CheckTargetApi((AndroidSdkVersions)33, 36));
            Assert.IsNull(GrovsAndroidBuildProcessors.CheckTargetApi((AndroidSdkVersions)34, 33));
            Assert.IsNull(GrovsAndroidBuildProcessors.CheckTargetApi((AndroidSdkVersions)35, 0));
            Assert.AreEqual(error, GrovsAndroidBuildProcessors.CheckTargetApi(AndroidSdkVersions.AndroidApiLevelAuto, 33));
            Assert.IsNull(GrovsAndroidBuildProcessors.CheckTargetApi(AndroidSdkVersions.AndroidApiLevelAuto, 34));
        }

        [Test]
        public void PushWarning_StandaloneWithoutGoogleServices()
        {
            _settings.googleServicesJsonPath = " ";
            StringAssert.Contains("google-services.json",
                GrovsAndroidBuildProcessors.PushWarning(Config(), GrovsAndroidPushMode.Standalone));
            Assert.IsNull(GrovsAndroidBuildProcessors.PushWarning(Config(), GrovsAndroidPushMode.FirebaseUnity));

            _settings.pushEnabled = false;
            Assert.IsNull(GrovsAndroidBuildProcessors.PushWarning(Config(), GrovsAndroidPushMode.Standalone));

            _settings.pushEnabled = true;
            _settings.googleServicesJsonPath = "Assets/google-services.json";
            Assert.IsNull(GrovsAndroidBuildProcessors.PushWarning(Config(), GrovsAndroidPushMode.Standalone));
        }

        [Test]
        public void FirebaseAar_IncludedOnlyInFirebaseUnityMode()
        {
            Assert.IsTrue(GrovsAndroidBuildProcessors.ShouldIncludeFirebaseAar(GrovsAndroidPushMode.FirebaseUnity));
            Assert.IsFalse(GrovsAndroidBuildProcessors.ShouldIncludeFirebaseAar(GrovsAndroidPushMode.Standalone));
        }

        [Test]
        public void PushModeCheck_FirebaseUnityForcedWithoutFirebase_Errors()
        {
            Assert.AreEqual("Grovs: Android push mode is Firebase Unity but Firebase Messaging is not installed.",
                GrovsAndroidBuildProcessors.CheckPushMode(GrovsAndroidPushMode.FirebaseUnity, false));
            Assert.IsNull(GrovsAndroidBuildProcessors.CheckPushMode(GrovsAndroidPushMode.FirebaseUnity, true));
            Assert.IsNull(GrovsAndroidBuildProcessors.CheckPushMode(GrovsAndroidPushMode.Auto, false));
            Assert.IsNull(GrovsAndroidBuildProcessors.CheckPushMode(GrovsAndroidPushMode.Standalone, false));
        }

        const string Agp742Root =
            "plugins {\n" +
            "    id 'com.android.application' version '7.4.2' apply false\n" +
            "    id 'com.android.library' version '7.4.2' apply false\n" +
            "}\n\n" +
            "task clean(type: Delete) {\n    delete rootProject.buildDir\n}\n";

        [Test]
        public void RootGradle_Agp7_GetsR8Classpath()
        {
            var patched = GrovsRootGradlePatcher.Apply(Agp742Root);
            StringAssert.Contains("classpath 'com.android.tools:r8:8.5.35'", patched);
            Assert.Less(patched.IndexOf("buildscript {", StringComparison.Ordinal),
                patched.IndexOf("plugins {", StringComparison.Ordinal));
            StringAssert.EndsWith(Agp742Root, patched);
            Assert.AreEqual(patched, GrovsRootGradlePatcher.Apply(patched));
        }

        [Test]
        public void RootGradle_Agp8_Unchanged()
        {
            var agp8 = Agp742Root.Replace("7.4.2", "8.3.0");
            Assert.AreEqual(agp8, GrovsRootGradlePatcher.Apply(agp8));
        }

        [Test]
        public void RootGradle_ExistingR8_Unchanged()
        {
            var withR8 = "buildscript {\n    dependencies {\n        classpath 'com.android.tools:r8:8.3.37'\n    }\n}\n\n" +
                         Agp742Root;
            Assert.AreEqual(withR8, GrovsRootGradlePatcher.Apply(withR8));
        }
        [Test]
        public void RootGradle_UnknownAgpVersion_WarnsAndSkips()
        {
            var dynamic = Agp742Root.Replace("'7.4.2'", "\"$agp\"");
            Assert.AreEqual(dynamic, GrovsRootGradlePatcher.Apply(dynamic, out var warning));
            Assert.AreEqual(GrovsRootGradlePatcher.UnknownVersionWarning, warning);

            GrovsRootGradlePatcher.Apply(Agp742Root, out var none);
            Assert.IsNull(none);
        }

        [Test]
        public void RootGradle_ExistingBuildScriptWithDependencies_GetsClasspathInside()
        {
            var input =
                "buildscript {\n" +
                "    repositories {\n        mavenCentral()\n    }\n" +
                "    dependencies {\n        classpath 'com.example:tool:1.0'\n    }\n" +
                "}\n\n" + Agp742Root;
            var expected =
                "buildscript {\n" +
                "    repositories {\n        mavenCentral()\n        google()\n    }\n" +
                "    dependencies {\n        classpath 'com.example:tool:1.0'\n" +
                "        classpath 'com.android.tools:r8:8.5.35'\n    }\n" +
                "}\n\n" + Agp742Root;

            var patched = GrovsRootGradlePatcher.Apply(input);
            Assert.AreEqual(expected, patched);
            Assert.AreEqual(patched, GrovsRootGradlePatcher.Apply(patched));
        }

        [Test]
        public void RootGradle_ExistingBuildScriptWithoutDependencies_AddsDependenciesBlock()
        {
            var input = "buildscript {\n    ext.kotlin = '1.9.0'\n}\n\n" + Agp742Root;
            var expected =
                "buildscript {\n" +
                "    repositories {\n        google()\n    }\n" +
                "    ext.kotlin = '1.9.0'\n" +
                "    dependencies {\n        classpath 'com.android.tools:r8:8.5.35'\n    }\n" +
                "}\n\n" + Agp742Root;

            var patched = GrovsRootGradlePatcher.Apply(input);
            Assert.AreEqual(expected, patched);
            Assert.AreEqual(1, patched.Split(new[] { "buildscript {" }, StringSplitOptions.None).Length - 1);
            Assert.AreEqual(patched, GrovsRootGradlePatcher.Apply(patched));
        }
    }
}
