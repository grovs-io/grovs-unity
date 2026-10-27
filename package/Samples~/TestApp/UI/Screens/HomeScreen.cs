using System;
using System.Collections.Generic;
using System.Linq;
using GrovsSDK;
using UnityEngine;
using UnityEngine.UIElements;

namespace GrovsTestApp
{
    public enum NotificationAllowAction { Request, OpenSettings }

    /// <summary>The decisions the Home tab makes, kept apart from the views so they can be tested.</summary>
    public static class HomeRules
    {
        public static readonly TimeSpan LinkSettleWindow = TimeSpan.FromSeconds(5);
        public const string NotAvailableOnIos = "Not available on iOS";
        public static readonly string[] CheckedSources = { "event", "last link" };

        /// <summary>
        /// Both sources: ok, drawn with a check after each name. One source still inside the window: waiting.
        /// One source after the window: the other one missed.
        /// </summary>
        public static (string text, LogState state) OpenedFromPill(LinkSources sources, TimeSpan age)
        {
            var both = LinkSources.Event | LinkSources.LastDeeplink;
            if ((sources & both) == both) return (string.Join(" ", CheckedSources), LogState.Ok);
            if (age <= LinkSettleWindow) return ("waiting", LogState.Neutral);
            return ((sources & LinkSources.Event) == 0 ? "event missed" : "last link missed", LogState.Error);
        }

        /// <summary>The text shown while the push token row has no token.</summary>
        public static string PushTokenPlaceholder(bool ios, bool requesting)
        {
            if (ios) return NotAvailableOnIos;
            return requesting ? "Waiting…" : "Not available in this build";
        }

        /// <summary>Ask once. After a denial in this install the system prompt will not show again, so open the app settings.</summary>
        public static NotificationAllowAction AllowAction(NotificationStatus status, bool requestedBefore) =>
            status == NotificationStatus.NotAllowed && requestedBefore
                ? NotificationAllowAction.OpenSettings
                : NotificationAllowAction.Request;

        /// <summary>After a Track run: a name the SDK accepted goes to the front of the recent ones. Returns whether the form changed.</summary>
        public static bool RememberTracked(EventForm form, LogState state)
        {
            if (state == LogState.Error) return false;
            Recents.Remember(form.Recent, form.Name.Trim());
            return true;
        }

        public static bool IsWebUrl(string text)
        {
            return Uri.TryCreate(text?.Trim(), UriKind.Absolute, out var uri)
                   && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }

        public static string Environment(BakedConfig config) => config.UseTestEnvironment ? "Test" : "Prod";

        public static string Caption(BakedConfig config, string sdkVersion) => Environment(config) + " · " + sdkVersion;

        public static string AttributesSummary(IEnumerable<KeyValueRow> rows)
        {
            var parts = rows.Where(r => !string.IsNullOrWhiteSpace(r.Key)).Select(r => r.Key + "=" + r.Value);
            var text = string.Join(", ", parts);
            return text.Length == 0 ? "none" : text;
        }
    }

    /// <summary>Asks for the push token and ignores new asks while one is still waiting for its answer.</summary>
    public sealed class PushTokenRequest
    {
        readonly Action<Action<string>> _get;

        public bool Requesting { get; private set; }
        public string Token { get; private set; }

        public event Action Changed;

        public PushTokenRequest(Action<Action<string>> get)
        {
            _get = get;
        }

        /// <summary>Returns false when a request is already running.</summary>
        public bool Start()
        {
            if (Requesting) return false;
            Requesting = true;
            Changed?.Invoke();
            _get(token =>
            {
                Requesting = false;
                Token = string.IsNullOrEmpty(token) ? null : token;
                Changed?.Invoke();
            });
            return true;
        }
    }

    /// <summary>A pill whose sources each end with a drawn check mark.</summary>
    sealed class CheckedPill : VisualElement
    {
        public const string CheckPath = "M5 12.5 L10 17.5 L19 7";

        public CheckedPill(IEnumerable<string> sources, LogState state)
        {
            AddToClassList("status-pill");
            AddToClassList("status-pill--checks");
            Ui.RoundAsPill(this);
            Ui.SetStateClass(this, state);
            var first = true;
            foreach (var source in sources)
            {
                var label = Ui.Text(source, "t-caption", "status-pill__text");
                label.EnableInClassList("status-pill__text--next", !first);
                Add(label);
                Add(new StrokeIcon(CheckPath, 2.6f));
                first = false;
            }
        }
    }

