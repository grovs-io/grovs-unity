using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace GrovsTestApp
{
    /// <summary>Small builders shared by the controls.</summary>
    public static class Ui
    {
        public const string PressedClass = "is-pressed";

        public static Label Text(string text, params string[] classes)
        {
            var label = new Label(text);
            foreach (var c in classes) label.AddToClassList(c);
            return label;
        }

        public static VisualElement Box(params string[] classes)
        {
            var box = new VisualElement();
            foreach (var c in classes) box.AddToClassList(c);
            return box;
        }

        public static VisualElement Divider() => Box("divider");

        /// <summary>Calls onClick on a tap and shows a pressed state while the pointer is down.</summary>
        public static void OnTap(VisualElement element, Action onClick)
        {
            element.RegisterCallback<ClickEvent>(_ => onClick());
            TrackPress(element);
        }

        public static void TrackPress(VisualElement element)
        {
            element.RegisterCallback<PointerDownEvent>(_ => element.AddToClassList(PressedClass));
            element.RegisterCallback<PointerUpEvent>(_ => element.RemoveFromClassList(PressedClass));
            element.RegisterCallback<PointerLeaveEvent>(_ => element.RemoveFromClassList(PressedClass));
            element.RegisterCallback<PointerCancelEvent>(_ => element.RemoveFromClassList(PressedClass));
        }

        /// <summary>A label that takes the free width of its row, with an optional "?" right after the text.</summary>
        public static VisualElement LabelWithHelp(string text, string textClass, (string title, string body)? help)
        {
            var row = Box("label-with-help");
            row.Add(Text(text, textClass, "label-with-help__text"));
            if (help.HasValue) row.Add(new InfoButton(help.Value.title, help.Value.body));
            return row;
        }

        public static string StateClass(LogState state)
        {
            switch (state)
            {
                case LogState.Ok: return "state-ok";
                case LogState.Warning: return "state-warning";
                case LogState.Error: return "state-error";
                default: return "state-neutral";
            }
        }

        public static void SetStateClass(VisualElement element, LogState state)
        {
            foreach (LogState s in Enum.GetValues(typeof(LogState))) element.RemoveFromClassList(StateClass(s));
            element.AddToClassList(StateClass(state));
        }
    }

    public class ScreenTitle : VisualElement
    {
        public ScreenTitle(string title, string action = null, Action onAction = null)
        {
            AddToClassList("screen-title");
            Add(Ui.Text(title, "t-title", "screen-title__text"));
            if (action != null) Add(new TextAction(action, onAction));
        }
    }

    public class SectionHeader : VisualElement
    {
        public SectionHeader(string text, string trailing = null, string action = null, Action onAction = null,
            (string title, string body)? help = null)
        {
            AddToClassList("section-header");
            Add(Ui.LabelWithHelp(text, "t-label", help));
            if (trailing != null) Add(Ui.Text(trailing, "t-caption", "text-faint"));
            if (action != null) Add(new TextAction(action, onAction));
        }
    }

    public class FieldLabel : VisualElement
    {
        readonly Label _trailing;

        public FieldLabel(string text, string trailing = null, string action = null, Action onAction = null,
            (string title, string body)? help = null)
        {
            AddToClassList("field-label");
            Add(Ui.LabelWithHelp(text, "t-label", help));
            _trailing = Ui.Text(trailing ?? "", "t-caption");
            _trailing.style.display = trailing == null ? DisplayStyle.None : DisplayStyle.Flex;
            Add(_trailing);
            if (action != null) Add(new TextAction(action, onAction));
        }

        public string Trailing
        {
            get => _trailing.text;
            set
            {
                _trailing.text = value ?? "";
                _trailing.style.display = value == null ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }
    }

    public class HelpText : Label
    {
        public HelpText(string text = "", bool error = false)
        {
            AddToClassList("t-caption");
            AddToClassList("help-text");
            Set(text, error);
        }

        public void Set(string text, bool error = false)
        {
            this.text = text ?? "";
            EnableInClassList("help-text--error", error);
            style.display = string.IsNullOrEmpty(text) ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }

    public class TextAction : VisualElement
    {
        readonly Label _label;
        bool _enabled = true;

        public event Action Clicked;

        public TextAction(string text, Action onClick = null, bool faint = false)
        {
            AddToClassList("text-action");
            EnableInClassList("text-action--faint", faint);
            _label = Ui.Text(text, "t-label", "text-action__text");
            Add(_label);
            if (onClick != null) Clicked += onClick;
            Ui.OnTap(this, () =>
            {
                if (_enabled) Clicked?.Invoke();
            });
        }

        public string Text
        {
            get => _label.text;
            set => _label.text = value;
        }

        public bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                EnableInClassList("text-action--disabled", !value);
            }
        }
    }

    public class SettingRow : VisualElement
    {
        readonly VisualElement _content;

        public VisualElement Trailing { get; }

        public SettingRow(string label, Action onClick = null, bool divider = true, (string title, string body)? help = null)
        {
            AddToClassList("setting-row");
            var row = Ui.Box("setting-row__content");
            row.Add(Ui.LabelWithHelp(label, "t-body", help));
            Trailing = Ui.Box("setting-row__trailing");
            row.Add(Trailing);
            _content = row;
            if (onClick != null) MakeTappable(onClick);
            Add(row);
            if (divider) Add(Ui.Divider());
        }

        public void MakeTappable(Action onClick)
        {
            _content.AddToClassList("setting-row__content--tappable");
            Ui.OnTap(_content, onClick);
        }
    }

    public class AppSwitch : VisualElement
    {
        bool _value;

        public event Action<bool> Changed;

        public AppSwitch(bool value = false)
        {
            AddToClassList("app-switch");
            Add(Ui.Box("app-switch__thumb"));
            SetValueWithoutNotify(value);
        }

        public bool Value
        {
            get => _value;
            set
            {
                if (value == _value) return;
                SetValueWithoutNotify(value);
                Changed?.Invoke(value);
            }
        }

        public void SetValueWithoutNotify(bool value)
        {
            _value = value;
            EnableInClassList("app-switch--on", value);
        }
    }

    public class SwitchRow : SettingRow
    {
        readonly AppSwitch _switch;

        public event Action<bool> Changed
        {
            add => _switch.Changed += value;
            remove => _switch.Changed -= value;
        }

        public SwitchRow(string label, bool value, Action<bool> onChange = null, string supporting = null,
            bool divider = true, (string title, string body)? help = null)
            : base(label, divider: divider, help: help)
        {
            _switch = new AppSwitch(value) { pickingMode = PickingMode.Ignore };
            if (supporting != null) Trailing.Add(Ui.Text(supporting, "t-caption"));
            Trailing.Add(_switch);
            if (onChange != null) Changed += onChange;
            MakeTappable(() => _switch.Value = !_switch.Value);
        }

        public bool Value
        {
            get => _switch.Value;
            set => _switch.Value = value;
        }

        public void SetValueWithoutNotify(bool value) => _switch.SetValueWithoutNotify(value);
    }

    public class Segment<T> : VisualElement
    {
        public const string OptionClass = "segment__option";

        readonly IList<T> _options;
        readonly List<VisualElement> _items = new List<VisualElement>();
        T _value;

        public event Action<T> Changed;

        public Segment(IList<T> options, T selected, Action<T> onChange = null, bool compact = false, Func<T, string> label = null)
        {
            _options = options;
            AddToClassList("segment");
            EnableInClassList("segment--compact", compact);
            for (var i = 0; i < options.Count; i++)
            {
                var option = options[i];
                var item = Ui.Box(OptionClass);
                item.name = "option-" + i;
                if (i > 0) item.style.marginLeft = 3;
                item.Add(Ui.Text(label != null ? label(option) : option?.ToString() ?? "", "t-caption", "segment__text"));
                Ui.OnTap(item, () => Value = option);
                _items.Add(item);
                Add(item);
            }
            if (onChange != null) Changed += onChange;
            SetValueWithoutNotify(selected);
        }

        public T Value
        {
            get => _value;
            set
            {
                if (EqualityComparer<T>.Default.Equals(value, _value)) return;
                SetValueWithoutNotify(value);
                Changed?.Invoke(value);
            }
        }

        public void SetValueWithoutNotify(T value)
        {
            _value = value;
            for (var i = 0; i < _items.Count; i++)
                _items[i].EnableInClassList("segment__option--on", EqualityComparer<T>.Default.Equals(_options[i], value));
        }
    }

    public class TriStateRow : SettingRow
    {
        static readonly bool?[] Options = { null, true, false };

        readonly Segment<bool?> _segment;

        public event Action<bool?> Changed
        {
            add => _segment.Changed += value;
            remove => _segment.Changed -= value;
        }

        public TriStateRow(string label, bool? value, Action<bool?> onChange = null, (string title, string body)? help = null)
            : base(label, help: help)
        {
            _segment = new Segment<bool?>(Options, value, onChange, true, Name);
            _segment.AddToClassList("tri-state");
            Trailing.Add(_segment);
        }

        public bool? Value
        {
            get => _segment.Value;
            set => _segment.Value = value;
        }

        public void SetValueWithoutNotify(bool? value) => _segment.SetValueWithoutNotify(value);

        static string Name(bool? value) => value == null ? "Default" : value.Value ? "On" : "Off";
    }

    public class StatusDot : VisualElement
    {
        public StatusDot(LogState state)
        {
            AddToClassList("status-dot");
            State = state;
        }

        public LogState State
        {
            set => Ui.SetStateClass(this, value);
        }
    }

    public class StatusPill : Label
    {
        public StatusPill(string text, LogState state) : base(text)
        {
            AddToClassList("t-caption");
            AddToClassList("status-pill");
            State = state;
        }

        public LogState State
        {
            set => Ui.SetStateClass(this, value);
        }
    }
}
