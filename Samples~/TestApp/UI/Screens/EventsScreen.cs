using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace GrovsTestApp
{
    /// <summary>The Events tab: send events, screen views and purchases, and set the global tags and aliases.</summary>
    public class EventsScreen : VisualElement
    {
        static readonly string[] DefaultScreenNames = { "Home", "Paywall", "Settings", "Checkout" };

        readonly AppContext _ctx;
        readonly ScrollView _scroll;
        readonly VisualElement _eventHost;
        readonly VisualElement _screenHost;
        readonly VisualElement _setupHost;
        readonly VisualElement _purchaseHost;
        EventForm _event;
        ScreenForm _screen;
        PurchaseForm _purchase;
        SetupForm _setup;

        AppButton _track;
        AppButton _trackScreen;
        AppButton _purchaseButton;
        ChipRow _screenChips;
        AppTextField _screenName;
        VisualElement _eventFields;
        bool _purchaseExpanded;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() => AppContext.RegisterScreen(Tab.Events, ctx => new EventsScreen(ctx));

        public EventsScreen(AppContext ctx)
        {
            _ctx = ctx;
            AddToClassList("events");
            LoadForms();

            _scroll = AppScaffold.NewScreen();
            _scroll.Add(new ScreenTitle("Events", "Reset", Reset));

            _scroll.Add(new SectionHeader("Custom event", help: Help.Get("customEvent")));
            _eventHost = Ui.Box();
            _scroll.Add(_eventHost);

            _scroll.Add(new SectionHeader("Screen view", help: Help.Get("screenView")));
            _screenHost = Ui.Box();
            _scroll.Add(_screenHost);

            _scroll.Add(new SectionHeader("Setup"));
            _setupHost = Ui.Box();
            _scroll.Add(_setupHost);

            _scroll.Add(new SectionHeader("Purchase", help: Help.Get("purchase")));
            _purchaseHost = Ui.Box();
            _scroll.Add(_purchaseHost);

            BuildAll();
            Keyboard.Track(_scroll);
            Add(_scroll);

            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                ctx.EventsScreen = this;
                ctx.Scaffold.TabChanged += OnTabChanged;
            });
            RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                if (ctx.EventsScreen == this) ctx.EventsScreen = null;
                if (ctx.Scaffold != null) ctx.Scaffold.TabChanged -= OnTabChanged;
            });
        }

        public async void RunTrack(bool anyway = false)
        {
            SyncFromStore();
            if (!anyway && EventFormFields.EventError(_event) != null) return;
            _ctx.Store.Save(FormKeys.Event, _event);
            var entry = await _ctx.Runner.Run(_ctx.Events.Track(_event, anyway));
            if (entry.State == LogState.Error) return;
            Recents.Remember(_event.Recent, _event.Name.Trim());
            _ctx.Store.Save(FormKeys.Event, _event);
            EventFormFields.SetEventSuggestions(_eventFields, _event);
        }

        public async void RunScreen()
        {
            SyncFromStore();
            if (string.IsNullOrWhiteSpace(_screen.Name)) return;
            _ctx.Store.Save(FormKeys.Screen, _screen);
            var entry = await _ctx.Runner.Run(_ctx.Events.Screen(_screen));
            if (entry.State == LogState.Error) return;
            Recents.Remember(_screen.Recent, _screen.Name.Trim());
            _ctx.Store.Save(FormKeys.Screen, _screen);
            _screenChips.SetValues(ScreenSuggestions());
            _screenChips.Selected = _screen.Name;
        }

        public void RunPurchase()
        {
            SyncFromStore();
            if (!PurchaseValid()) return;
            _ctx.Store.Save(FormKeys.Purchase, _purchase);
            _ = _ctx.Runner.Run(_ctx.Events.Purchase(_purchase));
        }

        void RunReceipt()
        {
            SyncFromStore();
            if (string.IsNullOrWhiteSpace(_purchase.StoreReceipt)) return;
            _ctx.Store.Save(FormKeys.Purchase, _purchase);
            _ = _ctx.Runner.Run(_ctx.Events.StoreReceipt(_purchase.StoreReceipt));
        }

        void LoadForms()
        {
            _event = _ctx.Store.Load<EventForm>(FormKeys.Event);
            _screen = _ctx.Store.Load<ScreenForm>(FormKeys.Screen);
            _purchase = _ctx.Store.Load<PurchaseForm>(FormKeys.Purchase);
            _setup = _ctx.Store.Load<SetupForm>(FormKeys.Setup);
        }

        void Reset()
        {
            _event = _ctx.Store.Reset<EventForm>(FormKeys.Event);
            _screen = _ctx.Store.Reset<ScreenForm>(FormKeys.Screen);
            _purchase = _ctx.Store.Reset<PurchaseForm>(FormKeys.Purchase);
            BuildAll();
        }

        // The Home sheets edit the same saved forms.
        void OnTabChanged(Tab tab)
        {
            if (tab == Tab.Events) SyncFromStore();
        }

        void SyncFromStore()
        {
            var changed = _ctx.Store.Refresh(FormKeys.Event, ref _event);
            changed |= _ctx.Store.Refresh(FormKeys.Screen, ref _screen);
            changed |= _ctx.Store.Refresh(FormKeys.Purchase, ref _purchase);
            if (changed) BuildAll();
        }

        void BuildAll()
        {
            BuildEvent();
            BuildScreen();
            BuildSetup();
            BuildPurchase();
        }

        void BuildEvent()
        {
            _eventHost.Clear();
            _eventFields = EventFormFields.BuildEvent(_event, () =>
            {
                _ctx.Store.Save(FormKeys.Event, _event);
                RefreshTrack();
            }, () => RunTrack(true), () => RunTrack());
            _eventHost.Add(_eventFields);

            _track = new AppButton("Track", () => RunTrack());
            _track.AddToClassList("events__full");
            _eventHost.Add(_track);
            RefreshTrack();
        }

        void RefreshTrack()
        {
            var error = EventFormFields.EventError(_event);
            _track.Text = error ?? "Track";
            _track.Enabled = error == null;
        }

        IList<string> ScreenSuggestions() => _screen.Recent.Concat(DefaultScreenNames).Distinct().ToList();

        void BuildScreen()
        {
            _screenHost.Clear();
            var row = Ui.Box("events__row");
            _screenName = new AppTextField();
            _screenName.AddToClassList("events__grow");
            _screenName.SetValueWithoutNotify(_screen.Name);
            _screenName.Changed += v =>
            {
                _screen.Name = v;
                _ctx.Store.Save(FormKeys.Screen, _screen);
                _screenChips.Selected = v;
                _trackScreen.Enabled = !string.IsNullOrWhiteSpace(v);
            };
            _screenName.Submitted += RunScreen;
            row.Add(_screenName);

            _trackScreen = new AppButton("Track", RunScreen, small: true, onLongPress: ShowScreenProperties);
            _trackScreen.AddToClassList("events__small");
            _trackScreen.Enabled = !string.IsNullOrWhiteSpace(_screen.Name);
            row.Add(_trackScreen);
            _screenHost.Add(row);

            _screenChips = new ChipRow(ScreenSuggestions(), _screen.Name, v =>
            {
                _screenName.Value = v;
            });
            _screenChips.AddToClassList("events__gap");
            _screenHost.Add(_screenChips);
        }

        void ShowScreenProperties()
        {
            var content = Ui.Box();
            content.Add(Ui.Text("Screen properties", "t-heading", "events__sheet-title"));
            var rows = new KeyValueRows(_screen.Properties);
            rows.Changed += () => _ctx.Store.Save(FormKeys.Screen, _screen);
            content.Add(rows);

            OverlayHandle sheet = null;
            var save = new AppButton("Save and track", () =>
            {
                RunScreen();
                sheet?.Close();
            });
            save.AddToClassList("events__gap");
            content.Add(save);
            sheet = Sheet.Show(Overlay.HostFor(this), content);
        }

        void BuildSetup()
        {
            _setupHost.Clear();

            var tags = new ChipInput(_setup.GlobalTags, placeholder: "Add tag");
            FoldRow tagsFold = null;
            tags.Changed += () =>
            {
                _ctx.Store.Save(FormKeys.Setup, _setup);
                tagsFold.RefreshSummary();
                _ = _ctx.Runner.Run(_ctx.Events.GlobalTags(_setup.GlobalTags));
            };
            tagsFold = new FoldRow("Global tags", () => _setup.GlobalTags.Count == 0 ? "none" : string.Join(", ", _setup.GlobalTags),
                tags, help: Help.Get("globalTags"));
            _setupHost.Add(tagsFold);

            var draft = _setup.Aliases.Select(Copy).ToList();
            var aliases = Ui.Box();
            aliases.Add(new KeyValueRows(draft, typed: false));
            FoldRow aliasesFold = null;
            var apply = new AppButton("Apply aliases", () =>
            {
                _setup.Aliases = draft.Select(Copy).ToList();
                _ctx.Store.Save(FormKeys.Setup, _setup);
                aliasesFold.RefreshSummary();
                _ = _ctx.Runner.Run(_ctx.Events.Aliases(_setup.Aliases));
            }, AppButtonStyle.Outline);
            apply.AddToClassList("events__gap");
            aliases.Add(apply);
            aliasesFold = new FoldRow("Screen aliases",
                () => _setup.Aliases.Count == 0 ? "none" : string.Join(", ", _setup.Aliases.Select(a => a.Key + " → " + a.Value)),
                aliases, help: Help.Get("screenAliases"));
            _setupHost.Add(aliasesFold);
        }

        static KeyValueRow Copy(KeyValueRow row) => new KeyValueRow { Key = row.Key, Value = row.Value, Kind = row.Kind };

        bool PurchaseValid() => EventFormFields.PurchaseValid(_purchase);

        void BuildPurchase()
        {
            _purchaseHost.Clear();
            var row = Ui.Box("events__row");
            _purchaseButton = new AppButton(_purchase.Label(), RunPurchase);
            _purchaseButton.AddToClassList("events__grow");
            row.Add(_purchaseButton);

            var fields = Ui.Box("events__gap");
            var chevron = new IconButton(TabIcons.ChevronDown, () => SetPurchaseExpanded(fields, row.Q<IconButton>(), !_purchaseExpanded));
            chevron.tooltip = "Edit purchase";
            chevron.name = "edit-purchase";
            row.Add(chevron);
            _purchaseHost.Add(row);

            fields.Add(EventFormFields.BuildPurchase(_purchase, () =>
            {
                _ctx.Store.Save(FormKeys.Purchase, _purchase);
                RefreshPurchase();
            }, RunReceipt));
            _purchaseHost.Add(fields);
            SetPurchaseExpanded(fields, chevron, _purchaseExpanded);
            RefreshPurchase();
        }

        void SetPurchaseExpanded(VisualElement fields, IconButton chevron, bool expanded)
        {
            _purchaseExpanded = expanded;
            fields.style.display = expanded ? DisplayStyle.Flex : DisplayStyle.None;
            chevron.style.rotate = new StyleRotate(new Rotate(expanded ? 180 : 0));
            RefreshPurchase();
        }

        void RefreshPurchase()
        {
            _purchaseButton.Text = _purchase.Label();
            _purchaseButton.Enabled = PurchaseValid();
            _purchaseButton.EnableInClassList("app-button--outline", !_purchaseExpanded);
            _purchaseButton.EnableInClassList("app-button--primary", _purchaseExpanded);
        }
    }
}
