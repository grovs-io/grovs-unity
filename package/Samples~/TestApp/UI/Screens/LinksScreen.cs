using System;
using GrovsSDK;
using UnityEngine;
using UnityEngine.UIElements;

namespace GrovsTestApp
{
    /// <summary>The Links tab: inspect a link, make a new one, and try the result.</summary>
    public class LinksScreen : VisualElement
    {
        readonly AppContext _ctx;
        readonly ScrollView _scroll;
        readonly LinkField _inspect;
        readonly VisualElement _result;
        readonly VisualElement _formHost;
        readonly AppButton _generate;
        LinkForm _form;
        TestAction _lastAction;
        bool _running;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() => AppContext.RegisterScreen(Tab.Links, ctx => new LinksScreen(ctx));

        public LinksScreen(AppContext ctx)
        {
            _ctx = ctx;
            AddToClassList("links");
            _form = ctx.Store.Load<LinkForm>(FormKeys.Link);

            _scroll = AppScaffold.NewScreen();
            _scroll.Add(new ScreenTitle("Links", "Reset", Reset));

            _inspect = new LinkField("Inspect a link", "Paste a link to inspect", true, Help.Get("inspect"));
            _inspect.Done += () => Inspect(_inspect.Value);
            _scroll.Add(_inspect);

            _result = Ui.Box();
            _scroll.Add(_result);

            _scroll.Add(new SectionHeader("New link"));
            _formHost = Ui.Box();
            _scroll.Add(_formHost);
            BuildForm();
            Keyboard.Track(_scroll);
            Add(_scroll);

            var bar = Ui.Box("links__bar");
            _generate = new AppButton("Generate link", RunGenerate);
            bar.Add(_generate);
            Add(bar);

            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                ctx.LinksScreen = this;
                ctx.Scaffold.TabChanged += OnTabChanged;
            });
            RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                if (ctx.LinksScreen == this) ctx.LinksScreen = null;
                if (ctx.Scaffold != null) ctx.Scaffold.TabChanged -= OnTabChanged;
            });
        }

        public async void RunGenerate()
        {
            if (_running) return;
            await Run(_ctx.Links.Generate(_form));
        }

        /// <summary>Loads what Grovs stores for the link and shows it in the logs.</summary>
        public async void Inspect(string link)
        {
            var path = LinkPath.Extract(link);
            if (path == null) return;
            _inspect.SetValueWithoutNotify(link);
            var entry = await _ctx.Runner.Run(_ctx.Links.Inspect(path));
            _ctx.Scaffold?.OpenLogs(entry);
        }

        void Reset()
        {
            _form = _ctx.Store.Reset<LinkForm>(FormKeys.Link);
            Save();
            BuildForm();
        }

        void Save() => _ctx.Store.Save(FormKeys.Link, _form);

        void BuildForm()
        {
            _formHost.Clear();
            _formHost.Add(LinkFormFields.Build(_form, Save));
        }

        // The Home sheet edits the same saved form.
        void OnTabChanged(Tab tab)
        {
            if (tab != Tab.Links) return;
            var saved = _ctx.Store.Load<LinkForm>(FormKeys.Link);
            if (JsonUtility.ToJson(saved) == JsonUtility.ToJson(_form)) return;
            _form = saved;
            BuildForm();
        }

        async System.Threading.Tasks.Task Run(TestAction action)
        {
            _lastAction = action;
            SetRunning(true);
            var entry = await _ctx.Runner.Run(action);
            SetRunning(false);
            ShowResult(entry);
        }

        void SetRunning(bool running)
        {
            _running = running;
            _generate.Text = running ? "Generating…" : "Generate link";
            _generate.Enabled = !running;
        }

        void RunAgain()
        {
            if (_running || _lastAction == null) return;
            _ = Run(_lastAction);
        }

        void ShowResult(LogEntry entry)
        {
            _result.Clear();
            var ok = entry.State != LogState.Error && entry.Link != null;
            var card = Ui.Box("result-card");
            Ui.SetStateClass(card, ok ? LogState.Ok : LogState.Error);

            var header = Ui.Box("result-card__header");
            header.Add(new StatusDot(ok ? LogState.Ok : LogState.Error));
            var status = ok
                ? "Created · " + entry.ElapsedMs + " ms · " + Calls.StyleLabel(_ctx.Developer.CallStyle)
                : "Failed";
            header.Add(Ui.Text(status, "t-caption", "result-card__status"));
            var again = new IconButton(TabIcons.Refresh, RunAgain);
            again.AddToClassList("icon-button--accent");
            again.tooltip = "Run again";
            header.Add(again);
            card.Add(header);

            if (ok)
            {
                card.Add(Ui.Text(entry.Link, "t-mono", "result-card__link"));
                card.Add(Actions(entry.Link));
            }
            else
            {
                card.Add(Ui.Text(entry.Detail, "t-caption", "result-card__error"));
            }
            _result.Add(card);
        }

        VisualElement Actions(string link)
        {
            var row = Ui.Box("result-card__actions");
            row.Add(new TextAction("Copy", () => Copy(link)));
            row.Add(new TextAction("Share", () => _ctx.Native.Share(link)));
            row.Add(new TextAction("QR", () => QrSheet.Show(link)));
            row.Add(new TextAction("Open", () => Open(link)));
            var command = Application.platform == RuntimePlatform.IPhonePlayer ? "simctl" : "adb";
            row.Add(new TextAction(command, () => Copy(_ctx.Native.ShareCommand(link))));
            row.Add(new TextAction("Inspect", () => Inspect(link)));
            return row;
        }

        void Copy(string text)
        {
            GUIUtility.systemCopyBuffer = text;
            Toast.Show(Overlay.HostFor(this), "Copied");
        }

        void Open(string link)
        {
            var opened = false;
            try
            {
                opened = _ctx.Api.HandleUrl(link);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Open link failed: " + e.Message);
            }
            if (!opened) Toast.Show(Overlay.HostFor(this), "This app could not open this link");
        }
    }
}
