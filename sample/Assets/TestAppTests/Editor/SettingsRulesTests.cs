using GrovsSDK;
using NUnit.Framework;

namespace GrovsTestApp.Tests
{
    public class SettingsRulesTests
    {
        static BakedConfig Config(string baseUrl = null, string[] domains = null) =>
            new BakedConfig("key", true, baseUrl, domains, false, true, null);

        [Test]
        public void Empty_key_is_not_set()
        {
            Assert.AreEqual("Not set", SettingsRules.ApiKey(""));
            Assert.AreEqual("Not set", SettingsRules.ApiKey(null));
        }

        [Test]
        public void Key_up_to_24_characters_is_shown_whole()
        {
            var key = new string('a', 24);
            Assert.AreEqual(key, SettingsRules.ApiKey(key));
        }

        [Test]
        public void Longer_key_keeps_eight_characters_each_side()
        {
            var key = "test_grovsu_01aecb88f7143385c98cb9b95f7e";
            Assert.AreEqual(key.Substring(0, 8) + "…" + key.Substring(key.Length - 8), SettingsRules.ApiKey(key));
        }

        [Test]
        public void Base_url_and_domains_fall_back_to_plain_words()
        {
            Assert.AreEqual("SDK default", SettingsRules.BaseUrl(Config()));
            Assert.AreEqual("none", SettingsRules.ClipboardDomains(Config()));
            Assert.AreEqual("https://x.io", SettingsRules.BaseUrl(Config("https://x.io")));
            Assert.AreEqual("a.io, b.io", SettingsRules.ClipboardDomains(Config(domains: new[] { "a.io", "b.io" })));
        }

        [Test]
        public void On_off_and_environment()
        {
            Assert.AreEqual("On", SettingsRules.OnOff(true));
            Assert.AreEqual("Off", SettingsRules.OnOff(false));
            Assert.AreEqual("Test", SettingsRules.Environment(Config()));
        }

        [TestCase(GrovsLogLevel.Info, GrovsLogLevel.Info)]
        [TestCase(GrovsLogLevel.Warn, GrovsLogLevel.Error)]
        [TestCase(GrovsLogLevel.Error, GrovsLogLevel.Error)]
        public void Saved_level_is_shown_as_info_or_error(GrovsLogLevel saved, GrovsLogLevel shown)
        {
            Assert.AreEqual(shown, SettingsRules.DisplayedLevel(saved));
        }

        [Test]
        public void Debug_level_title_is_upper_case()
        {
            Assert.AreEqual("Debug level INFO", SettingsRules.DebugLevelTitle(GrovsLogLevel.Info));
            Assert.AreEqual("Debug level ERROR", SettingsRules.DebugLevelTitle(GrovsLogLevel.Error));
        }
    }
}
