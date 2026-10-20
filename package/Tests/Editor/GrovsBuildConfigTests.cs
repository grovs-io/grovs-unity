using NUnit.Framework;
using UnityEngine;
using GrovsSDK.Editor;

namespace GrovsSDK.Tests
{
    public class GrovsBuildConfigTests
    {
        GrovsSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = ScriptableObject.CreateInstance<GrovsSettings>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_settings);
        }

        [Test]
        public void Resolve_TestForDevelopmentBuilds()
        {
            _settings.environment = GrovsEnvironment.TestForDevelopmentBuilds;
            Assert.IsTrue(GrovsBuildConfig.Resolve(_settings, true).UseTestEnvironment);
            Assert.IsFalse(GrovsBuildConfig.Resolve(_settings, false).UseTestEnvironment);

            _settings.environment = GrovsEnvironment.Production;
            Assert.IsFalse(GrovsBuildConfig.Resolve(_settings, true).UseTestEnvironment);

            _settings.environment = GrovsEnvironment.Test;
            Assert.IsTrue(GrovsBuildConfig.Resolve(_settings, false).UseTestEnvironment);
        }

        [Test]
        public void Validate_EmptyKeyAndScheme_Errors()
        {
            _settings.apiKey = " ";
            _settings.uriScheme = "";
            CollectionAssert.AreEqual(new[]
            {
                "Grovs: API key is empty (Project Settings > Grovs).",
                "Grovs: URI scheme is empty. iOS authentication fails without it."
            }, GrovsBuildConfig.Resolve(_settings, false).Validate());

            _settings.apiKey = "key";
            _settings.uriScheme = "scheme";
            CollectionAssert.IsEmpty(GrovsBuildConfig.Resolve(_settings, false).Validate());
        }

        [Test]
        public void Resolve_DefaultsAndCleanup()
        {
            Assert.IsTrue(_settings.startEnabled);
            Assert.IsTrue(_settings.iosOwnNotificationDelegate);
            Assert.IsFalse(_settings.pushEnabled);
            Assert.IsFalse(_settings.trackScenes);
            Assert.AreEqual(GrovsLogLevel.Error, _settings.logLevel);
            Assert.AreEqual(GrovsEnvironment.TestForDevelopmentBuilds, _settings.environment);
            Assert.AreEqual(GrovsAndroidPushMode.Auto, _settings.androidPushMode);

            _settings.apiKey = " key ";
            _settings.clipboardDomains = new[] { " a.sqd.link ", "", null, "a.sqd.link" };
            _settings.linkDomains = null;
            var c = GrovsBuildConfig.Resolve(_settings, false);
            Assert.AreEqual("key", c.ApiKey);
            CollectionAssert.AreEqual(new[] { "a.sqd.link" }, c.ClipboardDomains);
            CollectionAssert.IsEmpty(c.LinkDomains);
            Assert.AreEqual("error", c.LogLevelName);

            _settings.logLevel = GrovsLogLevel.Info;
            Assert.AreEqual("info", GrovsBuildConfig.Resolve(_settings, false).LogLevelName);
            _settings.logLevel = GrovsLogLevel.Warn;
            Assert.AreEqual("warn", GrovsBuildConfig.Resolve(_settings, false).LogLevelName);
        }
    }
}
