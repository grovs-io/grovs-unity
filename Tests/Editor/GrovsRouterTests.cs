using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using GrovsSDK.Internal;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GrovsSDK.Tests
{
    public class GrovsRouterTests
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

        static string Link(string link) => "{\"link\":\"" + link + "\",\"data\":{\"level\":3},\"tracking\":null}";

        [Test]
        public void Start_IsCalledOnce()
        {
            Assert.AreEqual(1, _fake.Count("Start"));
            Assert.IsNotNull(_fake.OnMessage);
            Assert.IsNotNull(_fake.ShouldShow);
        }

        [Test]
        public void Deeplink_BufferedUntilFirstSubscriber()
        {
            _fake.Emit("deeplink", Link("https://x/1"));
            _fake.Emit("deeplink", Link("https://x/2"));
            Grovs.Router.Drain();

            var a = new List<string>();
            var b = new List<string>();
            Grovs.DeeplinkReceived += d => a.Add(d.Link);
            CollectionAssert.AreEqual(new[] { "https://x/1", "https://x/2" }, a);

            Grovs.DeeplinkReceived += d => b.Add(d.Link);
            CollectionAssert.IsEmpty(b);

            Assert.AreEqual("https://x/2", Grovs.LastDeeplink.Link);
            Assert.AreEqual(3L, Grovs.LastDeeplink.Data["level"]);
        }

        [Test]
        public void Deeplink_AfterSubscribed_DeliveredOnDrain()
        {
            var got = new List<string>();
            Grovs.DeeplinkReceived += d => got.Add(d.Link);

            _fake.Emit("deeplink", Link("https://x/1"));
            CollectionAssert.IsEmpty(got);

            Grovs.Router.Drain();
            CollectionAssert.AreEqual(new[] { "https://x/1" }, got);
        }

        [Test]
        public void Deeplink_AfterAllUnsubscribe_BuffersAgain()
        {
            Action<GrovsDeeplink> first = d => { };
            Grovs.DeeplinkReceived += first;
            Grovs.DeeplinkReceived -= first;

            _fake.Emit("deeplink", Link("https://x/1"));
            Grovs.Router.Drain();

            var got = new List<string>();
            Grovs.DeeplinkReceived += d => got.Add(d.Link);
            CollectionAssert.AreEqual(new[] { "https://x/1" }, got);
        }

        [Test]
        public void HandlerThrows_OthersStillRun()
        {
            var got = new List<string>();
            Grovs.DeeplinkReceived += d => throw new InvalidOperationException("boom");
            Grovs.DeeplinkReceived += d => got.Add(d.Link);

            LogAssert.Expect(LogType.Exception, new Regex("boom"));
            _fake.Emit("deeplink", Link("https://x/1"));
            Grovs.Router.Drain();
            CollectionAssert.AreEqual(new[] { "https://x/1" }, got);

            LogAssert.Expect(LogType.Exception, new Regex("boom"));
            _fake.Emit("deeplink", Link("https://x/2"));
            Grovs.Router.Drain();
            CollectionAssert.AreEqual(new[] { "https://x/1", "https://x/2" }, got);
        }

        [Test]
        public void InAppEvent_Raised()
        {
            InAppMessageEvent got = null;
            Grovs.InAppMessageEvent += e => got = e;

            _fake.Emit("inapp_event", "{\"type\":\"clicked\",\"campaignId\":\"c1\",\"buttonId\":\"b1\",\"payload\":{\"k\":\"v\"}}");
            Grovs.Router.Drain();

            Assert.IsNotNull(got);
            Assert.AreEqual(InAppEventType.Clicked, got.Type);
            Assert.AreEqual("c1", got.CampaignId);
            Assert.AreEqual("b1", got.ButtonId);
            Assert.AreEqual("v", got.Payload["k"]);
        }

        [Test]
        public void Error_RaisedOnErrorOccurred()
        {
            GrovsError got = null;
            Grovs.ErrorOccurred += e => got = e;

            _fake.Emit("error", "{\"code\":\"authentication_failed\",\"message\":\"bad key\"}");
            Grovs.Router.Drain();

            Assert.AreEqual(GrovsErrorCode.AuthenticationFailed, got.Code);
            Assert.AreEqual("bad key", got.Message);
        }

        [Test]
        public void Log_MapsLevels()
        {
            LogAssert.Expect(LogType.Log, new Regex("hello"));
            LogAssert.Expect(LogType.Warning, new Regex("careful"));
            LogAssert.Expect(LogType.Error, new Regex("broken"));
            _fake.Emit("log", "{\"level\":\"info\",\"message\":\"hello\"}");
            _fake.Emit("log", "{\"level\":\"warn\",\"message\":\"careful\"}");
            _fake.Emit("log", "{\"level\":\"error\",\"message\":\"broken\"}");
            Grovs.Router.Drain();
        }

        [Test]
        public void BadMessage_IsLoggedAndDrainContinues()
        {
            GrovsError got = null;
            Grovs.ErrorOccurred += e => got = e;

            LogAssert.Expect(LogType.Error, new Regex("deeplink"));
            _fake.Emit("deeplink", "{not json");
            _fake.Emit("error", "{\"code\":\"unknown\",\"message\":\"m\"}");
            Grovs.Router.Drain();

            Assert.IsNotNull(got);
        }

        [Test]
        public void Enqueue_FromOtherThreads_AllDelivered()
        {
            var count = 0;
            Grovs.ErrorOccurred += e => count++;

            var threads = new List<System.Threading.Thread>();
            for (var t = 0; t < 4; t++)
            {
                var thread = new System.Threading.Thread(() =>
                {
                    for (var i = 0; i < 50; i++) _fake.Emit("error", "{\"code\":\"unknown\",\"message\":\"m\"}");
                });
                threads.Add(thread);
                thread.Start();
            }
            foreach (var thread in threads) thread.Join();

            Grovs.Router.Drain();
            Assert.AreEqual(200, count);
        }

        [Test]
        public void Drain_RetryFromCallback_StaysQueuedForNextDrain()
        {
            Grovs.ResetForTests(new GrovsStub());
            var attempts = 0;
            void Attempt() => Grovs.GenerateLink(new GenerateLinkOptions(), (link, error) =>
            {
                attempts++;
                if (error != null) Attempt();
            });

            Attempt();
            Grovs.Router.Drain();
            Assert.AreEqual(1, attempts);

            Grovs.Router.Drain();
            Assert.AreEqual(2, attempts);
        }

        [Test]
        public void NewRequest_FromOtherThreads_GivesUniqueIds()
        {
            var ids = new System.Collections.Concurrent.ConcurrentBag<int>();
            var threads = new List<System.Threading.Thread>();
            for (var t = 0; t < 4; t++)
            {
                var thread = new System.Threading.Thread(() =>
                {
                    for (var i = 0; i < 250; i++) ids.Add(Grovs.Router.NewRequest((v, e) => { }));
                });
                threads.Add(thread);
                thread.Start();
            }
            foreach (var thread in threads) thread.Join();

            Assert.AreEqual(1000, new HashSet<int>(ids).Count);
        }

        [Test]
        public void ShouldShow_NullDelegate_ReturnsTrue()
        {
            Grovs.ShouldShowInAppMessage = null;
            Assert.IsTrue(_fake.ShouldShow("c1"));

            Grovs.ShouldShowInAppMessage = id => id != "blocked";
            Assert.IsFalse(_fake.ShouldShow("blocked"));
            Assert.IsTrue(_fake.ShouldShow("c1"));
        }

        [Test]
        public void ShouldShow_DelegateThrows_ReturnsTrue()
        {
            Grovs.ShouldShowInAppMessage = id => throw new InvalidOperationException("veto failed");
            LogAssert.Expect(LogType.Exception, new Regex("veto failed"));
            Assert.IsTrue(_fake.ShouldShow("c1"));
        }
    }
}
