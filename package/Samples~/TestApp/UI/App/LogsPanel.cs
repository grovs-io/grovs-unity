using System.Globalization;
using System.Linq;
using GrovsSDK;
using UnityEngine;
using UnityEngine.UIElements;

namespace GrovsTestApp
{
    /// <summary>The log sheet: a filtered list, and the detail of one entry with its code.</summary>
    public class LogsPanel : VisualElement
    {
        const float HeightFraction = 0.82f;
        const string All = "All";
        const string Errors = "Errors";
        static readonly string[] Filters = { All, "Links", "In-app", "Events", "Push", Errors };

        static LogsPanel _open;
        static string _filter = All;

        readonly AppContext _ctx;
        readonly VisualElement _list;
        readonly VisualElement _chips;
        readonly ScrollView _rows;
        readonly VisualElement _detail;
        OverlayHandle _handle;
        long? _selectedId;

        public static void Show(AppContext ctx, LogEntry focus = null)
        {
            ctx.Log.MarkSeen();
            if (_open != null && _open._handle.IsOpen)
            {
                _open.Select(focus);
                return;
            }
            var panel = new LogsPanel(ctx, focus);
            _open = panel;
            panel._handle = Sheet.Show(panel, HeightFraction);
            panel._handle.Closed += () =>
            {
                // Errors that arrived while the panel was open have been seen.
                ctx.Log.MarkSeen();
                if (_open == panel) _open = null;
            };
        }