    /// <summary>The Home tab: status, the last opened link, the user identity, and quick actions.</summary>
    public class HomeScreen : VisualElement
    {
        const string NotificationsRequestedKey = "grovs_test_app.notifications_requested";
        const long OpenedFromRefreshMs = 30000;

        readonly AppContext _ctx;
        readonly ScrollView _scroll;
        readonly VisualElement _notificationsHost;
        readonly VisualElement _pushHost;
        readonly VisualElement _openedBody;
        readonly VisualElement _identityHost;
        readonly SwitchRow _enabled;
        readonly Label _openedTrailing;
        readonly AppButton _generate;
        readonly AppButton _track;
        readonly AppButton _purchase;
        IVisualElementScheduledItem _openedRefresh;
        IVisualElementScheduledItem _openedSettle;
        IdentityForm _identity;
        IdentityForm _draft;
        readonly PushTokenRequest _push;
        bool _editingIdentity;
        bool _generating;

        bool IsIos => Application.platform == RuntimePlatform.IPhonePlayer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() => AppContext.RegisterScreen(Tab.Home, ctx => new HomeScreen(ctx));

        public HomeScreen(AppContext ctx)
        {
            _ctx = ctx;
            AddToClassList("home");
            _identity = ctx.Store.Load<IdentityForm>(FormKeys.Identity);
            _push = new PushTokenRequest(ctx.Native.GetPushToken);
            _push.Changed += BuildPush;

            _scroll = AppScaffold.NewScreen();
            _scroll.Add(BuildHeader());

            _scroll.Add(new SectionHeader("Status"));
            _enabled = new SwitchRow("SDK enabled", ctx.Api.IsEnabled, on => _ = ctx.Runner.Run(ctx.Identity.SetEnabled(on)),
                help: Help.Get("sdkEnabled"));
            _scroll.Add(_enabled);
            _scroll.Add(BuildProjectRow());
            _notificationsHost = Ui.Box();
            _scroll.Add(_notificationsHost);
            _pushHost = Ui.Box();
            _scroll.Add(_pushHost);

            var openedHeader = new SectionHeader("Opened from", trailing: "", help: Help.Get("openedFrom"));
            _openedTrailing = openedHeader.Q<Label>(className: "text-faint");
            _scroll.Add(openedHeader);
            _openedBody = Ui.Box();
            _scroll.Add(_openedBody);

            _identityHost = Ui.Box();
            _scroll.Add(_identityHost);

            _scroll.Add(new SectionHeader("Quick actions", trailing: "hold to edit", help: Help.Get("quickActions")));
            _generate = new AppButton("Generate link", Generate, onLongPress: ShowGenerateSheet);
            var open = new AppButton("Open clipboard", OpenClipboard, AppButtonStyle.Outline);
            _track = new AppButton("Track", () => Track(false), AppButtonStyle.Outline, onLongPress: ShowTrackSheet);
            _purchase = new AppButton("Purchase", LogPurchase, AppButtonStyle.Outline, onLongPress: ShowPurchaseSheet);
            _scroll.Add(GridRow(_generate, open));
            _scroll.Add(GridRow(_track, _purchase, "home__grid-row--second"));

            BuildNotifications();
            BuildPush();
            BuildIdentity();
            RefreshQuickActions();
            Keyboard.Track(_scroll);
            Add(_scroll);

            RegisterCallback<AttachToPanelEvent>(_ => OnAttach());
            RegisterCallback<DetachFromPanelEvent>(_ => OnDetach());
        }

        void OnAttach()
        {
            _ctx.Scaffold.TabChanged += OnTabChanged;
            _ctx.Log.Changed += RefreshOpenedFrom;
            _ctx.Resumed += OnResumed;
            _openedRefresh = schedule.Execute(RefreshOpenedFrom).Every(OpenedFromRefreshMs);
            RefreshOpenedFrom();
            RequestPushToken();
        }

        void OnDetach()
        {
            if (_ctx.Scaffold != null) _ctx.Scaffold.TabChanged -= OnTabChanged;
            _ctx.Log.Changed -= RefreshOpenedFrom;
            _ctx.Resumed -= OnResumed;
            _openedRefresh?.Pause();
            _openedSettle?.Pause();
        }

        void OnTabChanged(Tab tab)
        {
            if (tab != Tab.Home) return;
            RefreshQuickActions();
            RefreshOpenedFrom();
        }

        void OnResumed()
        {
            BuildNotifications();
            _enabled.SetValueWithoutNotify(_ctx.Api.IsEnabled);
            RefreshOpenedFrom();
        }

