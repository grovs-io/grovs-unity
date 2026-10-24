using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GrovsSDK;
using NUnit.Framework;

namespace GrovsTestApp.Tests
{
    public class ActionsTests
    {
        class NoStore : ILinkHistoryStore
        {
            public string Load() => null;
            public void Save(string json) { }
        }

        static readonly DateTime Now = new DateTime(2026, 10, 23, 12, 0, 0, DateTimeKind.Utc);

        FakeGrovsApi _api;
        CallStyle _style;
        TestRunner _runner;
        LinkActions _links;
        EventActions _events;
        InAppActions _inApp;
        IdentityActions _identity;

        [SetUp]
        public void SetUp()
        {
            _api = new FakeGrovsApi();
            _style = CallStyle.Async;
            _runner = new TestRunner(new TestLog(new NoStore(), () => Now), () => 0);
            _links = new LinkActions(_api, () => _style);
            _events = new EventActions(_api, () => Now, false);
            _inApp = new InAppActions(_api);
            _identity = new IdentityActions(_api);
        }

        LogEntry Run(TestAction a) => _runner.Run(a).GetAwaiter().GetResult();

        [Test]
        public void Generate_without_utm_or_redirects_sends_nulls()
        {
            var entry = Run(_links.Generate(new LinkForm()));
            var o = _api.LastOptions;
            Assert.IsNull(o.TrackingSource);
            Assert.IsNull(o.TrackingMedium);
            Assert.IsNull(o.TrackingCampaign);
            Assert.IsNull(o.IosRedirect);
            Assert.IsNull(o.AndroidRedirect);
            Assert.IsNull(o.DesktopRedirect);
            Assert.IsNull(o.ImageUrl);
            Assert.IsNull(o.Tags);
            Assert.AreEqual("Test title", o.Title);
            Assert.AreEqual("Test value", o.Data["param1"]);
            Assert.AreEqual(LogState.Ok, entry.State);
            StringAssert.StartsWith("async", entry.Detail);
            StringAssert.EndsWith("\n\n-> \"https://x.grovs.link/abc\"", entry.Code);
            StringAssert.StartsWith("GenerateLinkAsync(", entry.Code);
        }

        [Test]
        public void Generate_maps_blank_strings_and_empty_data_to_null()
        {
            Run(_links.Generate(new LinkForm { Title = "  ", Subtitle = "", Data = new List<KeyValueRow>() }));
            Assert.IsNull(_api.LastOptions.Title);
            Assert.IsNull(_api.LastOptions.Subtitle);
            Assert.IsNull(_api.LastOptions.Data);
        }

        [Test]
        public void Generate_with_utm_and_redirects()
        {
            var form = new LinkForm
            {
                Utm = true, UtmMedium = " ", CustomRedirects = true,
                Tags = new List<string> { "t" }, Image = "https://i",
                PreviewIos = true, ClipboardAndroid = false
            };
            Run(_links.Generate(form));
            var o = _api.LastOptions;
            Assert.AreEqual("test_source", o.TrackingSource);
            Assert.IsNull(o.TrackingMedium);
            Assert.AreEqual("test_campaign", o.TrackingCampaign);
            Assert.AreEqual("https://www.google.com", o.IosRedirect.Url);
            Assert.IsFalse(o.IosRedirect.OpenAppIfInstalled);
            Assert.IsNull(o.DesktopRedirect);
            Assert.AreEqual("https://i", o.ImageUrl);
            CollectionAssert.AreEqual(new[] { "t" }, o.Tags);
            Assert.AreEqual(true, o.ShowPreviewIos);
            Assert.IsNull(o.ShowPreviewAndroid);
            Assert.AreEqual(false, o.CopyToClipboardAndroid);
        }

        [Test]
        public void Generate_callback_style_uses_the_callback_method_only()
        {
            _style = CallStyle.Callback;
            var entry = Run(_links.Generate(new LinkForm()));
            CollectionAssert.AreEqual(new[] { "GenerateLink" }, _api.Calls);
            StringAssert.StartsWith("callback", entry.Detail);
            Assert.AreEqual(LogState.Ok, entry.State);
        }

        [Test]
        public void Generate_callback_error_is_logged_as_error()
        {
            _style = CallStyle.Callback;
            _api.CallbackError = new GrovsError { Code = GrovsErrorCode.SdkDisabled, Message = "off" };
            var entry = Run(_links.Generate(new LinkForm()));
            Assert.AreEqual(LogState.Error, entry.State);
            Assert.AreEqual("SdkDisabled: off", entry.Detail);
        }