        LogsPanel(AppContext ctx, LogEntry focus)
        {
            _ctx = ctx;
            _selectedId = focus?.Id;
            AddToClassList("logs");

            _list = Ui.Box("logs__list");
            var header = Ui.Box("logs__header");
            header.Add(Ui.Text("Logs", "t-heading", "logs__title"));
            header.Add(new TextAction("Share", Share));
            var clear = new TextAction("Clear", () => _ctx.Log.Clear());
            clear.AddToClassList("logs__clear");
            header.Add(clear);
            _list.Add(header);

            var chipScroll = new ScrollView(ScrollViewMode.Horizontal);
            chipScroll.AddToClassList("logs__filters");
            chipScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _chips = chipScroll.contentContainer;
            _list.Add(chipScroll);

            _rows = new ScrollView(ScrollViewMode.Vertical);
            _rows.AddToClassList("logs__rows");
            _list.Add(_rows);

            _detail = Ui.Box("logs__detail");
            Add(_list);
            Add(_detail);

            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                _ctx.Log.Changed += Render;
                Render();
            });
            RegisterCallback<DetachFromPanelEvent>(_ => _ctx.Log.Changed -= Render);
        }

        void Select(LogEntry entry)
        {
            _selectedId = entry?.Id;
            Render();
        }

        void Render()
        {
            var selected = _selectedId.HasValue ? _ctx.Log.Entries.FirstOrDefault(e => e.Id == _selectedId.Value) : null;
            _list.style.display = selected == null ? DisplayStyle.Flex : DisplayStyle.None;
            _detail.style.display = selected == null ? DisplayStyle.None : DisplayStyle.Flex;
            if (selected == null) RenderList();
            else RenderDetail(selected);
        }

        void RenderList()
        {
            var entries = _ctx.Log.Entries;
            var errors = entries.Count(e => e.State == LogState.Error);
            _chips.Clear();
            foreach (var filter in Filters)
            {
                var isErrors = filter == Errors;
                var chip = new Chip(isErrors && errors > 0 ? Errors + " " + errors : filter, null, filter == _filter);
                chip.EnableInClassList("chip--error", isErrors && errors > 0 && filter != _filter);
                var picked = filter;
                chip.Clicked += () =>
                {
                    _filter = picked;
                    Render();
                };
                _chips.Add(chip);
            }

            _rows.Clear();
            var shown = entries.Where(Matches).ToList();
            if (shown.Count == 0) _rows.Add(Ui.Text("Nothing logged yet", "t-caption", "logs__empty"));
            foreach (var entry in shown)
            {
                _rows.Add(Row(entry));
                _rows.Add(Ui.Divider());
            }
        }

        static bool Matches(LogEntry entry)
        {
            if (_filter == All) return true;
            if (_filter == Errors) return entry.State == LogState.Error;
            return LogTypes.Filter(entry.Type) == _filter;
        }

        VisualElement Row(LogEntry entry)
        {
            var row = Ui.Box("log-row");
            row.Add(new StatusDot(entry.State));
            row.Add(Ui.Text(LogTypes.Label(entry.Type), "t-mono-small", "log-row__type"));
            var text = Ui.Box("log-row__text");
            text.Add(Ui.Text(entry.Title, "t-body-strong", "ellipsis"));
            if (!string.IsNullOrWhiteSpace(entry.Detail)) text.Add(Ui.Text(entry.Detail, "t-caption", "ellipsis"));
            row.Add(text);
            var action = entry.Action;
            if (action != null && action.Repeatable)
            {
                var again = new IconButton(TabIcons.Refresh, () => _ctx.Rerun(action));
                again.AddToClassList("icon-button--accent");
                again.tooltip = "Run again";
                row.Add(again);
            }
            Ui.OnTap(row, () => Select(entry));
            return row;
        }

        void RenderDetail(LogEntry entry)
        {
            _detail.Clear();
            var header = Ui.Box("logs__detail-header");
            var back = new IconButton(TabIcons.Back, () => Select(null));
            back.AddToClassList("icon-button--muted");
            header.Add(back);
            header.Add(new StatusDot(entry.State));
            header.Add(Ui.Text(entry.Title, "t-heading", "ellipsis", "logs__detail-title"));
            header.Add(Ui.Text(entry.Time.ToString("HH:mm:ss", CultureInfo.InvariantCulture), "t-caption"));
            _detail.Add(header);

            var copyText = string.IsNullOrWhiteSpace(entry.Code) ? entry.Detail ?? "" : entry.Code;
            var code = new ScrollView(ScrollViewMode.Vertical);
            code.AddToClassList("logs__code");
            code.Add(Ui.Text(copyText, "t-mono", "logs__code-text"));
            _detail.Add(code);

            if (entry.Type == LogType.Link)
            {
                _detail.Add(new SectionHeader("Sources", help: Help.Get("linkSources")));
                _detail.Add(SourceRow("Event", entry, LinkSources.Event));
                _detail.Add(SourceRow("Last link", entry, LinkSources.LastDeeplink));
            }

            var buttons = Ui.Box("button-row");
            buttons.Add(new AppButton("Copy", () =>
            {
                GUIUtility.systemCopyBuffer = copyText;
                Toast.Show(Overlay.HostFor(this), "Copied");
            }, AppButtonStyle.Outline));
            var action = entry.Action;
            if (action != null && action.Repeatable)
                buttons.Add(new AppButton("Run again", () => _ctx.Rerun(action), AppButtonStyle.Outline));
            if (entry.Link != null)
                buttons.Add(new AppButton("Inspect link", () => _ctx.InspectLink(entry.Link), AppButtonStyle.Outline));
            _detail.Add(buttons);
        }

        static SettingRow SourceRow(string label, LogEntry entry, LinkSources source)
        {
            var row = new SettingRow(label);
            var received = (entry.Sources & source) != 0;
            row.Trailing.Add(new StatusPill(received ? "received" : "missed", received ? LogState.Ok : LogState.Error));
            return row;
        }

        void Share()
        {
            var device = "@" + SystemInfo.deviceModel + " · " + SystemInfo.operatingSystem;
            var project = _ctx.Config.UseTestEnvironment ? "Test project" : "Prod project";
            _ctx.Native.Share(ExportLog.Build(_ctx.Log.Entries, device, Grovs.SdkVersion, project));
        }
    }
}
