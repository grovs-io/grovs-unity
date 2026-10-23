using System.Collections.Generic;
using GrovsSDK;
using NUnit.Framework;

namespace GrovsTestApp.Tests
{
    public class MemoryPrefs : IPrefs
    {
        public readonly Dictionary<string, string> Values = new Dictionary<string, string>();
        public string Get(string key) => Values.TryGetValue(key, out var v) ? v : null;
        public void Set(string key, string value) => Values[key] = value;
        public void Delete(string key) => Values.Remove(key);
    }

    public class FormStoreTests
    {
        MemoryPrefs _prefs;
        FormStore _store;

        [SetUp]
        public void SetUp()
        {
            _prefs = new MemoryPrefs();
            _store = new FormStore(_prefs);
        }

        [Test]
        public void Load_without_saved_value_gives_defaults()
        {
            var form = _store.Load<LinkForm>(FormKeys.Link);
            Assert.AreEqual("Test title", form.Title);
            Assert.AreEqual(1, form.Data.Count);
            Assert.AreEqual("param1", form.Data[0].Key);
            Assert.IsNull(form.PreviewAndroid);
        }

        [Test]
        public void Link_form_round_trips_including_tri_states()
        {
            var form = new LinkForm
            {
                Title = "Changed",
                Utm = true,
                CustomRedirects = true,
                Tags = new List<string> { "a", "b" },
                PreviewAndroid = true,
                ClipboardIos = false,
                Desktop = new RedirectForm { Url = "https://d.example", OpenAppIfInstalled = true }
            };
            form.Data.Add(new KeyValueRow { Key = "n", Value = "5", Kind = ValueKind.Number });
            _store.Save(FormKeys.Link, form);

            var loaded = _store.Load<LinkForm>(FormKeys.Link);
            Assert.AreEqual("Changed", loaded.Title);
            Assert.IsTrue(loaded.Utm);
            CollectionAssert.AreEqual(new[] { "a", "b" }, loaded.Tags);
            Assert.AreEqual(true, loaded.PreviewAndroid);
            Assert.IsNull(loaded.PreviewIos);
            Assert.AreEqual(false, loaded.ClipboardIos);
            Assert.AreEqual("https://d.example", loaded.Desktop.Url);
            Assert.IsTrue(loaded.Desktop.OpenAppIfInstalled);
            Assert.AreEqual(2, loaded.Data.Count);
            Assert.AreEqual(ValueKind.Number, loaded.Data[1].Kind);
        }

        [Test]
        public void Other_forms_round_trip()
        {
            _store.Save(FormKeys.Event, new EventForm { Name = "x", Recent = new List<string> { "a" } });
            Assert.AreEqual("x", _store.Load<EventForm>(FormKeys.Event).Name);
            Assert.AreEqual("a", _store.Load<EventForm>(FormKeys.Event).Recent[0]);

            _store.Save(FormKeys.Screen, new ScreenForm { Name = "Cart" });
            Assert.AreEqual("Cart", _store.Load<ScreenForm>(FormKeys.Screen).Name);

            _store.Save(FormKeys.Purchase, new PurchaseForm { Type = GrovsTransactionType.Refund, DaysAgo = 3 });
            var purchase = _store.Load<PurchaseForm>(FormKeys.Purchase);
            Assert.AreEqual(GrovsTransactionType.Refund, purchase.Type);
            Assert.AreEqual(3, purchase.DaysAgo);

            _store.Save(FormKeys.Identity, new IdentityForm { Identifier = "me" });
            Assert.AreEqual("me", _store.Load<IdentityForm>(FormKeys.Identity).Identifier);
            Assert.AreEqual(3, _store.Load<IdentityForm>(FormKeys.Identity).Attributes.Count);

            _store.Save(FormKeys.Setup, new SetupForm { GlobalTags = new List<string> { "t" } });
            Assert.AreEqual("t", _store.Load<SetupForm>(FormKeys.Setup).GlobalTags[0]);

            _store.Save(FormKeys.InApp, new InAppPrefs { DebugLogging = false });
            Assert.IsFalse(_store.Load<InAppPrefs>(FormKeys.InApp).DebugLogging);

            _store.Save(FormKeys.Developer, new DeveloperPrefs { LogLevel = GrovsLogLevel.Error, CallStyle = CallStyle.Callback });
            var dev = _store.Load<DeveloperPrefs>(FormKeys.Developer);
            Assert.AreEqual(GrovsLogLevel.Error, dev.LogLevel);
            Assert.AreEqual(CallStyle.Callback, dev.CallStyle);
        }