        [Test]
        public void Inspect_counts_fields_and_appends_the_map()
        {
            _api.DetailsResult = new Dictionary<string, object> { { "a", 1L }, { "b", "x" } };
            var entry = Run(_links.Inspect("abc"));
            Assert.AreEqual(LogType.Inspect, entry.Type);
            Assert.AreEqual("Link details abc", entry.Title);
            StringAssert.StartsWith("2 fields · async", entry.Detail);
            StringAssert.EndsWith("\n\n-> { a: 1, b: \"x\" }", entry.Code);
            Assert.AreEqual("abc", _api.LastDetailsPath);

            _style = CallStyle.Callback;
            Run(_links.Inspect("abc"));
            Assert.AreEqual("GetLinkDetails", _api.Calls[_api.Calls.Count - 1]);
        }

        [Test]
        public void Track_without_rows_passes_null_properties()
        {
            var entry = Run(_events.Track(new EventForm { Tags = new List<string> { "a", "b" } }, false));
            Assert.IsNull(_api.LastTrackProperties);
            Assert.AreEqual("demo_tapped", _api.LastTrackName);
            Assert.AreEqual("track demo_tapped", entry.Title);
            StringAssert.StartsWith("tags a, b", entry.Detail);
        }

        [Test]
        public void Track_plain_says_sent()
        {
            var entry = Run(_events.Track(new EventForm(), false));
            StringAssert.StartsWith("sent", entry.Detail);
            Assert.IsNull(_api.LastTrackTags);
        }

        [Test]
        public void Track_anyway_calls_the_sdk_and_warns()
        {
            var entry = Run(_events.Track(new EventForm { Name = "install" }, true));
            Assert.AreEqual("install", _api.LastTrackName);
            Assert.AreEqual(LogState.Warning, entry.State);
            StringAssert.StartsWith("sent anyway, the SDK should reject it", entry.Detail);
        }

        [Test]
        public void Screen_sends_name_and_properties()
        {
            var form = new ScreenForm();
            form.Properties.Add(new KeyValueRow { Key = "k", Value = "1", Kind = ValueKind.Number });
            var entry = Run(_events.Screen(form));
            Assert.AreEqual(LogType.Screen, entry.Type);
            Assert.AreEqual("Screen Checkout", entry.Title);
            Assert.AreEqual(1L, _api.LastScreenProperties["k"]);
        }

        [Test]
        public void Purchase_sends_cents_and_back_dated_time()
        {
            var entry = Run(_events.Purchase(new PurchaseForm { DaysAgo = 30 }));
            var p = _api.LastPurchase;
            Assert.AreEqual(GrovsTransactionType.Buy, p.type);
            Assert.AreEqual(499, p.cents);
            Assert.AreEqual("USD", p.currency);
            Assert.AreEqual("pro_monthly", p.product);
            Assert.AreEqual(Now.AddDays(-30), p.date);
            Assert.AreEqual("Log $4.99 buy · USD · pro_monthly", entry.Title);
            StringAssert.StartsWith("499 cents · pro_monthly", entry.Detail);
        }

        [Test]
        public void Purchase_today_still_passes_a_date()
        {
            Run(_events.Purchase(new PurchaseForm()));
            Assert.AreEqual(Now, _api.LastPurchase.date);
        }

        [Test]
        public void Purchase_with_bad_amount_logs_an_error_and_does_not_call()
        {
            var entry = Run(_events.Purchase(new PurchaseForm { Amount = "abc" }));
            Assert.AreEqual(LogState.Error, entry.State);
            Assert.AreEqual("Enter an amount like 4.99", entry.Detail);
            CollectionAssert.IsEmpty(_api.Calls);
        }

        [Test]
        public void Store_receipt_title_depends_on_platform()
        {
            Assert.AreEqual("Google Play purchase", Run(_events.StoreReceipt("json")).Title);
            Assert.AreEqual("json", _api.LastReceipt);
            var ios = new EventActions(_api, () => Now, true);
            Assert.AreEqual("App Store purchase", Run(ios.StoreReceipt("123")).Title);
        }

