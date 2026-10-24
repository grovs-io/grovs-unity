using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using GrovsSDK;
using UnityEngine;
using UnityEngine.UIElements;

namespace GrovsTestApp
{
    /// <summary>
    /// The objects every screen shares. Created once, by the first AppRoot, and kept for the whole run,
    /// so the SDK subscriptions and the startup calls happen once even when scenes change.
    /// </summary>
    public sealed class AppContext
    {
        const string LinkHistoryKey = "grovs_test_app.link_history";

        static AppContext _current;
        static readonly Dictionary<Tab, Func<AppContext, VisualElement>> ScreenFactories =
            new Dictionary<Tab, Func<AppContext, VisualElement>>();
        static Func<AppContext, VisualElement> _settingsFactory;

        public static AppContext Current => _current ??= new AppContext();

        /// <summary>
        /// Play Mode can start without a domain reload, which keeps static fields. The SDK clears its
        /// events at this point, so the context is dropped too and the next access subscribes again.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ResetStatics()
        {
            _current = null;
            ScreenFactories.Clear();
            _settingsFactory = null;
        }

        /// <summary>
        /// The screen every scaffold shows for a tab. Every scene builds a new scaffold, so screens are
        /// registered as factories, for example from a BeforeSceneLoad method in the screen's file.
        /// </summary>
        public static void RegisterScreen(Tab tab, Func<AppContext, VisualElement> create) => ScreenFactories[tab] = create;

        public static void RegisterSettings(Func<AppContext, VisualElement> create) => _settingsFactory = create;

        /// <summary>Puts the registered screens into a new scaffold. Tabs without one keep their placeholder.</summary>
        internal void ApplyScreens(AppScaffold scaffold)
        {
            foreach (var pair in ScreenFactories) scaffold.SetScreen(pair.Key, pair.Value(this));
            if (_settingsFactory != null) scaffold.SetSettingsScreen(_settingsFactory(this));
        }

        public TestLog Log { get; }
        public TestRunner Runner { get; }
        public FormStore Store { get; }
        public IGrovsApi Api { get; }
        public INativeServices Native { get; }
        public BakedConfig Config { get; }
        public DeveloperPrefs Developer { get; }
        public LinkActions Links { get; }
        public EventActions Events { get; }
        public InAppActions InApp { get; }
        public IdentityActions Identity { get; }

        /// <summary>The Links tab of the scene that is showing now.</summary>
        public LinksScreen LinksScreen { get; internal set; }

        /// <summary>The Events tab of the scene that is showing now.</summary>
        public EventsScreen EventsScreen { get; internal set; }

        /// <summary>The scaffold of the scene that is showing now.</summary>
        public AppScaffold Scaffold { get; internal set; }

        /// <summary>The tab a new scaffold opens on, so a scene change comes back to where the user was.</summary>
        public Tab SelectedTab { get; set; } = Tab.Home;

        /// <summary>
        /// Whether in-app messages are running. The SDK has no getter, so this follows what the app last asked for.
        /// </summary>
        public bool InAppRunning { get; set; } = true;

        public NotificationStatus Notifications { get; private set; }

        /// <summary>Raised when the app comes back to the front, after the link and notification status are read again.</summary>
        public event Action Resumed;

        readonly ConcurrentQueue<string> _blocked = new ConcurrentQueue<string>();
        volatile bool _blockAll;
        GrovsDeeplink _recordedLastDeeplink;

        /// <summary>Not saved: every launch starts with messages allowed.</summary>
        public bool BlockAll
        {
            get => _blockAll;
            set => _blockAll = value;
        }

