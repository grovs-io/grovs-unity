using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace GrovsTestApp.Tests
{
    public class HelpersTests
    {
        [TestCase("https://x.sqd.link/abc?u=1#t", "abc")]
        [TestCase("x.sqd.link/abc/", "abc")]
        [TestCase("x.sqd.link/a/b", "b")]
        [TestCase("abc", "abc")]
        [TestCase("  abc  ", "abc")]
        [TestCase("/abc", "abc")]
        [TestCase("https://x.sqd.link/", null)]
        [TestCase("https://x.sqd.link", null)]
        [TestCase("", null)]
        [TestCase(null, null)]
        public void LinkPath_extracts_the_last_segment(string input, string expected)
        {
            Assert.AreEqual(expected, LinkPath.Extract(input));
        }

        [TestCase("4.99", 499)]
        [TestCase("4,9", 490)]
        [TestCase("5", 500)]
        [TestCase("0.05", 5)]
        [TestCase("21474836.47", 2147483647)]
        public void Money_parses_cents(string input, int expected)
        {
            Assert.AreEqual(expected, Money.ParseCents(input));
        }

        [TestCase("4.999")]
        [TestCase("")]
        [TestCase("abc")]
        [TestCase("-1")]
        [TestCase("4.")]
        [TestCase(null)]
        [TestCase("99999999999")]
        [TestCase("4611686018427387904")]
        [TestCase("21474836.48")]
        [TestCase("4.\u0669")]
        public void Money_rejects_bad_amounts(string input)
        {
            Assert.IsNull(Money.ParseCents(input));
        }

        [TestCase(499, "USD", "$4.99")]
        [TestCase(499, "EUR", "€4.99")]
        [TestCase(499, "GBP", "£4.99")]
        [TestCase(499, "RON", "4.99 RON")]
        [TestCase(5, "USD", "$0.05")]
        public void Money_formats(int cents, string currency, string expected)
        {
            Assert.AreEqual(expected, Money.Format(cents, currency));
        }

        [Test]
        public void Reserved_names_are_exactly_the_sdk_list()
        {
            var expected = new[]
            {
                "app_open", "custom", "in_app_answered", "in_app_clicked", "in_app_commented", "in_app_dismissed",
                "in_app_held", "in_app_viewed", "install", "journey_entered", "journey_held", "journey_path", "open",
                "push_delivered", "push_held", "push_open", "reactivation", "reinstall", "screen_view", "time_spent",
                "user_referred", "view"
            };

            CollectionAssert.AreEquivalent(expected, EventRules.Reserved.ToArray());
        }

        [Test]
        public void IsReserved_ignores_case_and_spaces()
        {
            Assert.IsTrue(EventRules.IsReserved(" Install "));
            Assert.IsFalse(EventRules.IsReserved("install_now"));
            Assert.IsFalse(EventRules.IsReserved(null));
        }

        [Test]
        public void NameError_checks_blank_and_reserved()
        {
            Assert.IsNull(EventRules.NameError("level_up"));
            Assert.AreEqual("Enter an event name", EventRules.NameError("  "));
            Assert.AreEqual("Enter an event name", EventRules.NameError(null));
            Assert.AreEqual("\"Install\" is reserved by the SDK", EventRules.NameError(" Install "));
        }

        [Test]
        public void TagsError_checks_count_and_length()
        {
            Assert.IsNull(EventRules.TagsError(new List<string> { "a" }));
            Assert.IsNull(EventRules.TagsError(Enumerable.Repeat("t", 20).ToList()));
            Assert.AreEqual("Up to 20 tags", EventRules.TagsError(Enumerable.Repeat("t", 21).ToList()));
            Assert.IsNull(EventRules.TagsError(new List<string> { new string('a', 255) }));
            Assert.AreEqual("Each tag can have up to 255 characters",
                EventRules.TagsError(new List<string> { new string('a', 256) }));
        }

        [Test]
        public void MiddleEllipsis_shortens_long_strings_only()
        {
            Assert.AreEqual("abcde…vwxyz", TextFormat.MiddleEllipsis("abcdefghijklmnopqrstuvwxyz"));
            Assert.AreEqual("abcdefghijk", TextFormat.MiddleEllipsis("abcdefghijk"));
            Assert.AreEqual("abcde…hijkl", TextFormat.MiddleEllipsis("abcdefghijkl"));
            Assert.AreEqual("ab…yz", TextFormat.MiddleEllipsis("abcdefghijklmnopqrstuvwxyz", 2));
            Assert.AreEqual("", TextFormat.MiddleEllipsis(null));
        }

        [TestCase("https://example.com", true)]
        [TestCase("http://example.com/a?b=1", true)]
        [TestCase("ftp://example.com", false)]
        [TestCase("example.com", false)]
        [TestCase("https://", false)]
        [TestCase("", false)]
        [TestCase(null, false)]
        public void Url_accepts_only_http_and_https(string input, bool expected)
        {
            Assert.AreEqual(expected, TextFormat.Url(input));
        }

        [TestCase(0, "just now")]
        [TestCase(59, "just now")]
        [TestCase(-30, "just now")]
        [TestCase(60, "1 min ago")]
        [TestCase(59 * 60, "59 min ago")]
        [TestCase(60 * 60, "1 h ago")]
        [TestCase(23 * 3600, "23 h ago")]
        [TestCase(24 * 3600, "yesterday")]
        [TestCase(47 * 3600, "yesterday")]
        [TestCase(48 * 3600, "2 days ago")]
        [TestCase(5 * 24 * 3600, "5 days ago")]
        public void RelativeTime_formats(int seconds, string expected)
        {
            Assert.AreEqual(expected, RelativeTime.Format(TimeSpan.FromSeconds(seconds)));
        }

        [Test]
        public void DeviceInfo_name_starts_with_at()
        {
            StringAssert.StartsWith("@", DeviceInfo.Name);
        }

        [Test]
        public void RandomId_has_the_qa_format()
        {
            for (var i = 0; i < 50; i++) StringAssert.IsMatch(@"^qa_\d{6}$", RandomId.Qa());
        }

        [Test]
        public void ChipText_commits_everything_before_the_last_comma()
        {
            var (commit, rest) = ChipText.Split("a,b, c");
            CollectionAssert.AreEqual(new[] { "a", "b" }, commit);
            Assert.AreEqual(" c", rest);
        }

        [Test]
        public void ChipText_without_a_comma_commits_nothing()
        {
            var (commit, rest) = ChipText.Split("a");
            CollectionAssert.IsEmpty(commit);
            Assert.AreEqual("a", rest);
        }
    }
}