        VisualElement BuildHeader()
        {
            var header = Ui.Box("home__header");
            var logo = Ui.Box("home__logo");
            logo.Add(Ui.Text("g", "t-label", "home__logo-text"));
            header.Add(logo);
            header.Add(Ui.Text("Grovs Test", "t-title", "home__title"));
            header.Add(Ui.Text(HomeRules.Caption(_ctx.Config, Grovs.SdkVersion), "t-caption", "home__caption"));
            var gear = new IconButton(TabIcons.Gear, OpenSettings);
            gear.AddToClassList("icon-button--muted");
            gear.tooltip = "Settings";
            header.Add(gear);
            return header;
        }

        void OpenSettings() => _ctx.Scaffold?.OpenSettings();

        static VisualElement GridRow(VisualElement left, VisualElement right, string extraClass = null)
        {
            var row = Ui.Box("home__grid-row");
            if (extraClass != null) row.AddToClassList(extraClass);
            row.Add(left);
            row.Add(right);
            return row;
        }

        // Status

        VisualElement BuildProjectRow()
        {
            var row = new SettingRow("Project", OpenSettings);
            row.Trailing.Add(Ui.Text(HomeRules.Environment(_ctx.Config) + " project", "t-body", "home__muted"));
            row.Trailing.Add(Ui.Text(TextFormat.MiddleEllipsis(_ctx.Config.ApiKey), "t-mono", "text-faint"));
            row.Trailing.Add(new StrokeIcon(TabIcons.ChevronRight));
            return row;
        }

        void BuildNotifications()
        {
            _notificationsHost.Clear();
            var row = new SettingRow("Notifications");
            if (_ctx.Notifications == NotificationStatus.Allowed)
            {
                row.Trailing.Add(new StatusPill("Allowed", LogState.Ok));
            }
            else
            {
                row.Trailing.Add(new StatusPill("Not allowed", LogState.Warning));
                row.Trailing.Add(new TextAction("Allow", AllowNotifications));
            }
            _notificationsHost.Add(row);
        }

        void AllowNotifications()
        {
            var requestedBefore = PlayerPrefs.GetInt(NotificationsRequestedKey, 0) == 1;
            if (HomeRules.AllowAction(_ctx.Notifications, requestedBefore) == NotificationAllowAction.OpenSettings)
            {
                _ctx.Native.OpenAppSettings();
                return;
            }
            PlayerPrefs.SetInt(NotificationsRequestedKey, 1);
            PlayerPrefs.Save();
            _ctx.Native.RequestNotifications(_ =>
            {
                _ctx.RefreshNotifications();
                BuildNotifications();
                RequestPushToken();
            });
        }

        void RequestPushToken()
        {
            if (IsIos) BuildPush();
            else _push.Start();
        }

        void BuildPush()
        {
            _pushHost.Clear();
            var placeholder = HomeRules.PushTokenPlaceholder(IsIos, _push.Requesting);
            _pushHost.Add(new CopyRow("Push token", IsIos ? null : _push.Token, placeholder, Help.Get("pushToken")));
        }

        // Opened from

        void RefreshOpenedFrom()
        {
            _openedBody.Clear();
            _openedSettle?.Pause();
            var latest = _ctx.Log.LatestLink;
            if (latest == null)
            {
                _openedTrailing.text = "";
                _openedBody.Add(Ui.Text("Not opened from a link yet", "t-caption", "home__empty"));
                return;
            }

            var age = DateTime.Now - latest.Time;
            _openedTrailing.text = RelativeTime.Format(age);
            var (text, state) = HomeRules.OpenedFromPill(latest.Sources, age);
            var row = new SettingRow(latest.Link ?? "no link", () => _ctx.Scaffold?.OpenLogs(latest));
            row.Q<Label>(className: "label-with-help__text")?.AddToClassList("home__link");
            row.Trailing.Add(state == LogState.Ok
                ? (VisualElement)new CheckedPill(HomeRules.CheckedSources, state)
                : new StatusPill(text, state));
            _openedBody.Add(row);

            var remaining = HomeRules.LinkSettleWindow - age;
            if (state == LogState.Neutral && remaining > TimeSpan.Zero)
                _openedSettle = schedule.Execute(RefreshOpenedFrom).StartingIn((long)remaining.TotalMilliseconds + 50);
        }

        // Identity

