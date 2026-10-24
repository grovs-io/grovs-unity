using System;
using NUnit.Framework;

namespace GrovsTestApp.Tests
{
    public class HomeRulesTests
    {
        const LinkSources Both = LinkSources.Event | LinkSources.LastDeeplink;

        [TestCase(0, LogState.Ok)]
        [TestCase(60, LogState.Ok)]
        public void Pill_is_ok_when_both_sources_reported(int seconds, LogState state)
        {
            var (text, actual) = HomeRules.OpenedFromPill(Both, TimeSpan.FromSeconds(seconds));
            Assert.AreEqual("event ✓ last link ✓", text);
            Assert.AreEqual(state, actual);
        }

        [Test]
        public void Pill_waits_inside_the_window()
        {
            var (text, state) = HomeRules.OpenedFromPill(LinkSources.Event, TimeSpan.FromSeconds(4));
            Assert.AreEqual("waiting", text);
            Assert.AreEqual(LogState.Neutral, state);
        }

        [Test]
        public void Pill_names_the_missing_last_link()
        {
            var (text, state) = HomeRules.OpenedFromPill(LinkSources.Event, TimeSpan.FromSeconds(6));
            Assert.AreEqual("last link missed", text);
            Assert.AreEqual(LogState.Error, state);
        }

        [Test]
        public void Pill_names_the_missing_event()
        {
            var (text, state) = HomeRules.OpenedFromPill(LinkSources.LastDeeplink, TimeSpan.FromSeconds(6));
            Assert.AreEqual("event missed", text);
            Assert.AreEqual(LogState.Error, state);
        }

        [TestCase(true, false, "Not available on iOS")]
        [TestCase(true, true, "Not available on iOS")]
        [TestCase(false, true, "Waiting…")]
        [TestCase(false, false, "Not available in this build")]
        public void Push_token_placeholder(bool ios, bool requesting, string expected)
        {
            Assert.AreEqual(expected, HomeRules.PushTokenPlaceholder(ios, requesting));
        }

        [TestCase(NotificationStatus.NotAllowed, false, NotificationAllowAction.Request)]
        [TestCase(NotificationStatus.NotAllowed, true, NotificationAllowAction.OpenSettings)]
        [TestCase(NotificationStatus.Unknown, true, NotificationAllowAction.Request)]
        public void Allow_requests_once_then_opens_settings(NotificationStatus status, bool requestedBefore, NotificationAllowAction expected)
        {
            Assert.AreEqual(expected, HomeRules.AllowAction(status, requestedBefore));
        }

        [TestCase("https://x.sqd.link/abc", true)]
        [TestCase("  http://a.b/c  ", true)]
        [TestCase("hello", false)]
        [TestCase("ftp://a.b", false)]
        [TestCase("myapp://open", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void Web_url_check(string text, bool expected)
        {
            Assert.AreEqual(expected, HomeRules.IsWebUrl(text));
        }

        [Test]
        public void Attributes_summary_lists_pairs_or_none()
        {
            var rows = new[]
            {
                new KeyValueRow { Key = "plan", Value = "pro" },
                new KeyValueRow { Key = " ", Value = "skipped" },
                new KeyValueRow { Key = "country", Value = "RO" }
            };
            Assert.AreEqual("plan=pro, country=RO", HomeRules.AttributesSummary(rows));
            Assert.AreEqual("none", HomeRules.AttributesSummary(new KeyValueRow[0]));
        }

        [Test]
        public void Caption_joins_environment_and_version()
        {
            var config = new BakedConfig("k", true, null, null, false, true, null);
            Assert.AreEqual("Test · 1.0.0", HomeRules.Caption(config, "1.0.0"));
        }
    }
}
