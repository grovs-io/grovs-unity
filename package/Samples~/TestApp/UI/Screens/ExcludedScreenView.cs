using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace GrovsTestApp
{
    /// <summary>
    /// A plain screen to test in-app message exclusion. With trackScenes on, the SDK sees the scene name
    /// as the current screen.
    /// </summary>
    public class ExcludedScreenView : VisualElement
    {
        public const string SceneName = "ExcludedScreen";
        const string MainScene = "TestApp";

        readonly AppContext _ctx;
        readonly InAppPrefs _prefs;
        readonly VisualElement _latest;

        public ExcludedScreenView(AppContext ctx)
        {
            _ctx = ctx;
            _prefs = ctx.Store.Load<InAppPrefs>(FormKeys.InApp);
            AddToClassList("excluded-view");

            var scroll = AppScaffold.NewScreen();
            scroll.Add(Header());
            scroll.Add(Ui.Text("This is the " + SceneName + " scene. While it is in the excluded list, in-app messages wait "
                + "instead of showing here. Trigger one below, then go back: it should show on the previous screen.",
                "t-body", "excluded-view__intro"));

            scroll.Add(new SectionHeader("Status"));
            scroll.Add(StatusRow());
            AddStatusHelp(scroll);

            scroll.Add(new SectionHeader("Test"));
            var trigger = new AppButton("Trigger " + _prefs.Trigger, () => _ = _ctx.FireTrigger(_prefs));
            trigger.Enabled = !string.IsNullOrWhiteSpace(_prefs.Trigger);
            scroll.Add(trigger);

            _latest = Ui.Box("excluded-view__latest");
            scroll.Add(_latest);
            RefreshLatest();

            var back = new AppButton("Back", Back, AppButtonStyle.Outline);
            back.AddToClassList("excluded-view__back");
            scroll.Add(back);
            Add(scroll);

            RegisterCallback<AttachToPanelEvent>(_ => ctx.Log.Changed += RefreshLatest);
            RegisterCallback<DetachFromPanelEvent>(_ => ctx.Log.Changed -= RefreshLatest);
        }

        /// <summary>Returns to the main scene, which opens on the tab the user left.</summary>
        public void Back() => SceneManager.LoadScene(MainScene);

        VisualElement Header()
        {
            var header = Ui.Box("settings-header");
            var back = new IconButton(TabIcons.Back, Back);
            back.AddToClassList("icon-button--muted");
            header.Add(back);
            header.Add(Ui.Text("Excluded screen", "t-title"));
            return header;
        }

        VisualElement StatusRow()
        {
            var row = new SettingRow("This screen", divider: false);
            row.Trailing.Add(Excluded
                ? new StatusPill("excluded", LogState.Ok)
                : new StatusPill("not excluded", LogState.Warning));
            return row;
        }

        void AddStatusHelp(VisualElement parent)
        {
            if (!Excluded)
                parent.Add(new HelpText("Add " + SceneName + " to Excluded screens on the In-app tab to test exclusion."));
            if (!_ctx.Config.TrackScenes)
                parent.Add(new HelpText("Turn on Track scenes in Grovs settings so this scene is reported as a screen."));
        }

        bool Excluded => _prefs.Excludes(SceneName);

        void RefreshLatest()
        {
            _latest.Clear();
            if (_ctx.Log.Entries.Count == 0) return;
            var latest = _ctx.Log.Entries[0];
            _latest.Add(new StatusDot(latest.State));
            _latest.Add(Ui.Text(latest.Title + " · " + latest.Detail, "t-caption", "excluded-view__text"));
        }
    }
}
