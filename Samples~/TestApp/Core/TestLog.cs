using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GrovsSDK;

namespace GrovsTestApp
{
    public interface ILinkHistoryStore
    {
        string Load();
        void Save(string json);
    }

    public sealed class TestLog
    {
        public const int MaxEntries = 500;
        public const int MaxLinks = 50;
        static readonly TimeSpan MergeWindow = TimeSpan.FromSeconds(5);

        readonly ILinkHistoryStore _store;
        readonly Func<DateTime> _clock;
        readonly List<LogEntry> _entries;
        long _nextId = 1;

        public TestLog(ILinkHistoryStore store, Func<DateTime> clock)
        {
            _store = store;
            _clock = clock;
            _entries = LinkHistory.Decode(store.Load());
            for (var i = _entries.Count - 1; i >= 0; i--) _entries[i].Id = _nextId++;
        }

        public event Action Changed;

        /// <summary>Newest first.</summary>
        public IReadOnlyList<LogEntry> Entries => _entries;
        public bool HasUnseenError { get; private set; }
        public LogEntry LatestLink => _entries.FirstOrDefault(e => e.Type == LogType.Link);

        public LogEntry Add(LogType type, LogState state, string title, string detail, string code = null,
            TestAction action = null, string link = null)
        {
            var entry = new LogEntry
            {
                Id = _nextId++,
                Time = _clock(),
                Type = type,
                State = state,
                Title = title,
                Detail = detail,
                Code = code,
                Action = action,
                Link = link
            };
            Insert(entry);
            if (state == LogState.Error) HasUnseenError = true;
            if (type == LogType.Link) SaveLinks();
            Changed?.Invoke();
            return entry;
        }

        /// <summary>The event and the last-link read report the same open separately; both land on one entry.</summary>
        public LogEntry RecordLinkOpen(string link, IReadOnlyDictionary<string, object> data, GrovsTracking tracking,
            LinkSources source)
        {
            var now = _clock();
            var match = _entries.FirstOrDefault(e =>
                e.Type == LogType.Link && e.Link == link && (e.Sources & source) == 0 && now - e.Time <= MergeWindow);

            LogEntry result;
            if (match != null)
            {
                match.Sources |= source;
                match.Detail = LinkDetail(link, match.Sources);
                result = match;
            }
            else
            {
                result = new LogEntry
                {
                    Id = _nextId++,
                    Time = now,
                    Type = LogType.Link,
                    State = LogState.Ok,
                    Title = "Opened from link",
                    Detail = LinkDetail(link, source),
                    Code = CallCode.Format("DeeplinkReceived", ("link", link), ("data", data), ("tracking", tracking)),
                    Link = link,
                    Sources = source
                };
                Insert(result);
            }
            SaveLinks();
            Changed?.Invoke();
            return result;
        }

        public void MarkSeen()
        {
            HasUnseenError = false;
            Changed?.Invoke();
        }

        public void Clear()
        {
            _entries.Clear();
            HasUnseenError = false;
            SaveLinks();
            Changed?.Invoke();
        }

        void Insert(LogEntry entry)
        {
            _entries.Insert(0, entry);
            if (_entries.Count > MaxEntries) _entries.RemoveRange(MaxEntries, _entries.Count - MaxEntries);
        }

        void SaveLinks()
        {
            _store.Save(LinkHistory.Encode(_entries.Where(e => e.Type == LogType.Link).Take(MaxLinks)));
        }

        static string LinkDetail(string link, LinkSources sources)
        {
            var parts = new List<string>();
            if ((sources & LinkSources.Event) != 0) parts.Add("event");
            if ((sources & LinkSources.LastDeeplink) != 0) parts.Add("last link");
            return (LinkPath.Extract(link) ?? "no link") + " · " + string.Join(" + ", parts);
        }
    }

    static class LinkHistory
    {
        public static string Encode(IEnumerable<LogEntry> links)
        {
            var list = links.Select(e => (object)new Dictionary<string, object>
            {
                { "time", e.Time.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture) },
                { "type", e.Type.ToString() },
                { "state", e.State.ToString() },
                { "title", e.Title },
                { "detail", e.Detail },
                { "code", e.Code },
                { "link", e.Link },
                { "sources", (long)e.Sources }
            }).ToList();
            return MiniJson.Serialize(list);
        }

        /// <summary>Returns the stored links newest first. A corrupt value gives an empty list.</summary>
        public static List<LogEntry> Decode(string json)
        {
            var result = new List<LogEntry>();
            if (string.IsNullOrEmpty(json)) return result;
            try
            {
                var list = MiniJson.Parse(json) as List<object>;
                if (list == null) return result;
                foreach (var item in list)
                {
                    var map = (Dictionary<string, object>)item;
                    result.Add(new LogEntry
                    {
                        Time = DateTime.Parse((string)map["time"], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind).ToLocalTime(),
                        Type = (LogType)Enum.Parse(typeof(LogType), (string)map["type"]),
                        State = (LogState)Enum.Parse(typeof(LogState), (string)map["state"]),
                        Title = map["title"] as string,
                        Detail = map["detail"] as string,
                        Code = map["code"] as string,
                        Link = map["link"] as string,
                        Sources = (LinkSources)Convert.ToInt32(map["sources"], CultureInfo.InvariantCulture)
                    });
                }
            }
            catch (Exception)
            {
                return new List<LogEntry>();
            }
            return result.Take(TestLog.MaxLinks).ToList();
        }
    }
}