        [Test]
        public void Corrupt_value_loads_defaults()
        {
            _prefs.Set(FormKeys.Event, "{not json");
            Assert.AreEqual("demo_tapped", _store.Load<EventForm>(FormKeys.Event).Name);
        }

        [Test]
        public void Reset_deletes_the_key_and_returns_defaults()
        {
            _store.Save(FormKeys.Event, new EventForm { Name = "x" });
            var form = _store.Reset<EventForm>(FormKeys.Event);
            Assert.AreEqual("demo_tapped", form.Name);
            Assert.IsNull(_prefs.Get(FormKeys.Event));
        }

        [Test]
        public void Defaults_match_the_spec()
        {
            var link = new LinkForm();
            Assert.AreEqual("Test subtitle", link.Subtitle);
            Assert.AreEqual("", link.Image);
            Assert.IsFalse(link.Utm);
            Assert.AreEqual("test_source", link.UtmSource);
            Assert.AreEqual("https://www.youtube.com", link.Android.Url);
            Assert.AreEqual("https://www.google.com", link.Ios.Url);
            Assert.AreEqual("", link.Desktop.Url);

            var purchase = new PurchaseForm();
            Assert.AreEqual(GrovsTransactionType.Buy, purchase.Type);
            Assert.AreEqual("4.99", purchase.Amount);
            Assert.AreEqual("pro_monthly", purchase.Product);

            var identity = new IdentityForm();
            Assert.AreEqual(ValueKind.Text, identity.Attributes[0].Kind);
            Assert.AreEqual(ValueKind.Number, identity.Attributes[1].Kind);
            Assert.AreEqual(ValueKind.Bool, identity.Attributes[2].Kind);

            Assert.AreEqual("Home", new SetupForm().Aliases[0].Value);
            CollectionAssert.AreEqual(new[] { "ExcludedScreen" }, new InAppPrefs().Excluded);
            Assert.AreEqual(GrovsLogLevel.Info, new DeveloperPrefs().LogLevel);
            Assert.AreEqual(CallStyle.Async, new DeveloperPrefs().CallStyle);
        }

        [Test]
        public void Remember_moves_to_front_dedupes_and_keeps_five()
        {
            var recent = new List<string> { "a", "b", "c", "d", "e" };
            Recents.Remember(recent, "c");
            CollectionAssert.AreEqual(new[] { "c", "a", "b", "d", "e" }, recent);
            Recents.Remember(recent, "f");
            CollectionAssert.AreEqual(new[] { "f", "c", "a", "b", "d" }, recent);
            Recents.Remember(recent, "F");
            Assert.AreEqual("F", recent[0]);
            Assert.AreEqual(5, recent.Count);
        }

        [Test]
        public void Summaries_and_labels()
        {
            var purchase = new PurchaseForm { Type = GrovsTransactionType.Refund, Amount = "10", Currency = "EUR" };
            Assert.AreEqual("Log €10.00 refund · EUR · pro_monthly", purchase.Label());
            Assert.AreEqual("Log $4.99 buy", new PurchaseForm().ShortLabel());
            Assert.AreEqual("Log purchase", new PurchaseForm { Amount = "x" }.ShortLabel());

            Assert.AreEqual("UTM off · no tags · no image · no redirects", new LinkForm().Summary());
            var link = new LinkForm { Utm = true, Tags = new List<string> { "a", "b" }, Image = "https://i", CustomRedirects = true };
            Assert.AreEqual("UTM on · 2 tags · image · redirects on", link.Summary());
        }
    }
}
