using System;
using System.Collections.Generic;
using System.Linq;
using GrovsSDK;
using NUnit.Framework;

namespace GrovsTestApp.Tests
{
    public class TestLogTests
    {
        class MemoryStore : ILinkHistoryStore
        {
            public string Json;
            public string Load() => Json;
            public void Save(string json) => Json = json;
        }

        DateTime _now;
        MemoryStore _store;
        TestLog _log;

        [SetUp]
        public void SetUp()
        {
            _now = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Local);
            _store = new MemoryStore();
            _log = new TestLog(_store, () => _now);
        }

        const string Link = "https://x.sqd.link/abc";

        LogEntry Open(string link, LinkSources source) =>
            _log.RecordLinkOpen(link, new Dictionary<string, object> { { "a", "b" } }, new GrovsTracking { Campaign = "c" }, source);

        [Test]
        public void Caps_entries_at_500_and_drops_the_oldest()
        {
            for (var i = 0; i < 501; i++) _log.Add(LogType.Sdk, LogState.Ok, "t" + i, "");
            Assert.AreEqual(500, _log.Entries.Count);
            Assert.AreEqual("t500", _log.Entries[0].Title);
            Assert.AreEqual("t1", _log.Entries[499].Title);
        }

        [Test]
        public void Ids_increase()
        {
            var a = _log.Add(LogType.Sdk, LogState.Ok, "a", "");
            var b = _log.Add(LogType.Sdk, LogState.Ok, "b", "");
            Assert.Greater(b.Id, a.Id);
        }

        [Test]
        public void Event_then_last_link_within_the_window_merge()
        {
            Open(Link, LinkSources.Event);
            _now = _now.AddSeconds(3);
            var merged = Open(Link, LinkSources.LastDeeplink);

            Assert.AreEqual(1, _log.Entries.Count);
            Assert.AreEqual(LinkSources.Event | LinkSources.LastDeeplink, merged.Sources);
            Assert.AreEqual("Opened from link", merged.Title);
            Assert.AreEqual("abc · event + last link", merged.Detail);
        }

        [Test]
        public void Last_link_then_event_also_merge()
        {
            Open(Link, LinkSources.LastDeeplink);
            _now = _now.AddSeconds(1);
            Open(Link, LinkSources.Event);
            Assert.AreEqual(1, _log.Entries.Count);
            Assert.AreEqual("abc · event + last link", _log.Entries[0].Detail);
        }

        [Test]
        public void Single_source_details()
        {
            Assert.AreEqual("abc · event", Open(Link, LinkSources.Event).Detail);
            _log.Clear();
            Assert.AreEqual("abc · last link", Open(Link, LinkSources.LastDeeplink).Detail);
            _log.Clear();
            Assert.AreEqual("no link · event", Open(null, LinkSources.Event).Detail);
        }

        [Test]
        public void Opens_six_seconds_apart_stay_separate()
        {
            Open(Link, LinkSources.Event);
            _now = _now.AddSeconds(6);
            Open(Link, LinkSources.LastDeeplink);
            Assert.AreEqual(2, _log.Entries.Count);
        }

        [Test]
        public void A_different_link_stays_separate()
        {
            Open(Link, LinkSources.Event);
            _now = _now.AddSeconds(1);
            Open("https://x.sqd.link/other", LinkSources.LastDeeplink);
            Assert.AreEqual(2, _log.Entries.Count);
        }

        [Test]
        public void The_same_source_twice_stays_separate()
        {
            Open(Link, LinkSources.Event);
            Open(Link, LinkSources.Event);
            Assert.AreEqual(2, _log.Entries.Count);
        }

        [Test]
        public void Merged_entry_code_is_the_call_code()
        {
            var data = new Dictionary<string, object> { { "a", "b" } };
            var tracking = new GrovsTracking { Campaign = "c" };
            _log.RecordLinkOpen(Link, data, tracking, LinkSources.Event);
            var merged = _log.RecordLinkOpen(Link, data, tracking, LinkSources.LastDeeplink);
            Assert.AreEqual(CallCode.Format("DeeplinkReceived", ("link", Link), ("data", data), ("tracking", tracking)), merged.Code);
        }

