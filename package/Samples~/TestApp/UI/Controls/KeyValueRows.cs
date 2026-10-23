using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace GrovsTestApp
{
    /// <summary>
    /// Edits a list of key/value rows in place. A row's type follows what is typed until its badge is
    /// tapped, after which the tapped type sticks.
    /// </summary>
    public class KeyValueRows : VisualElement
    {
        public const string BadgeClass = "kv-badge";

        readonly List<KeyValueRow> _rows;
        readonly IList<string> _suggestedKeys;
        readonly bool _typed;
        readonly HashSet<KeyValueRow> _pinned = new HashSet<KeyValueRow>();
        readonly VisualElement _list;
        readonly VisualElement _chips;

        public event Action Changed;

        public KeyValueRows(List<KeyValueRow> rows, string label = null, IList<string> suggestedKeys = null, bool typed = true,
            (string title, string body)? help = null)
        {
            _rows = rows;
            _suggestedKeys = suggestedKeys ?? new string[0];
            _typed = typed;
            AddToClassList("kv-rows");

            if (label != null)
            {
                Add(typed
                    ? new FieldLabel(label, action: "Paste JSON", onAction: ShowPasteSheet, help: help)
                    : new FieldLabel(label, help: help));
            }
            _list = Ui.Box("kv-rows__list");
            Add(_list);

            var scroll = new ScrollView(ScrollViewMode.Horizontal)
            {
                horizontalScrollerVisibility = ScrollerVisibility.Hidden,
                verticalScrollerVisibility = ScrollerVisibility.Hidden
            };
            scroll.AddToClassList("chip-row");
            _chips = scroll.contentContainer;
            Add(scroll);
            Refresh();
        }

        /// <summary>Redraws after the bound list was changed from outside.</summary>
        public void Refresh()
        {
            _pinned.Clear();
            foreach (var row in _rows)
                if (row.Kind != KeyValue.Detect(row.Value)) _pinned.Add(row);

            _list.Clear();
            for (var i = 0; i < _rows.Count; i++) _list.Add(BuildRow(_rows[i], i));
            RefreshChips();
        }

        VisualElement BuildRow(KeyValueRow row, int index)
        {
            var line = Ui.Box("kv-row");

            var key = new AppTextField("key");
            key.AddToClassList("kv-row__key");
            key.Input.name = "kvKey" + index;
            key.SetValueWithoutNotify(row.Key);
            key.Changed += v =>
            {
                row.Key = v;
                RefreshChips();
                Changed?.Invoke();
            };
            line.Add(key);

            var value = new AppTextField("value");
            value.AddToClassList("kv-row__value");
            value.Input.name = "kvValue" + index;
            value.SetValueWithoutNotify(row.Value);
            line.Add(value);

            Label badge = null;
            if (_typed)
            {
                badge = Ui.Text(Badge(row.Kind), "t-mono-small", BadgeClass);
                Ui.OnTap(badge, () =>
                {
                    _pinned.Add(row);
                    row.Kind = Next(row.Kind);
                    badge.text = Badge(row.Kind);
                    Changed?.Invoke();
                });
                value.Trailing.Add(badge);
            }
            value.Changed += v =>
            {
                row.Value = v;
                if (!_pinned.Contains(row)) row.Kind = KeyValue.Detect(v);
                if (badge != null) badge.text = Badge(row.Kind);
                Changed?.Invoke();
            };

            line.Add(new TextAction("×", () =>
            {
                _rows.Remove(row);
                Refresh();
                Changed?.Invoke();
            }, faint: true));
            return line;
        }

        void RefreshChips()
        {
            _chips.Clear();
            _chips.Add(new Chip("+ Add", () => AddRow(""), dashed: true));
            foreach (var key in _suggestedKeys.Where(k => _rows.All(r => r.Key != k)))
                _chips.Add(new Chip("+ " + key, () => AddRow(key)));
        }

        void AddRow(string key)
        {
            _rows.Add(new KeyValueRow { Key = key, Value = "", Kind = ValueKind.Text });
            Refresh();
            Changed?.Invoke();
        }

        void ShowPasteSheet()
        {
            var content = Ui.Box("json-sheet");
            content.Add(Ui.Text("Paste JSON", "t-heading", "json-sheet__title"));
            var field = new AppTextField("", true, multiline: true);
            field.AddToClassList("json-sheet__field");
            field.SetValueWithoutNotify(_rows.Count == 0 ? "{}" : KeyValue.ToJson(_rows));
            content.Add(field);
            var help = new HelpText();
            void ShowHint() => help.Set("An object, e.g. {\"level\": 3}");
            ShowHint();
            content.Add(help);
            field.Changed += _ => ShowHint();

            var buttons = Ui.Box("json-sheet__buttons");
            OverlayHandle sheet = null;
            var paste = new AppButton("Paste", () => field.Value = GUIUtility.systemCopyBuffer ?? "", AppButtonStyle.Outline);
            var apply = new AppButton("Apply", () =>
            {
                try
                {
                    var parsed = KeyValue.FromJson(field.Value);
                    _rows.Clear();
                    _rows.AddRange(parsed);
                    Refresh();
                    Changed?.Invoke();
                    sheet?.Close();
                }
                catch (FormatException)
                {
                    help.Set("Not valid JSON", true);
                }
            });
            buttons.Add(paste);
            buttons.Add(apply);
            content.Add(buttons);
            sheet = Sheet.Show(Overlay.HostFor(this), content);
        }

        static string Badge(ValueKind kind)
        {
            switch (kind)
            {
                case ValueKind.Number: return "123";
                case ValueKind.Bool: return "y/n";
                case ValueKind.Date: return "date";
                default: return "abc";
            }
        }

        static ValueKind Next(ValueKind kind)
        {
            switch (kind)
            {
                case ValueKind.Text: return ValueKind.Number;
                case ValueKind.Number: return ValueKind.Bool;
                case ValueKind.Bool: return ValueKind.Date;
                default: return ValueKind.Text;
            }
        }
    }
}
