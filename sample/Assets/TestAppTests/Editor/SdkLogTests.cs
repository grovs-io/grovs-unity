using System.Collections.Generic;
using GrovsSDK;
using NUnit.Framework;

namespace GrovsTestApp.Tests
{
    public class SdkLogTests
    {
        [Test]
        public void Shown_message()
        {
            var e = SdkLog.InApp(new InAppMessageEvent { Type = InAppEventType.Shown, CampaignId = "c1" });
            Assert.That(e.state, Is.EqualTo(LogState.Ok));
            Assert.That(e.title, Is.EqualTo("Message c1 shown"));
        }

        [Test]
        public void Clicked_body_without_payload()
        {
            var e = SdkLog.InApp(new InAppMessageEvent { Type = InAppEventType.Clicked, CampaignId = "c1" });
            Assert.That(e.title, Is.EqualTo("Clicked message"));
            Assert.That(e.detail, Is.EqualTo("c1 · no payload"));
        }

        [Test]
        public void Clicked_button_lists_payload_keys()
        {
            var e = SdkLog.InApp(new InAppMessageEvent
            {
                Type = InAppEventType.Clicked, CampaignId = "c1", ButtonId = "buy",
                Payload = new Dictionary<string, object> { { "sku", "x" }, { "qty", 2L } }
            });
            Assert.That(e.title, Is.EqualTo("Clicked buy"));
            Assert.That(e.detail, Is.EqualTo("c1 · sku, qty"));
        }

        [Test]
        public void Dismissed_is_neutral_with_reason()
        {
            var e = SdkLog.InApp(new InAppMessageEvent { Type = InAppEventType.Dismissed, CampaignId = "c1", DismissReason = "swipe" });
            Assert.That(e.state, Is.EqualTo(LogState.Neutral));
            Assert.That(e.title, Is.EqualTo("Dismissed c1"));
            Assert.That(e.detail, Is.EqualTo("swipe"));
        }

        [Test]
        public void Answered_omits_empty_parts()
        {
            var e = SdkLog.InApp(new InAppMessageEvent { Type = InAppEventType.Answered, CampaignId = "c1", AnswerValue = 4L });
            Assert.That(e.title, Is.EqualTo("Answered c1"));
            Assert.That(e.detail, Is.EqualTo("4"));
            e = SdkLog.InApp(new InAppMessageEvent { Type = InAppEventType.Answered, CampaignId = "c1", AnswerValue = "yes", Comment = "great" });
            Assert.That(e.detail, Is.EqualTo("yes · great"));
        }

        [Test]
        public void Failed_is_an_error()
        {
            var e = SdkLog.InApp(new InAppMessageEvent { Type = InAppEventType.Failed, CampaignId = "c1", FailureMessage = "boom" });
            Assert.That(e.state, Is.EqualTo(LogState.Error));
            Assert.That(e.title, Is.EqualTo("Message c1 failed"));
            Assert.That(e.detail, Is.EqualTo("boom"));
        }

        [Test]
        public void Configured_detail_names_env_and_base_url()
        {
            var test = new BakedConfig("grovsu_0123456789", true, null, null, true, true, null);
            Assert.That(SdkLog.ConfiguredDetail(test), Is.EqualTo("test · SDK default"));
            var prod = new BakedConfig("k", false, "https://api.example.com", null, true, true, null);
            Assert.That(SdkLog.ConfiguredDetail(prod), Is.EqualTo("prod · https://api.example.com"));
            Assert.That(SdkLog.ConfiguredCode(test), Does.Contain(TextFormat.MiddleEllipsis("grovsu_0123456789")));
            Assert.That(SdkLog.ConfiguredCode(test), Does.Not.Contain("grovsu_0123456789"));
        }
    }
}
