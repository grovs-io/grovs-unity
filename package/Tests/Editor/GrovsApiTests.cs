using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GrovsSDK.Internal;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GrovsSDK.Tests
{
    public class GrovsApiTests
    {
        FakePlatform _fake;

        [SetUp]
        public void SetUp()
        {
            _fake = new FakePlatform();
            Grovs.ResetForTests(_fake);
        }

        [TearDown]
        public void TearDown()
        {
            Grovs.ResetForTests(new GrovsStub());
        }

        void Result(int requestId, string body)
        {
            _fake.Emit("result", "{\"requestId\":" + requestId + "," + body + "}");
            Grovs.Router.Drain();
        }

        static GrovsException Unwrap(Task task)
        {
            Assert.IsTrue(task.IsFaulted);
            return (GrovsException)task.Exception.InnerException;
        }

        [Test]
        public void GenerateLinkAsync_ResolvesOnResult()
        {
            var task = Grovs.GenerateLinkAsync(new GenerateLinkOptions { Title = "t" });

            var args = _fake.Last("GenerateLink");
            Assert.AreEqual("{\"title\":\"t\"}", args[1]);
            Assert.IsFalse(task.IsCompleted);

            Result((int)args[0], "\"ok\":true,\"value\":\"https://x/y\",\"error\":null");
            Assert.IsTrue(task.IsCompleted);
            Assert.AreEqual("https://x/y", task.Result);
        }

        [Test]
        public void GenerateLinkAsync_ErrorThrowsGrovsException()
        {
            var task = Grovs.GenerateLinkAsync(new GenerateLinkOptions());
            Result((int)_fake.Last("GenerateLink")[0],
                "\"ok\":false,\"value\":null,\"error\":{\"code\":\"link_generation_failed\",\"message\":\"no\"}");

            Assert.AreEqual(GrovsErrorCode.LinkGenerationFailed, Unwrap(task).Error.Code);
        }

        [Test]
        public void GenerateLinkAsync_NonSerializableData_Throws()
        {
            var options = new GenerateLinkOptions { Data = new Dictionary<string, object> { { "pos", Vector3.one } } };
            Assert.Throws<ArgumentException>(() => Grovs.GenerateLinkAsync(options));
            Assert.AreEqual(0, _fake.Count("GenerateLink"));
        }

        [Test]
        public void GenerateLink_Callback_ReceivesError()
        {
            var called = 0;
            string link = "unset";
            GrovsError error = null;
            Grovs.GenerateLink(new GenerateLinkOptions(), (l, e) => { called++; link = l; error = e; });

            Result((int)_fake.Last("GenerateLink")[0],
                "\"ok\":false,\"value\":null,\"error\":{\"code\":\"sdk_disabled\",\"message\":\"off\"}");

            Assert.AreEqual(1, called);
            Assert.IsNull(link);
            Assert.AreEqual(GrovsErrorCode.SdkDisabled, error.Code);
        }

        [Test]
        public void GenerateLink_Callback_ReceivesValue()
        {
            string link = null;
            GrovsError error = null;
            Grovs.GenerateLink(new GenerateLinkOptions(), (l, e) => { link = l; error = e; });

            Result((int)_fake.Last("GenerateLink")[0], "\"ok\":true,\"value\":\"https://x/y\",\"error\":null");

            Assert.AreEqual("https://x/y", link);
            Assert.IsNull(error);
        }

        [Test]
        public void Callback_Throws_IsLogged()
        {
            Grovs.GenerateLink(new GenerateLinkOptions(), (l, e) => throw new InvalidOperationException("cb boom"));
            LogAssert.Expect(LogType.Exception, new Regex("cb boom"));
            Result((int)_fake.Last("GenerateLink")[0], "\"ok\":true,\"value\":\"https://x/y\",\"error\":null");
        }

        [Test]
        public void GetLinkDetailsAsync_ReturnsDictionary()
        {
            var task = Grovs.GetLinkDetailsAsync("abc");
            var args = _fake.Last("LinkDetails");
            Assert.AreEqual("abc", args[1]);

            Result((int)args[0], "\"ok\":true,\"value\":{\"title\":\"T\",\"n\":2},\"error\":null");
            Assert.AreEqual("T", task.Result["title"]);
            Assert.AreEqual(2L, task.Result["n"]);
        }

        [Test]
        public void GetLinkDetails_Callback_ReceivesError()
        {
            GrovsError error = null;
            Grovs.GetLinkDetails("abc", (d, e) => error = e);
            Result((int)_fake.Last("LinkDetails")[0],
                "\"ok\":false,\"value\":null,\"error\":{\"code\":\"link_details_failed\",\"message\":\"x\"}");
            Assert.AreEqual(GrovsErrorCode.LinkDetailsFailed, error.Code);
        }

        [Test]
        public void LogInAppPurchaseAsync_ReturnsBool()
        {
            var task = Grovs.LogInAppPurchaseAsync("1000001");
            var args = _fake.Last("LogInAppPurchase");
            Assert.AreEqual("1000001", args[1]);

            Result((int)args[0], "\"ok\":true,\"value\":true,\"error\":null");
            Assert.IsTrue(task.Result);
        }

        [Test]
        public void LogCustomPurchaseAsync_PassesEpochMs()
        {
            var date = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            Grovs.LogCustomPurchaseAsync(GrovsTransactionType.Buy, 499, "USD", "gems", date);
            var args = _fake.Last("LogCustomPurchase");
            Assert.AreEqual(0, args[1]);
            Assert.AreEqual(499, args[2]);
            Assert.AreEqual("USD", args[3]);
            Assert.AreEqual("gems", args[4]);
            Assert.AreEqual(1767225600000L, args[5]);

            Grovs.LogCustomPurchaseAsync(GrovsTransactionType.Cancel, 499, "USD", "gems");
            args = _fake.Last("LogCustomPurchase");
            Assert.AreEqual(1, args[1]);
            Assert.AreEqual(-1L, args[5]);

            var task = Grovs.LogCustomPurchaseAsync(GrovsTransactionType.Refund, 1, "EUR", "p");
            args = _fake.Last("LogCustomPurchase");
            Assert.AreEqual(2, args[1]);
            Result((int)args[0], "\"ok\":true,\"value\":false,\"error\":null");
            Assert.IsFalse(task.Result);
        }

        [Test]
        public void RequestIds_AreUnique()
        {
            Grovs.GenerateLinkAsync(new GenerateLinkOptions());
            Grovs.GetLinkDetailsAsync("p");
            Assert.AreNotEqual(_fake.Last("GenerateLink")[0], _fake.Last("LinkDetails")[0]);
        }

        [Test]
        public void Track_ReservedName_WarnsAndStillForwards()
        {
            LogAssert.Expect(LogType.Warning, new Regex("reserved"));
            Grovs.Track("view");
            Assert.AreEqual(1, _fake.Count("Track"));
        }

        [Test]
        public void Track_ForwardsJson()
        {
            Grovs.Track("level_up", new Dictionary<string, object> { { "level", 3 } }, new List<string> { "a" });
            var args = _fake.Last("Track");
            Assert.AreEqual("level_up", args[0]);
            Assert.AreEqual("{\"level\":3}", args[1]);
            Assert.AreEqual("[\"a\"]", args[2]);

            Grovs.Track("level_up");
            args = _fake.Last("Track");
            Assert.IsNull(args[1]);
            Assert.IsNull(args[2]);
        }

        [Test]
        public void Track_NonSerializable_Throws()
        {
            var props = new Dictionary<string, object> { { "pos", Vector3.one } };
            Assert.Throws<ArgumentException>(() => Grovs.Track("e", props));
            Assert.Throws<ArgumentException>(() => Grovs.TrackScreenView("s", props));
            Assert.AreEqual(0, _fake.Count("Track"));
            Assert.AreEqual(0, _fake.Count("TrackScreenView"));
        }

        [Test]
        public void TrackScreenView_Forwards()
        {
            Grovs.TrackScreenView("Menu", new Dictionary<string, object> { { "k", "v" } });
            var args = _fake.Last("TrackScreenView");
            Assert.AreEqual("Menu", args[0]);
            Assert.AreEqual("{\"k\":\"v\"}", args[1]);
        }

        [Test]
        public void SetUserAttributes_NonSerializable_Throws()
        {
            var attrs = new Dictionary<string, object> { { "pos", Vector3.one } };
            Assert.Throws<ArgumentException>(() => Grovs.SetUserAttributes(attrs));
            Assert.AreEqual(0, _fake.Count("SetUserAttributes"));

            Grovs.SetUserAttributes(new Dictionary<string, object> { { "a", 1 } });
            Assert.AreEqual("{\"a\":1}", _fake.Last("SetUserAttributes")[0]);
        }

        [Test]
        public void Identity_Forwards()
        {
            Grovs.SetUserIdentifier("u1");
            Grovs.SetUserIdentifier(null);
            Grovs.SetPushToken("tok");
            Assert.AreEqual(2, _fake.Count("SetUserIdentifier"));
            Assert.IsNull(_fake.Last("SetUserIdentifier")[0]);
            Assert.AreEqual("tok", _fake.Last("SetPushToken")[0]);
        }

        [Test]
        public void HandleUrl_RelativeUrl_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => Grovs.HandleUrl("abc"));
            Assert.Throws<ArgumentException>(() => Grovs.HandleUrl("/x"));
            Assert.Throws<ArgumentNullException>(() => Grovs.HandleUrl(null));
            Assert.AreEqual(0, _fake.Count("HandleUrl"));

            Assert.IsTrue(Grovs.HandleUrl("https://x.sqd.link/a"));
            Assert.AreEqual("https://x.sqd.link/a", _fake.Last("HandleUrl")[0]);
            Assert.IsTrue(Grovs.HandleUrl("myapp://open/a"));
        }

        [Test]
        public void SetLogLevel_Warn_SendsOne()
        {
            Grovs.SetLogLevel(GrovsLogLevel.Warn);
            Assert.AreEqual(1, _fake.Last("SetLogLevel")[0]);
        }

        [Test]
        public void Consent_Forwards()
        {
            Grovs.SetEnabled(false);
            Assert.AreEqual(false, _fake.Last("SetEnabled")[0]);
            Assert.IsTrue(Grovs.IsEnabled);
            Assert.IsTrue(Grovs.IsSupportedPlatform);
            Assert.IsFalse(string.IsNullOrEmpty(Grovs.SdkVersion));
        }

        [Test]
        public void TagsAndAliases_Forward()
        {
            Grovs.SetGlobalTags(new List<string> { "a", "b" });
            Assert.AreEqual("[\"a\",\"b\"]", _fake.Last("SetGlobalTags")[0]);
            Grovs.SetGlobalTags(null);
            Assert.IsNull(_fake.Last("SetGlobalTags")[0]);

            Grovs.SetScreenAliases(new Dictionary<string, string> { { "Scene1", "Menu" } });
            Assert.AreEqual("{\"Scene1\":\"Menu\"}", _fake.Last("SetScreenAliases")[0]);
            Grovs.SetScreenAliases(null);
            Assert.AreEqual("{}", _fake.Last("SetScreenAliases")[0]);
        }

        [Test]
        public void InAppControls_Forward()
        {
            Grovs.PauseInAppMessages();
            Grovs.ResumeInAppMessages();
            Grovs.DiscardInAppMessages();
            Assert.AreEqual(false, _fake.Last("DiscardInApp")[0]);
            Grovs.DiscardInAppMessages(true);
            Assert.AreEqual(true, _fake.Last("DiscardInApp")[0]);
            Grovs.SetInAppExcludedScreens(new List<string> { "Menu" });
            Assert.AreEqual("[\"Menu\"]", _fake.Last("SetInAppExcludedScreens")[0]);
            Grovs.SetInAppExcludedScreens(null);
            Assert.AreEqual("[]", _fake.Last("SetInAppExcludedScreens")[0]);
            Grovs.SetInAppDebugLogging(true);
            Assert.AreEqual(true, _fake.Last("SetInAppDebugLogging")[0]);
            Assert.AreEqual(1, _fake.Count("PauseInApp"));
            Assert.AreEqual(1, _fake.Count("ResumeInApp"));
        }

        [Test]
        public void PushHelpers_ForwardJson()
        {
            Assert.IsTrue(Grovs.IsGrovsNotification(new Dictionary<string, string> { { "grovs_campaign_id", "1" } }));
            Assert.AreEqual("{\"grovs_campaign_id\":\"1\"}", _fake.Last("IsGrovsNotification")[0]);

            Assert.IsTrue(Grovs.HandleNotificationOpen(new Dictionary<string, object> { { "n", 1 } }));
            Assert.AreEqual("{\"n\":1}", _fake.Last("HandleNotificationOpen")[0]);

            Assert.IsFalse(Grovs.IsGrovsNotification(null));
            Assert.IsFalse(Grovs.HandleNotificationOpen(null));
            Assert.AreEqual(1, _fake.Count("IsGrovsNotification"));
            Assert.AreEqual(1, _fake.Count("HandleNotificationOpen"));
        }

        [Test]
        public void Stub_GenerateLink_FailsUnsupported()
        {
            Grovs.ResetForTests(new GrovsStub());
            Assert.IsFalse(Grovs.IsSupportedPlatform);

            var task = Grovs.GenerateLinkAsync(new GenerateLinkOptions());
            Grovs.Router.Drain();
            Assert.AreEqual(GrovsErrorCode.UnsupportedPlatform, Unwrap(task).Error.Code);
        }

        [Test]
        public void Stub_Defaults()
        {
            Grovs.ResetForTests(new GrovsStub());
            Assert.IsFalse(Grovs.IsEnabled);
            Assert.IsFalse(Grovs.HandleUrl("https://x.sqd.link/a"));
            Assert.IsFalse(Grovs.IsGrovsNotification(new Dictionary<string, string>()));
            Assert.IsFalse(Grovs.HandleNotificationOpen(new Dictionary<string, object>()));
            Grovs.Track("level_up");

            var purchase = Grovs.LogCustomPurchaseAsync(GrovsTransactionType.Buy, 1, "USD", "p");
            var details = Grovs.GetLinkDetailsAsync("p");
            Grovs.Router.Drain();
            Assert.IsFalse(purchase.Result);
            Assert.AreEqual(GrovsErrorCode.UnsupportedPlatform, Unwrap(details).Error.Code);
        }
    }
}
