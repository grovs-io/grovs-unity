using NUnit.Framework;

namespace GrovsTestApp.Tests
{
    public class NativeServicesTests
    {
        [Test]
        public void Editor_config_comes_from_the_sample_grovs_settings()
        {
            var config = new EditorNativeServices().ReadBakedConfig();
            Assert.AreEqual("GROVS_SAMPLE_PLACEHOLDER", config.ApiKey);
            Assert.IsTrue(config.UseTestEnvironment);
            Assert.IsNull(config.BaseUrl);
            Assert.IsEmpty(config.ClipboardDomains);
            Assert.IsTrue(config.TrackScenes);
            Assert.IsTrue(config.StartEnabled);
            Assert.AreEqual("error", config.LogLevel);
        }

        [Test]
        public void Android_share_command_uses_the_given_package()
        {
            Assert.AreEqual(
                "adb shell am start -a android.intent.action.VIEW -d \"https://x.sqd.link/a\" io.grovs.unity.sample",
                ShareCommands.Android("https://x.sqd.link/a", "io.grovs.unity.sample"));
        }

        [Test]
        public void Ios_share_command_opens_the_booted_simulator()
        {
            Assert.AreEqual("xcrun simctl openurl booted \"https://x.sqd.link/a\"", ShareCommands.Ios("https://x.sqd.link/a"));
        }

        [Test]
        public void Raw_values_parse_like_the_native_readers()
        {
            var values = new System.Collections.Generic.Dictionary<string, string>
            {
                ["apiKey"] = " key ",
                ["useTestEnvironment"] = "true",
                ["clipboardDomains"] = "a.com, b.com,,",
                ["trackScenes"] = "false",
                ["logLevel"] = "info"
            };
            var config = BakedConfig.FromRaw(name => values.TryGetValue(name, out var v) ? v : null);
            Assert.AreEqual("key", config.ApiKey);
            Assert.IsTrue(config.UseTestEnvironment);
            Assert.IsNull(config.BaseUrl);
            CollectionAssert.AreEqual(new[] { "a.com", "b.com" }, config.ClipboardDomains);
            Assert.IsFalse(config.TrackScenes);
            Assert.IsTrue(config.StartEnabled);
            Assert.AreEqual("info", config.LogLevel);
        }
    }
}