        AppContext()
        {
            var prefs = new PlayerPrefsStore();
            var clock = Stopwatch.StartNew();
            Log = new TestLog(new PrefsLinkHistory(prefs, LinkHistoryKey), () => DateTime.Now);
            Runner = new TestRunner(Log, () => clock.Elapsed.TotalMilliseconds);
            Store = new FormStore(prefs);
            Api = new GrovsApi();
            Native = NativeServices.Current;
            Config = Native.ReadBakedConfig();
            Developer = Store.Load<DeveloperPrefs>(FormKeys.Developer);
            Links = new LinkActions(Api, () => Developer.CallStyle);
            Events = new EventActions(Api, () => DateTime.Now, Application.platform == RuntimePlatform.IPhonePlayer);
            InApp = new InAppActions(Api);
            Identity = new IdentityActions(Api);

            ApplySavedSettings();
            Subscribe();
            ReadLastDeeplink();
            Log.Add(LogType.Sdk, LogState.Ok, "SDK configured", SdkLog.ConfiguredDetail(Config), SdkLog.ConfiguredCode(Config));
            Notifications = Native.GetNotificationStatus();
        }

        /// <summary>Sends the saved trigger as an event. A trigger that worked goes to the front of the recent ones.</summary>
        public async Task<LogEntry> FireTrigger(InAppPrefs prefs)
        {
            var name = prefs.Trigger.Trim();
            var entry = await Runner.Run(InApp.Trigger(name));
            if (entry.State == LogState.Error) return entry;
            Recents.Remember(prefs.RecentTriggers, name);
            Store.Save(FormKeys.InApp, prefs);
            return entry;
        }

        public void Rerun(TestAction action) => _ = Runner.Run(action);

        /// <summary>Opens the Links tab on the link and shows what Grovs stores for it in the logs.</summary>
        public async void InspectLink(string link)
        {
            if (LinksScreen != null)
            {
                Scaffold?.ShowTab(Tab.Links);
                LinksScreen.Inspect(link);
                return;
            }
            var path = LinkPath.Extract(link);
            if (path == null) return;
            var entry = await Runner.Run(Links.Inspect(path));
            Scaffold?.OpenLogs(entry);
        }

        public void RefreshNotifications() => Notifications = Native.GetNotificationStatus();

        internal void OnResume()
        {
            ReadLastDeeplink();
            RefreshNotifications();
            Resumed?.Invoke();
        }

        /// <summary>Logs the messages the native thread blocked. Runs on the main thread.</summary>
        internal void FlushBlocked()
        {
            while (_blocked.TryDequeue(out var id))
            {
                Log.Add(LogType.InApp, LogState.Warning, "Blocked " + id, "shouldShowInAppMessage returned false",
                    CallCode.Format("ShouldShowInAppMessage", ("campaignId", id)));
            }
        }

        void ApplySavedSettings()
        {
            var inApp = Store.Load<InAppPrefs>(FormKeys.InApp);
            var identity = Store.Load<IdentityForm>(FormKeys.Identity);
            var setup = Store.Load<SetupForm>(FormKeys.Setup);
            ApplySilently(InApp.SetExcluded(inApp.Excluded));
            ApplySilently(InApp.SetDebug(inApp.DebugLogging));
            Grovs.ShouldShowInAppMessage = AllowMessage;

            ApplySilently(new TestAction
            {
                Title = "Debug level",
                Code = CallCode.Format("SetLogLevel", ("level", Developer.LogLevel)),
                Run = () =>
                {
                    Api.SetLogLevel(Developer.LogLevel);
                    return Task.FromResult(new ActionResult());
                }
            });
            ApplySilently(Identity.Save(identity));
            ApplySilently(Events.GlobalTags(setup.GlobalTags));
            ApplySilently(Events.Aliases(setup.Aliases));
        }

        // Called on a native thread: no Unity APIs and no log access here.
        bool AllowMessage(string campaignId)
        {
            if (!_blockAll) return true;
            _blocked.Enqueue(campaignId);
            return false;
        }

        /// <summary>Applies a saved setting at launch. Only a failure is logged.</summary>
        void ApplySilently(TestAction action)
        {
            try
            {
                var task = action.Run();
                if (task.IsFaulted) throw task.Exception.GetBaseException();
            }
            catch (Exception e)
            {
                var detail = TestRunner.Describe(e);
                Log.Add(LogType.Sdk, LogState.Error, action.Title + " failed", detail, action.Code + "\n\n-> " + e.GetType().Name + ": " + detail);
            }
        }

