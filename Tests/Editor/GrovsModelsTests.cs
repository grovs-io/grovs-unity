using System.Collections.Generic;
using NUnit.Framework;
using GrovsSDK.Internal;

namespace GrovsSDK.Tests
{
    public class GrovsModelsTests
    {
        [Test]
        public void GenerateLinkOptions_ToJson_FullKeys()
        {
            var options = new GenerateLinkOptions
            {
                Title = "t", Subtitle = "s", ImageUrl = "i",
                Data = new Dictionary<string, object> { { "k", 1 } },
                Tags = new List<string> { "a" },
                IosRedirect = new GrovsRedirect { Url = "https://i", OpenAppIfInstalled = false },
                AndroidRedirect = new GrovsRedirect { Url = "https://a" },
                DesktopRedirect = new GrovsRedirect { Url = "https://d" },
                ShowPreviewIos = true, ShowPreviewAndroid = false,
                CopyToClipboardIos = true, CopyToClipboardAndroid = false,
                TrackingCampaign = "c", TrackingSource = "s", TrackingMedium = "m"
            };
            var map = (Dictionary<string, object>)GrovsJson.Parse(options.ToJson());
            CollectionAssert.AreEquivalent(new[]
            {
                "title", "subtitle", "imageUrl", "data", "tags", "iosRedirect", "androidRedirect",
                "desktopRedirect", "showPreviewIos", "showPreviewAndroid", "copyToClipboardIos",
                "copyToClipboardAndroid", "trackingCampaign", "trackingSource", "trackingMedium"
            }, map.Keys);
            var ios = (Dictionary<string, object>)map["iosRedirect"];
            Assert.AreEqual("https://i", ios["url"]);
            Assert.AreEqual(false, ios["openAppIfInstalled"]);
            Assert.AreEqual(false, map["showPreviewAndroid"]);
        }

        [Test]
        public void GenerateLinkOptions_ToJson_OmitsNulls()
        {
            Assert.AreEqual("{}", new GenerateLinkOptions().ToJson());
        }

        [Test]
        public void GrovsRedirect_DefaultOpenAppIfInstalled_True()
        {
            Assert.IsTrue(new GrovsRedirect().OpenAppIfInstalled);
        }

        [Test]
        public void Deeplink_FromJson()
        {
            var link = GrovsDeeplink.FromJson(
                "{\"link\":\"https://x.sqd.link/a\",\"data\":{\"level\":3.0,\"grovs_campaign_id\":\"c1\"}," +
                "\"tracking\":{\"campaign\":\"s\",\"source\":\"e\",\"medium\":\"m\"}}");
            Assert.AreEqual("https://x.sqd.link/a", link.Link);
            Assert.AreEqual(3L, link.Data["level"]);
            Assert.IsTrue(link.FromPush);
            Assert.AreEqual("e", link.Tracking.Source);

            var empty = GrovsDeeplink.FromJson("{\"link\":null,\"data\":null,\"tracking\":null}");
            Assert.IsNull(empty.Link);
            Assert.IsNotNull(empty.Data);
            Assert.AreEqual(0, empty.Data.Count);
            Assert.IsNull(empty.Tracking);
            Assert.IsFalse(empty.FromPush);
        }

        [Test]
        public void Error_FromJson_MapsEveryCanonicalCode()
        {
            var expected = new Dictionary<string, GrovsErrorCode>
            {
                { "authentication_failed", GrovsErrorCode.AuthenticationFailed },
                { "network_request_failed", GrovsErrorCode.NetworkRequestFailed },
                { "event_dispatch_failed", GrovsErrorCode.EventDispatchFailed },
                { "link_generation_failed", GrovsErrorCode.LinkGenerationFailed },
                { "in_app_display_failed", GrovsErrorCode.InAppDisplayFailed },
                { "link_details_failed", GrovsErrorCode.LinkDetailsFailed },
                { "notifications_error", GrovsErrorCode.NotificationsError },
                { "sdk_not_initialized", GrovsErrorCode.SdkNotInitialized },
                { "sdk_disabled", GrovsErrorCode.SdkDisabled },
                { "invalid_argument", GrovsErrorCode.InvalidArgument },
                { "unsupported_platform", GrovsErrorCode.UnsupportedPlatform },
                { "unknown", GrovsErrorCode.Unknown }
            };
            foreach (var pair in expected)
            {
                var obj = GrovsJson.Parse("{\"code\":\"" + pair.Key + "\",\"message\":\"m\"}");
                var error = GrovsError.FromJson(obj);
                Assert.AreEqual(pair.Value, error.Code, pair.Key);
                Assert.AreEqual("m", error.Message);
            }

            var weird = GrovsError.FromJson(GrovsJson.Parse("{\"code\":\"weird\",\"message\":\"boom\"}"));
            Assert.AreEqual(GrovsErrorCode.Unknown, weird.Code);
            StringAssert.Contains("weird", weird.Message);
        }

        [Test]
        public void InAppEvent_FromJson_Answered()
        {
            var e = InAppMessageEvent.FromJson(
                "{\"type\":\"answered\",\"campaignId\":\"c\",\"value\":4,\"comment\":\"ok\"}");
            Assert.AreEqual(InAppEventType.Answered, e.Type);
            Assert.AreEqual("c", e.CampaignId);
            Assert.AreEqual(4L, e.AnswerValue);
            Assert.AreEqual("ok", e.Comment);
        }
    }
}
