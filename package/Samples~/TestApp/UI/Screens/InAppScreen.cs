using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace GrovsTestApp
{
    /// <summary>The In-app tab: pause and resume messages, trigger one, manage the queue and the rules.</summary>
    public class InAppScreen : VisualElement
    {
        static readonly string[] DefaultTriggers = { "demo_tapped", "onboarding_done", "promo_seen" };

        readonly AppContext _ctx;
        readonly ScrollView _scroll;
        readonly VisualElement _body;
        InAppPrefs _prefs;

        AppButton _trigger;
        AppTextField _triggerField;
        ChipRow _chips;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() => AppContext.RegisterScreen(Tab.InApp, ctx => new InAppScreen(ctx));

        public InAppScreen(AppContext ctx)
        {
            _ctx = ctx;
            AddToClassList("in-app");
            _prefs = ctx.Store.Load<InAppPrefs>(FormKeys.InApp);

            _scroll = AppScaffold.NewScreen();
            _body = Ui.Box();
            _scroll.Add(_body);
            Build();
            Keyboard.Track(_scroll);
            Add(_scroll);

            RegisterCallback<AttachToPanelEvent>(_ => ctx.Scaffold.TabChanged += OnTabChanged);
            RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                if (ctx.Scaffold != null) ctx.Scaffold.TabChanged -= OnTabChanged;
            });
        }

        void OnTabChanged(Tab tab)
        {
            if (tab != Tab.InApp || !_ctx.Store.Refresh(FormKeys.InApp, ref _prefs)) return;
            Build();
        }

        void Build()
        {
            _body.Clear();
            _body.Add(new ScreenTitle("In-app"));

            var running = new Segment<bool>(new[] { false, true }, _ctx.InAppRunning, SetRunning,
                label: on => on ? "Running" : "Paused");
            running.name = "running";
            _body.Add(running);

            BuildTrigger();
            BuildQueue(running);
            BuildRules();
        }

        void SetRunning(bool on)
        {
            _ctx.InAppRunning = on;
            _ = _ctx.Runner.Run(on ? _ctx.InApp.Resume() : _ctx.InApp.Pause());
        }

        void BuildTrigger()
        {
            _body.Add(new SectionHeader("Show a message", help: Help.Get("showMessage")));
            var row = Ui.Box("in-app__row");
            _triggerField = new AppTextField(mono: true);
            _triggerField.AddToClassList("in-app__grow");
            _triggerField.SetValueWithoutNotify(_prefs.Trigger);
            _triggerField.Changed += v =>
            {
                _prefs.Trigger = v;
                _ctx.Store.Save(FormKeys.InApp, _prefs);
                _chips.Selected = v;
                _trigger.Enabled = !string.IsNullOrWhiteSpace(v);
            };
            _triggerField.Submitted += RunTrigger;
            row.Add(_triggerField);

            _trigger = new AppButton("Trigger", RunTrigger, small: true);
            _trigger.AddToClassList("in-app__small");
            _trigger.Enabled = !string.IsNullOrWhiteSpace(_prefs.Trigger);
            row.Add(_trigger);
            _body.Add(row);

            _body.Add(new HelpText("Fires this event. Set a campaign with this trigger in the dashboard."));
            _chips = new ChipRow(TriggerSuggestions(), _prefs.Trigger, v => _triggerField.Value = v);
            _chips.AddToClassList("in-app__gap");
            _body.Add(_chips);
        }

        IList<string> TriggerSuggestions() => _prefs.RecentTriggers.Concat(DefaultTriggers).Distinct().ToList();

        async void RunTrigger()
        {
            if (string.IsNullOrWhiteSpace(_prefs.Trigger)) return;
            _ctx.Store.Save(FormKeys.InApp, _prefs);
            await _ctx.FireTrigger(_prefs);
            _chips.SetValues(TriggerSuggestions());
            _chips.Selected = _prefs.Trigger;
        }

        void BuildQueue(Segment<bool> running)
        {
            _body.Add(new SectionHeader("Queue", help: Help.Get("queue")));
            var row = Ui.Box("in-app__row");
            row.Add(QueueButton("Discard waiting", _ctx.InApp.DiscardWaiting, running));
            var close = QueueButton("Close current", _ctx.InApp.CloseCurrent, running);
            close.AddToClassList("in-app__second");
            row.Add(close);
            _body.Add(row);
        }

        // Discarding also leaves the SDK paused.
        AppButton QueueButton(string text, System.Func<TestAction> action, Segment<bool> running)
        {
            var button = new AppButton(text, () =>
            {
                _ctx.InAppRunning = false;
                running.SetValueWithoutNotify(false);
                _ = _ctx.Runner.Run(action());
            }, AppButtonStyle.Outline, small: true);
            button.AddToClassList("in-app__grow");
            return button;
        }

        void BuildRules()
        {
            _body.Add(new SectionHeader("Rules"));

            _body.Add(new SwitchRow("Block all messages", _ctx.BlockAll, on =>
            {
                _ctx.BlockAll = on;
                _ctx.Log.Add(LogType.InApp, LogState.Neutral, on ? "Blocking all messages" : "Messages allowed",
                    "shouldShowInAppMessage returns " + (on ? "false" : "true"));
            }, help: Help.Get("blockAll")));

            _body.Add(new SwitchRow("Debug logging", _prefs.DebugLogging, on =>
            {
                _prefs.DebugLogging = on;
                _ctx.Store.Save(FormKeys.InApp, _prefs);
                _ = _ctx.Runner.Run(_ctx.InApp.SetDebug(on));
            }, help: Help.Get("inAppDebug")));

            var excluded = new ChipInput(_prefs.Excluded, "Excluded screens", "Add screen", help: Help.Get("excluded"));
            excluded.AddToClassList("in-app__gap");
            excluded.Changed += () =>
            {
                _ctx.Store.Save(FormKeys.InApp, _prefs);
                _ = _ctx.Runner.Run(_ctx.InApp.SetExcluded(_prefs.Excluded));
            };
            _body.Add(excluded);

            var open = new SettingRow("Open an excluded screen", () => SceneManager.LoadScene(ExcludedScreenView.SceneName),
                divider: false);
            var chevron = new StrokeIcon(TabIcons.ChevronRight);
            chevron.AddToClassList("in-app__chevron");
            open.Trailing.Add(chevron);
            _body.Add(open);
        }
    }
}
