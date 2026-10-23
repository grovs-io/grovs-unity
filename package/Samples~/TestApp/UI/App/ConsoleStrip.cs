using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace GrovsTestApp
{
    /// <summary>The latest log entry above the tab bar. A tap or a drag up opens the logs.</summary>
    public class ConsoleStrip : VisualElement
    {
        const float DragOpenDistance = 8f;

        readonly TestLog _log;
        readonly Action _open;
        readonly StatusDot _dot;
        readonly Label _title;
        readonly Label _time;
        bool _pressed;
        bool _dragOpened;
        Vector2 _pressStart;

        public ConsoleStrip(TestLog log, Action open)
        {
            _log = log;
            _open = open;
            AddToClassList("console-strip");

            _dot = new StatusDot(LogState.Neutral);
            _title = Ui.Text("", "t-caption", "console-strip__title");
            _time = Ui.Text("", "t-caption", "text-faint", "console-strip__time");
            Add(_dot);
            Add(_title);
            Add(_time);
            Add(new StrokeIcon(TabIcons.ChevronUp, 2f) { name = "console-strip-chevron" });

            RegisterCallback<ClickEvent>(_ =>
            {
                if (!_dragOpened) _open();
            });
            RegisterCallback<PointerDownEvent>(evt =>
            {
                _pressed = true;
                _dragOpened = false;
                _pressStart = evt.position;
            });
            RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (!_pressed || _dragOpened || _pressStart.y - evt.position.y < DragOpenDistance) return;
                _dragOpened = true;
                _open();
            });
            RegisterCallback<PointerUpEvent>(_ => _pressed = false);
            RegisterCallback<PointerLeaveEvent>(_ => _pressed = false);
            Ui.TrackPress(this);

            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                _log.Changed += Refresh;
                Refresh();
            });
            RegisterCallback<DetachFromPanelEvent>(_ => _log.Changed -= Refresh);
        }

        void Refresh()
        {
            var latest = _log.Entries.Count > 0 ? _log.Entries[0] : null;
            _dot.State = _log.HasUnseenError ? LogState.Error : latest?.State ?? LogState.Neutral;
            _title.text = latest?.Title ?? "No events yet";
            _time.text = latest == null ? "" : latest.Time.ToString("HH:mm", CultureInfo.InvariantCulture);
            _time.style.display = latest == null ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }
}