        [Test]
        public void Global_tags_and_aliases()
        {
            var tags = _events.GlobalTags(new List<string>());
            Assert.IsFalse(tags.Repeatable);
            Run(tags);
            Assert.IsNull(_api.LastGlobalTags);

            var aliases = _events.Aliases(new SetupForm().Aliases);
            Assert.IsFalse(aliases.Repeatable);
            var entry = Run(aliases);
            Assert.AreEqual("Screen aliases set", entry.Title);
            StringAssert.StartsWith("1 aliases", entry.Detail);
            Assert.AreEqual("Home", _api.LastAliases["TestApp"]);
        }

        [Test]
        public void In_app_actions_titles_details_and_repeatable()
        {
            var cases = new (TestAction action, string title, string detail, bool repeatable)[]
            {
                (_inApp.Pause(), "Pause messages", "paused", false),
                (_inApp.Resume(), "Resume messages", "running", false),
                (_inApp.Trigger("go"), "Trigger go", "sent", true),
                (_inApp.DiscardWaiting(), "Discard waiting messages", "paused", true),
                (_inApp.CloseCurrent(), "Close current message", "paused", true),
                (_inApp.SetDebug(true), "Debug logging on", "applied", false),
                (_inApp.SetDebug(false), "Debug logging off", "applied", false),
                (_inApp.SetExcluded(new List<string> { "A", "B" }), "Excluded screens set", "A, B", false),
                (_inApp.SetExcluded(new List<string>()), "Excluded screens set", "none", false),
            };
            foreach (var c in cases)
            {
                Assert.AreEqual(c.repeatable, c.action.Repeatable, c.title);
                var entry = Run(c.action);
                Assert.AreEqual(c.title, entry.Title);
                StringAssert.StartsWith(c.detail, entry.Detail, c.title);
            }
            Assert.AreEqual("go", _api.LastTrackName);
        }

        [Test]
        public void Track_replays_the_tags_it_was_built_with()
        {
            var form = new EventForm { Tags = new List<string> { "a", "b" } };
            var action = _events.Track(form, false);
            form.Tags.Clear();
            form.Tags.Add("c");
            Run(action);
            CollectionAssert.AreEqual(new[] { "a", "b" }, _api.LastTrackTags);
        }

        [Test]
        public void Generate_replays_the_tags_it_was_built_with()
        {
            var form = new LinkForm { Tags = new List<string> { "a", "b" } };
            var action = _links.Generate(form);
            form.Tags.Clear();
            form.Tags.Add("c");
            Run(action);
            CollectionAssert.AreEqual(new[] { "a", "b" }, _api.LastOptions.Tags);
        }

        [Test]
        public void Set_excluded_and_global_tags_replay_the_lists_they_were_built_with()
        {
            var screens = new List<string> { "A", "B" };
            var excluded = _inApp.SetExcluded(screens);
            var tags = new List<string> { "x" };
            var global = _events.GlobalTags(tags);
            screens.Clear();
            tags.Clear();
            Run(excluded);
            Run(global);
            CollectionAssert.AreEqual(new[] { "A", "B" }, _api.LastExcluded);
            CollectionAssert.AreEqual(new[] { "x" }, _api.LastGlobalTags);
        }

        [Test]
        public void Discard_waiting_and_close_current_pass_the_flag()
        {
            Run(_inApp.CloseCurrent());
            Assert.IsTrue(_api.LastDiscardDismiss.Value);
        }

        [Test]
        public void Identity_actions()
        {
            var on = _identity.SetEnabled(false);
            Assert.IsFalse(on.Repeatable);
            var entry = Run(on);
            Assert.AreEqual("SDK disabled", entry.Title);
            StringAssert.StartsWith("holding events", entry.Detail);
            Assert.AreEqual("SDK enabled", Run(_identity.SetEnabled(true)).Title);

            var save = _identity.Save(new IdentityForm { Identifier = "me" });
            Assert.IsFalse(save.Repeatable);
            entry = Run(save);
            Assert.AreEqual("Identity saved", entry.Title);
            StringAssert.StartsWith("me", entry.Detail);
            Assert.AreEqual("me", _api.LastIdentifier);
            Assert.AreEqual(123L, _api.LastAttributes["param2"]);
            Assert.AreEqual(true, _api.LastAttributes["param3"]);

            entry = Run(_identity.Save(new IdentityForm { Identifier = " " }));
            Assert.IsNull(_api.LastIdentifier);
            StringAssert.StartsWith("no identifier", entry.Detail);
        }
    }
}
