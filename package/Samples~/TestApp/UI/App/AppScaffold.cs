using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace GrovsTestApp
{
    public enum Tab { Home, Links, Events, InApp }

    public static class Tabs
    {
        public static readonly Tab[] All = { Tab.Home, Tab.Links, Tab.Events, Tab.InApp };

        public static string Title(Tab tab)
        {
            switch (tab)
            {
                case Tab.Home: return "Home";
                case Tab.Links: return "Links";
                case Tab.Events: return "Events";
                default: return "In-app";
            }
        }
    }

    /// <summary>
    /// The app frame: one page per tab, the console strip, the tab bar, and Settings pushed over the tabs.
    /// Screens are placeholders until a real one is set with SetScreen or SetSettingsScreen.
    /// </summary>
    public class AppScaffold : VisualElement
    {
        const float TransitionMs = 260f;
        const float FadeInDelayMs = 60f;

        readonly AppContext _ctx;
        readonly VisualElement _main;
        readonly VisualElement _content;
        readonly VisualElement _settings;
        readonly ConsoleStrip _strip;
        readonly VisualElement _tabBar;
        readonly Dictionary<Tab, VisualElement> _pages = new Dictionary<Tab, VisualElement>();
        readonly Dictionary<Tab, TabItem> _items = new Dictionary<Tab, TabItem>();
        Transition _tabTransition;
        Transition _settingsTransition;
        bool _typing;

        public Tab Current { get; private set; }
        public bool SettingsOpen { get; private set; }

        public event Action<Tab> TabChanged;

        public AppScaffold(AppContext ctx)
        {
            _ctx = ctx;
            Current = ctx.SelectedTab;
            AddToClassList("scaffold");

            _main = Ui.Box("scaffold__main");
            _content = Ui.Box("scaffold__content");
            _main.Add(_content);
            foreach (var tab in Tabs.All)
            {
                var page = Ui.Box("scaffold__page");
                page.style.display = tab == Current ? DisplayStyle.Flex : DisplayStyle.None;
                page.Add(Placeholder(Tabs.Title(tab)));
                _pages[tab] = page;
                _content.Add(page);
            }

            _strip = new ConsoleStrip(ctx.Log, () => OpenLogs());
            _main.Add(_strip);

            _tabBar = Ui.Box("tab-bar");
            foreach (var tab in Tabs.All)
            {
                var item = new TabItem(tab, () => ShowTab(tab)) { Selected = tab == Current };
                _items[tab] = item;
                _tabBar.Add(item);
            }
            _main.Add(_tabBar);
            Add(_main);

            _settings = Ui.Box("scaffold__settings");
            _settings.style.display = DisplayStyle.None;
            _settings.Add(SettingsPlaceholder());
            Add(_settings);
        }

        public void SetScreen(Tab tab, VisualElement screen)
        {
            _pages[tab].Clear();
            _pages[tab].Add(screen);
        }

        public void SetSettingsScreen(VisualElement screen)
        {
            _settings.Clear();
            _settings.Add(screen);
        }

        public void ShowTab(Tab tab)
        {
            if (tab == Current) return;
            _tabTransition?.Finish();
            var from = _pages[Current];
            var to = _pages[tab];
            var direction = tab > Current ? 1 : -1;
            _items[Current].Selected = false;
            _items[tab].Selected = true;
            Current = tab;
            _ctx.SelectedTab = tab;

            var shift = Width(_content) / 12f;
            to.style.display = DisplayStyle.Flex;
            to.BringToFront();
            _tabTransition = Transition.Start(this, TransitionMs + FadeInDelayMs, ms =>
            {
                var slide = Transition.Ease(ms / TransitionMs);
                to.style.translate = new Translate(direction * shift * (1 - slide), 0);
                to.style.opacity = Transition.Ease((ms - FadeInDelayMs) / TransitionMs);
                from.style.translate = new Translate(-direction * shift * slide, 0);
                from.style.opacity = 1 - Transition.Ease(ms / (TransitionMs / 2));
            }, () =>
            {
                from.style.display = DisplayStyle.None;
                from.style.translate = StyleKeyword.Null;
                from.style.opacity = StyleKeyword.Null;
                to.style.translate = StyleKeyword.Null;
                to.style.opacity = StyleKeyword.Null;
            });
            TabChanged?.Invoke(tab);
        }

        public void OpenSettings()
        {
            if (SettingsOpen) return;
            SettingsOpen = true;
            _settingsTransition?.Finish();
            var width = Width(this);
            _settings.style.display = DisplayStyle.Flex;
            _settings.BringToFront();
            _settingsTransition = Transition.Start(this, TransitionMs, ms =>
            {
                var t = Transition.Ease(ms / TransitionMs);
                _settings.style.translate = new Translate(width * (1 - t), 0);
                _main.style.translate = new Translate(-width / 4 * t, 0);
                _main.style.opacity = 1 - t;
            }, () => _main.style.display = DisplayStyle.None);
        }

        public void CloseSettings()
        {
            if (!SettingsOpen) return;
            SettingsOpen = false;
            _settingsTransition?.Finish();
            var width = Width(this);
            _main.style.display = DisplayStyle.Flex;
            _settingsTransition = Transition.Start(this, TransitionMs, ms =>
            {
                var t = Transition.Ease(ms / TransitionMs);
                _settings.style.translate = new Translate(width * t, 0);
                _main.style.translate = new Translate(-width / 4 * (1 - t), 0);
                _main.style.opacity = t;
            }, () =>
            {
                _settings.style.display = DisplayStyle.None;
                _main.style.translate = StyleKeyword.Null;
                _main.style.opacity = StyleKeyword.Null;
            });
        }

        public void OpenLogs(LogEntry focus = null) => LogsPanel.Show(_ctx, focus);

        /// <summary>The keyboard covers the strip and the tab bar, so they step aside while typing.</summary>
        public void SetTyping(bool typing)
        {
            if (typing == _typing) return;
            _typing = typing;
            var display = typing ? DisplayStyle.None : DisplayStyle.Flex;
            _strip.style.display = display;
            _tabBar.style.display = display;
        }

        /// <summary>The Android back button when no sheet or dialog is open. Returns false when there is nothing to close.</summary>
        public bool HandleBack()
        {
            if (!SettingsOpen) return false;
            CloseSettings();
            return true;
        }

        static float Width(VisualElement element) => float.IsNaN(element.layout.width) ? 0 : element.layout.width;

        /// <summary>A vertical scroll with the screen padding, the base of every screen.</summary>
        public static ScrollView NewScreen()
        {
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("screen");
            scroll.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            scroll.contentContainer.AddToClassList("screen__content");
            return scroll;
        }

        static VisualElement Placeholder(string title)
        {
            var scroll = NewScreen();
            scroll.Add(new ScreenTitle(title));
            return scroll;
        }

        VisualElement SettingsPlaceholder()
        {
            var scroll = NewScreen();
            scroll.Add(SettingsHeader(CloseSettings));
            return scroll;
        }

        /// <summary>The Settings title with its back arrow.</summary>
        public static VisualElement SettingsHeader(Action onBack)
        {
            var header = Ui.Box("settings-header");
            var back = new IconButton(TabIcons.Back, onBack);
            back.AddToClassList("icon-button--muted");
            header.Add(back);
            header.Add(Ui.Text("Settings", "t-title"));
            return header;
        }

        class TabItem : VisualElement
        {
            public TabItem(Tab tab, Action onClick)
            {
                AddToClassList("tab-item");
                var indicator = Ui.Box("tab-item__indicator");
                indicator.Add(new StrokeIcon(TabIcons.For(tab)));
                Add(indicator);
                Add(Ui.Text(Tabs.Title(tab), "t-label", "tab-item__label"));
                Ui.OnTap(this, onClick);
            }

            public bool Selected
            {
                set => EnableInClassList("tab-item--selected", value);
            }
        }
    }

    /// <summary>A frame-driven animation that can be jumped to its end when a new one starts.</summary>
    sealed class Transition
    {
        readonly Action<float> _step;
        readonly Action _done;
        readonly float _durationMs;
        readonly float _startedAt;
        IVisualElementScheduledItem _item;
        bool _finished;

        Transition(Action<float> step, Action done, float durationMs)
        {
            _step = step;
            _done = done;
            _durationMs = durationMs;
            _startedAt = Time.realtimeSinceStartup;
        }

        /// <summary>Calls step with the elapsed milliseconds every frame, then done once.</summary>
        public static Transition Start(VisualElement owner, float durationMs, Action<float> step, Action done)
        {
            var transition = new Transition(step, done, durationMs);
            step(0);
            transition._item = owner.schedule.Execute(transition.Tick).Every(0);
            return transition;
        }

        void Tick()
        {
            var elapsed = (Time.realtimeSinceStartup - _startedAt) * 1000f;
            if (elapsed >= _durationMs) Finish();
            else _step(elapsed);
        }

        public void Finish()
        {
            if (_finished) return;
            _finished = true;
            _item?.Pause();
            _step(_durationMs);
            _done?.Invoke();
        }

        /// <summary>Material's fast out, slow in curve: cubic-bezier(0.4, 0, 0.2, 1).</summary>
        public static float Ease(float t)
        {
            t = Mathf.Clamp01(t);
            // Solve x(s) = t for the curve parameter s with Newton steps, then return y(s).
            var s = t;
            for (var i = 0; i < 6; i++)
            {
                var x = Bezier(s, 0.4f, 0.2f) - t;
                var dx = BezierSlope(s, 0.4f, 0.2f);
                if (Mathf.Abs(dx) < 1e-5f) break;
                s = Mathf.Clamp01(s - x / dx);
            }
            return Bezier(s, 0f, 1f);
        }

        static float Bezier(float s, float p1, float p2)
        {
            var u = 1 - s;
            return 3 * u * u * s * p1 + 3 * u * s * s * p2 + s * s * s;
        }

        static float BezierSlope(float s, float p1, float p2)
        {
            var u = 1 - s;
            return 3 * u * u * p1 + 6 * u * s * (p2 - p1) + 3 * s * s * (1 - p2);
        }
    }
}