        void Subscribe()
        {
            Grovs.DeeplinkReceived += OnDeeplink;
            Grovs.ErrorOccurred += error =>
                Log.Add(LogType.Sdk, LogState.Error, "SDK error " + error.Code, error.Message,
                    CallCode.Format("ErrorOccurred", ("Code", error.Code.ToString()), ("Message", error.Message)));
            Grovs.InAppMessageEvent += e =>
            {
                var (state, title, detail, code) = SdkLog.InApp(e);
                Log.Add(LogType.InApp, state, title, detail, code);
            };
        }

        void OnDeeplink(GrovsDeeplink deeplink)
        {
            Log.RecordLinkOpen(deeplink.Link, deeplink.Data, deeplink.Tracking, LinkSources.Event);
            if (deeplink.FromPush)
            {
                Log.Add(LogType.Push, LogState.Ok, "Push opened", SdkLog.Keys(deeplink.Data),
                    CallCode.Format("DeeplinkReceived", ("FromPush", true), ("Data", deeplink.Data)));
            }
            ReadLastDeeplink();
        }

        /// <summary>Records LastDeeplink once per link the SDK hands over, not on every read.</summary>
        void ReadLastDeeplink()
        {
            var last = Grovs.LastDeeplink;
            if (last == null || ReferenceEquals(last, _recordedLastDeeplink)) return;
            _recordedLastDeeplink = last;
            Log.RecordLinkOpen(last.Link, last.Data, last.Tracking, LinkSources.LastDeeplink);
        }
    }

    /// <summary>How SDK callbacks and the startup config read in the log.</summary>
    public static class SdkLog
    {
        public static (LogState state, string title, string detail, string code) InApp(InAppMessageEvent e)
        {
            var code = CallCode.Format("InAppMessageEvent", ("Type", e.Type.ToString()), ("CampaignId", e.CampaignId),
                ("ButtonId", e.ButtonId), ("Payload", e.Payload), ("DismissReason", e.DismissReason),
                ("AnswerValue", e.AnswerValue), ("Comment", e.Comment), ("FailureMessage", e.FailureMessage));
            switch (e.Type)
            {
                case InAppEventType.Shown:
                    return (LogState.Ok, "Message " + e.CampaignId + " shown", "", code);
                case InAppEventType.Clicked:
                    var payload = e.Payload == null || e.Payload.Count == 0 ? "no payload" : Keys(e.Payload);
                    return (LogState.Ok, "Clicked " + (e.ButtonId ?? "message"), e.CampaignId + " · " + payload, code);
                case InAppEventType.Dismissed:
                    return (LogState.Neutral, "Dismissed " + e.CampaignId, e.DismissReason ?? "", code);
                case InAppEventType.Answered:
                    var parts = new[] { Convert.ToString(e.AnswerValue, CultureInfo.InvariantCulture), e.Comment }
                        .Where(p => !string.IsNullOrEmpty(p));
                    return (LogState.Ok, "Answered " + e.CampaignId, string.Join(" · ", parts), code);
                default:
                    return (LogState.Error, "Message " + e.CampaignId + " failed", e.FailureMessage ?? "", code);
            }
        }

        public static string Keys(IReadOnlyDictionary<string, object> data) =>
            data == null ? "" : string.Join(", ", data.Keys);

        public static string ConfiguredDetail(BakedConfig config) =>
            (config.UseTestEnvironment ? "test" : "prod") + " · " + (config.BaseUrl ?? "SDK default");

        public static string ConfiguredCode(BakedConfig config) =>
            CallCode.Format("configure",
                ("apiKey", TextFormat.MiddleEllipsis(config.ApiKey)),
                ("useTestEnvironment", config.UseTestEnvironment),
                ("baseUrl", config.BaseUrl),
                ("clipboardDomains", config.ClipboardDomains),
                ("trackScenes", config.TrackScenes),
                ("startEnabled", config.StartEnabled));
    }

    sealed class PrefsLinkHistory : ILinkHistoryStore
    {
        readonly IPrefs _prefs;
        readonly string _key;

        public PrefsLinkHistory(IPrefs prefs, string key)
        {
            _prefs = prefs;
            _key = key;
        }

        public string Load() => _prefs.Get(_key);

        public void Save(string json) => _prefs.Set(_key, json);
    }
}