        void BuildIdentity()
        {
            _identityHost.Clear();
            _identityHost.Add(new SectionHeader("Identity", action: _editingIdentity ? null : "Change", onAction: StartEditing,
                help: Help.Get("identity")));
            if (!_editingIdentity)
            {
                var identifier = new SettingRow("Identifier", StartEditing);
                identifier.Trailing.Add(Ui.Text(_identity.Identifier ?? "None", "t-body", "home__muted"));
                _identityHost.Add(identifier);
                var attributes = new SettingRow("Attributes", StartEditing);
                attributes.Trailing.Add(Ui.Text(HomeRules.AttributesSummary(_identity.Attributes), "t-caption", "home__muted"));
                _identityHost.Add(attributes);
                return;
            }
            _identityHost.Add(BuildIdentityEditor());
        }

        void StartEditing()
        {
            _draft = new IdentityForm
            {
                Identifier = _identity.Identifier,
                Attributes = _identity.Attributes.Select(Copy).ToList()
            };
            _editingIdentity = true;
            BuildIdentity();
        }

        void CancelEditing()
        {
            _editingIdentity = false;
            BuildIdentity();
        }

        void SaveIdentity()
        {
            _identity = _draft;
            _ctx.Store.Save(FormKeys.Identity, _identity);
            _ = _ctx.Runner.Run(_ctx.Identity.Save(_identity));
            _editingIdentity = false;
            BuildIdentity();
        }

        static KeyValueRow Copy(KeyValueRow row) => new KeyValueRow { Key = row.Key, Value = row.Value, Kind = row.Kind };

        VisualElement BuildIdentityEditor()
        {
            var root = Ui.Box();
            var field = new SuggestField(new string[0], "Identifier");
            field.SetValueWithoutNotify(_draft.Identifier ?? "");
            root.Add(field);

            var chips = new List<(Chip chip, Func<bool> selected)>();
            void RefreshChips()
            {
                foreach (var (chip, selected) in chips)
                {
                    chip.Selected = selected();
                    if (chip.Text == "None") chip.Dashed = _draft.Identifier != null;
                }
            }
            void Pick(string value)
            {
                _draft.Identifier = value;
                field.SetValueWithoutNotify(value ?? "");
                RefreshChips();
            }
            void AddChip(VisualElement host, string text, Action onClick, Func<bool> selected)
            {
                var chip = new Chip(text, onClick);
                chips.Add((chip, selected));
                host.Add(chip);
            }

            var scroll = new ScrollView(ScrollViewMode.Horizontal)
            {
                horizontalScrollerVisibility = ScrollerVisibility.Hidden,
                verticalScrollerVisibility = ScrollerVisibility.Hidden
            };
            scroll.AddToClassList("chip-row");
            scroll.AddToClassList("home__chips");
            AddChip(scroll.contentContainer, "Device name", () => Pick(DeviceInfo.Name), () => _draft.Identifier == DeviceInfo.Name);
            AddChip(scroll.contentContainer, "qa_user_1", () => Pick("qa_user_1"), () => _draft.Identifier == "qa_user_1");
            AddChip(scroll.contentContainer, "Random id", () => Pick(RandomId.Qa()), () => false);
            AddChip(scroll.contentContainer, "None", () => Pick(null), () => _draft.Identifier == null);
            root.Add(scroll);
            RefreshChips();

            field.Changed += v =>
            {
                _draft.Identifier = string.IsNullOrEmpty(v) ? null : v;
                RefreshChips();
            };

            var attributes = new KeyValueRows(_draft.Attributes, "Attributes", new[] { "plan", "country" });
            attributes.AddToClassList("home__gap");
            root.Add(attributes);

            var buttons = Ui.Box("home__buttons");
            buttons.Add(new AppButton("Cancel", CancelEditing, AppButtonStyle.Outline));
            buttons.Add(new AppButton("Save", SaveIdentity));
            root.Add(buttons);
            return root;
        }

        // Quick actions

        void RefreshQuickActions()
        {
            var ev = _ctx.Store.Load<EventForm>(FormKeys.Event);
            _track.Text = "Track " + ev.Name;
            _track.Enabled = EventFormFields.EventError(ev) == null;

            var purchase = _ctx.Store.Load<PurchaseForm>(FormKeys.Purchase);
            _purchase.Text = purchase.ShortLabel();
            _purchase.Enabled = EventFormFields.PurchaseValid(purchase);
        }

        void Generate()
        {
            if (_ctx.LinksScreen != null)
            {
                _ctx.LinksScreen.RunGenerate();
                return;
            }
            if (_generating) return;
            _generating = true;
            RunGenerateDirect();
        }