        [Test]
        public void Merge_raises_changed_and_keeps_position()
        {
            Open(Link, LinkSources.Event);
            _log.Add(LogType.Sdk, LogState.Ok, "later", "");
            var changed = 0;
            _log.Changed += () => changed++;
            _now = _now.AddSeconds(1);
            Open(Link, LinkSources.LastDeeplink);
            Assert.AreEqual(1, changed);
            Assert.AreEqual("later", _log.Entries[0].Title);
        }

        [Test]
        public void Changed_is_raised_by_every_mutation()
        {
            var changed = 0;
            _log.Changed += () => changed++;
            _log.Add(LogType.Sdk, LogState.Error, "x", "");
            _log.MarkSeen();
            _log.Clear();
            Assert.AreEqual(3, changed);
        }

        [Test]
        public void Unseen_error_is_set_by_an_error_and_cleared_by_mark_seen()
        {
            _log.Add(LogType.Sdk, LogState.Ok, "ok", "");
            Assert.IsFalse(_log.HasUnseenError);
            _log.Add(LogType.Sdk, LogState.Error, "bad", "");
            Assert.IsTrue(_log.HasUnseenError);
            _log.MarkSeen();
            Assert.IsFalse(_log.HasUnseenError);
        }

        [Test]
        public void Clear_empties_entries_and_history()
        {
            Open(Link, LinkSources.Event);
            _log.Add(LogType.Sdk, LogState.Error, "bad", "");
            _log.Clear();
            Assert.AreEqual(0, _log.Entries.Count);
            Assert.IsFalse(_log.HasUnseenError);
            Assert.AreEqual(0, new TestLog(_store, () => _now).Entries.Count);
        }

        [Test]
        public void Latest_link_is_the_newest_link_entry()
        {
            Assert.IsNull(_log.LatestLink);
            Open(Link, LinkSources.Event);
            _log.Add(LogType.Sdk, LogState.Ok, "x", "");
            Assert.AreEqual("Opened from link", _log.LatestLink.Title);
        }

        [Test]
        public void Link_history_is_saved_and_restored_for_link_entries_only()
        {
            Open(Link, LinkSources.Event);
            _now = _now.AddSeconds(1);
            Open(Link, LinkSources.LastDeeplink);
            _log.Add(LogType.Event, LogState.Ok, "not saved", "");

            var restored = new TestLog(_store, () => _now);
            Assert.AreEqual(1, restored.Entries.Count);
            var e = restored.Entries[0];
            Assert.AreEqual(LogType.Link, e.Type);
            Assert.AreEqual(LogState.Ok, e.State);
            Assert.AreEqual("abc · event + last link", e.Detail);
            Assert.AreEqual(Link, e.Link);
            Assert.AreEqual(LinkSources.Event | LinkSources.LastDeeplink, e.Sources);
            Assert.AreEqual(_log.Entries.Last(x => x.Type == LogType.Link).Code, e.Code);
            Assert.AreEqual(_log.LatestLink.Time.ToUniversalTime(), e.Time.ToUniversalTime());
            Assert.IsNull(e.Action);
        }

        [Test]
        public void Only_the_last_fifty_links_are_saved_and_ids_continue()
        {
            for (var i = 0; i < 60; i++)
            {
                _now = _now.AddMinutes(1);
                Open("https://x.sqd.link/l" + i, LinkSources.Event);
            }
            var restored = new TestLog(_store, () => _now);
            Assert.AreEqual(50, restored.Entries.Count);
            Assert.AreEqual("l59 · event", restored.Entries[0].Detail);
            Assert.Greater(restored.Entries[0].Id, restored.Entries[1].Id);
            var added = restored.Add(LogType.Sdk, LogState.Ok, "x", "");
            Assert.Greater(added.Id, restored.Entries[1].Id);
        }

        [TestCase("not json")]
        [TestCase("{\"a\":1}")]
        [TestCase("[1,2]")]
        [TestCase("[{\"time\":\"bad\"}]")]
        public void Corrupt_history_loads_as_empty(string json)
        {
            _store.Json = json;
            Assert.AreEqual(0, new TestLog(_store, () => _now).Entries.Count);
        }
    }
}
