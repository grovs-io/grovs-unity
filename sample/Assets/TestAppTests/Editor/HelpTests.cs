using System;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace GrovsTestApp.Tests
{
    public class HelpTests
    {
        static readonly Regex Activity = new Regex(@"\bactivity\b", RegexOptions.IgnoreCase);

        [Test]
        public void Every_key_has_a_title_and_a_body([Values(false, true)] bool ios)
        {
            Assert.That(Help.Keys, Is.Not.Empty);
            foreach (var key in Help.Keys)
            {
                var (title, body) = Help.Get(key, ios);
                Assert.That(title, Is.Not.Empty, key);
                Assert.That(body, Is.Not.Empty, key);
            }
        }

        [Test]
        public void No_text_uses_android_only_words([Values(false, true)] bool ios)
        {
            foreach (var key in Help.Keys)
            {
                var (title, body) = Help.Get(key, ios);
                foreach (var text in new[] { title, body })
                {
                    Assert.That(text, Does.Not.Contain("\u2014"), key);
                    Assert.That(text, Does.Not.Contain("coroutine").IgnoreCase, key);
                    Assert.That(text, Does.Not.Contain("SecondActivity"), key);
                    Assert.That(Activity.IsMatch(text), Is.False, key + ": " + text);
                }
            }
        }

        [Test]
        public void Store_receipt_and_push_token_follow_the_platform()
        {
            Assert.That(Help.Get("storeReceipt", false).title, Is.EqualTo("Google Play purchase JSON"));
            Assert.That(Help.Get("storeReceipt", true).title, Is.EqualTo("App Store transaction id"));
            Assert.That(Help.Get("pushToken", true).body, Does.EndWith("Not available on iOS in this test app."));
            Assert.That(Help.Get("pushToken", false).body, Does.Not.Contain("Not available"));
        }

        [Test]
        public void Unknown_key_throws()
        {
            Assert.Throws<ArgumentException>(() => Help.Get("nope"));
        }
    }
}