        async void RunGenerateDirect()
        {
            var form = _ctx.Store.Load<LinkForm>(FormKeys.Link);
            await _ctx.Runner.Run(_ctx.Links.Generate(form));
            _generating = false;
        }

        void Track(bool anyway)
        {
            if (_ctx.EventsScreen != null)
            {
                _ctx.EventsScreen.RunTrack(anyway);
                RefreshQuickActions();
                return;
            }
            TrackDirect(anyway);
        }

        async void TrackDirect(bool anyway)
        {
            var form = _ctx.Store.Load<EventForm>(FormKeys.Event);
            if (!anyway && EventFormFields.EventError(form) != null) return;
            var entry = await _ctx.Runner.Run(_ctx.Events.Track(form, anyway));
            if (HomeRules.RememberTracked(form, entry.State)) _ctx.Store.Save(FormKeys.Event, form);
        }

        void LogPurchase()
        {
            if (_ctx.EventsScreen != null)
            {
                _ctx.EventsScreen.RunPurchase();
                return;
            }
            var form = _ctx.Store.Load<PurchaseForm>(FormKeys.Purchase);
            if (!EventFormFields.PurchaseValid(form)) return;
            _ = _ctx.Runner.Run(_ctx.Events.Purchase(form));
        }

        void OpenClipboard()
        {
            var text = GUIUtility.systemCopyBuffer?.Trim();
            if (!HomeRules.IsWebUrl(text))
            {
                Toast.Show(Overlay.HostFor(this), "Clipboard has no link");
                return;
            }
            var opened = false;
            try
            {
                opened = _ctx.Api.HandleUrl(text);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Open clipboard link failed: " + e.Message);
            }
            if (!opened) Toast.Show(Overlay.HostFor(this), "This app could not open this link");
        }

        // Sheets: each edits the same saved form as its tab.

        OverlayHandle ShowSheet(string title, VisualElement fields, AppButton action)
        {
            var content = Ui.Box();
            content.Add(Ui.Text(title, "t-heading", "home__sheet-title"));
            content.Add(fields);
            action.AddToClassList("home__sheet-button");
            content.Add(action);
            var handle = Sheet.Show(Overlay.HostFor(this), content);
            handle.Closed += RefreshQuickActions;
            return handle;
        }

        void ShowGenerateSheet()
        {
            var form = _ctx.Store.Load<LinkForm>(FormKeys.Link);
            OverlayHandle sheet = null;
            var save = new AppButton("Save and generate", () =>
            {
                _ctx.Store.Save(FormKeys.Link, form);
                Generate();
                sheet?.Close();
            });
            sheet = ShowSheet("Generate link", LinkFormFields.Build(form, () => _ctx.Store.Save(FormKeys.Link, form)), save);
        }

        void ShowTrackSheet()
        {
            var form = _ctx.Store.Load<EventForm>(FormKeys.Event);
            OverlayHandle sheet = null;
            AppButton save = null;
            Action saveForm = () =>
            {
                _ctx.Store.Save(FormKeys.Event, form);
                if (save != null) save.Enabled = EventFormFields.EventError(form) == null;
            };
            Action trackAndClose = () =>
            {
                if (EventFormFields.EventError(form) != null) return;
                saveForm();
                Track(false);
                sheet?.Close();
            };
            save = new AppButton("Save and track", trackAndClose);
            save.Enabled = EventFormFields.EventError(form) == null;
            var fields = EventFormFields.BuildEvent(form, saveForm, () =>
            {
                saveForm();
                Track(true);
            }, trackAndClose);
            sheet = ShowSheet("Track event", fields, save);
        }

        void ShowPurchaseSheet()
        {
            var form = _ctx.Store.Load<PurchaseForm>(FormKeys.Purchase);
            OverlayHandle sheet = null;
            AppButton save = null;
            Action saveForm = () =>
            {
                _ctx.Store.Save(FormKeys.Purchase, form);
                if (save != null) save.Enabled = EventFormFields.PurchaseValid(form);
            };
            save = new AppButton("Save and log", () =>
            {
                if (!EventFormFields.PurchaseValid(form)) return;
                saveForm();
                LogPurchase();
                sheet?.Close();
            });
            save.Enabled = EventFormFields.PurchaseValid(form);
            var fields = EventFormFields.BuildPurchase(form, saveForm, () =>
            {
                if (string.IsNullOrWhiteSpace(form.StoreReceipt)) return;
                saveForm();
                _ = _ctx.Runner.Run(_ctx.Events.StoreReceipt(form.StoreReceipt));
            });
            sheet = ShowSheet("Purchase", fields, save);
        }
    }
}
