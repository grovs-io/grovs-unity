using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace GrovsTestApp
{
    public class Chip : VisualElement
    {
        const float Dash = 3f;
        const float Gap = 2f;

        static readonly CustomStyleProperty<Color> DashColorProperty = new CustomStyleProperty<Color>("--dash-color");

        readonly Label _label;
        Color _dashColor = Color.gray;
        bool _dashed;

        public event Action Clicked;

        public Chip(string text, Action onClick = null, bool selected = false, bool dashed = false, bool removable = false)
        {
            AddToClassList("chip");
            _label = Ui.Text(text, "t-caption", "chip__text");
            Add(_label);
            if (removable) Add(Ui.Text(" ×", "t-caption", "chip__remove"));
            if (onClick != null) Clicked += onClick;
            Ui.OnTap(this, () => Clicked?.Invoke());

            Selected = selected;
            Dashed = dashed;
            RegisterCallback<CustomStyleResolvedEvent>(_ =>
            {
                if (customStyle.TryGetValue(DashColorProperty, out var color)) _dashColor = color;
                MarkDirtyRepaint();
            });
            generateVisualContent += DrawDashes;
        }

        public string Text
        {
            get => _label.text;
            set => _label.text = value;
        }

        public bool Selected
        {
            set => EnableInClassList("chip--selected", value);
        }

        public bool Dashed
        {
            set
            {
                _dashed = value;
                EnableInClassList("chip--dashed", value);
                MarkDirtyRepaint();
            }
        }

        void DrawDashes(MeshGenerationContext context)
        {
            var w = layout.width;
            var h = layout.height;
            if (!_dashed || float.IsNaN(w) || w <= 0 || h <= 0) return;

            var painter = context.painter2D;
            painter.strokeColor = _dashColor;
            painter.lineWidth = 1f;
            painter.lineCap = LineCap.Butt;

            var r = Mathf.Min(w, h) / 2f - 0.5f;
            var straight = Mathf.Max(0, w - 2 * (r + 0.5f));
            var arc = Mathf.PI * r;
            var perimeter = 2 * straight + 2 * arc;
            for (var start = 0f; start < perimeter; start += Dash + Gap)
            {
                var end = Mathf.Min(start + Dash, perimeter);
                painter.BeginPath();
                painter.MoveTo(PointAt(start, w, h, r, straight, arc));
                for (var s = start + 1f; s < end; s += 1f) painter.LineTo(PointAt(s, w, h, r, straight, arc));
                painter.LineTo(PointAt(end, w, h, r, straight, arc));
                painter.Stroke();
            }
        }

        // A point on the pill outline, walked clockwise from the top left end of the top edge.
        static Vector2 PointAt(float s, float w, float h, float r, float straight, float arc)
        {
            var left = r + 0.5f;
            var cy = h / 2f;
            if (s < straight) return new Vector2(left + s, 0.5f);
            s -= straight;
            if (s < arc) return OnArc(new Vector2(left + straight, cy), r, -90f + 180f * s / arc);
            s -= arc;
            if (s < straight) return new Vector2(left + straight - s, h - 0.5f);
            s -= straight;
            return OnArc(new Vector2(left, cy), r, 90f + 180f * Mathf.Min(s, arc) / arc);
        }

        static Vector2 OnArc(Vector2 center, float r, float degrees)
        {
            var rad = degrees * Mathf.Deg2Rad;
            return center + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * r;
        }
    }

    /// <summary>A horizontally scrolling row of chips where one can be selected.</summary>
    public class ChipRow : ScrollView
    {
        readonly Action<string> _onPick;
        readonly Func<string, string> _label;
        readonly List<(string value, Chip chip)> _chips = new List<(string, Chip)>();
        string _selected;

        public ChipRow(IList<string> values, string selected, Action<string> onPick, Func<string, string> label = null)
            : base(ScrollViewMode.Horizontal)
        {
            AddToClassList("chip-row");
            horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            verticalScrollerVisibility = ScrollerVisibility.Hidden;
            _onPick = onPick;
            _label = label ?? (v => v);
            _selected = selected;
            SetValues(values);
        }

        public int Count => _chips.Count;

        public string Selected
        {
            get => _selected;
            set
            {
                _selected = value;
                foreach (var (v, chip) in _chips) chip.Selected = v == value;
            }
        }

        public void SetValues(IList<string> values)
        {
            Clear();
            _chips.Clear();
            foreach (var value in values)
            {
                var chip = new Chip(_label(value), () => _onPick?.Invoke(value), value == _selected);
                _chips.Add((value, chip));
                Add(chip);
            }
        }
    }

    /// <summary>Pick one option, or type another value when otherPlaceholder is given.</summary>
    public class ChipPicker : VisualElement
    {
        readonly IList<string> _options;
        readonly string _otherLabel;
        readonly string _otherPlaceholder;
        readonly Func<string, string> _display;
        readonly VisualElement _chips;
        readonly AppTextField _other;
        string _selected;
        bool _editingOther;
        string _typed = "";

        public event Action<string> Changed;

        public ChipPicker(IList<string> options, string selected, Action<string> onSelect = null, string label = null,
            string otherPlaceholder = null, string otherLabel = "Other…", Func<string, string> display = null,
            (string title, string body)? help = null)
        {
            _options = options;
            _otherLabel = otherLabel;
            _otherPlaceholder = otherPlaceholder;
            _display = display ?? (v => v);
            _selected = selected ?? "";
            if (onSelect != null) Changed += onSelect;

            AddToClassList("chip-picker");
            if (label != null) Add(new FieldLabel(label, help: help));

            var scroll = new ScrollView(ScrollViewMode.Horizontal)
            {
                horizontalScrollerVisibility = ScrollerVisibility.Hidden,
                verticalScrollerVisibility = ScrollerVisibility.Hidden
            };
            scroll.AddToClassList("chip-row");
            scroll.AddToClassList("chip-picker__row");
            _chips = scroll.contentContainer;
            Add(scroll);

            if (otherPlaceholder != null)
            {
                _other = new AppTextField(otherPlaceholder);
                _other.AddToClassList("chip-picker__other");
                _other.Changed += v =>
                {
                    _typed = v;
                    _editingOther = true;
                    Select(v);
                };
                Add(_other);
            }
            Rebuild();
        }

        public string Value
        {
            get => _selected;
            set
            {
                SetValueWithoutNotify(value);
                Changed?.Invoke(_selected);
            }
        }

        public void SetValueWithoutNotify(string value)
        {
            _selected = value ?? "";
            _editingOther = false;
            Rebuild();
        }

        string Custom => _selected.Length > 0 && !_options.Contains(_selected) ? _selected : null;

        void Select(string value)
        {
            _selected = value ?? "";
            Rebuild();
            Changed?.Invoke(_selected);
        }

        void Rebuild()
        {
            var custom = Custom;
            var showOther = _otherPlaceholder != null && (_editingOther || custom != null);
            var otherText = _editingOther ? _typed : custom ?? "";

            _chips.Clear();
            foreach (var option in _options)
            {
                var o = option;
                _chips.Add(new Chip(_display(o), () =>
                {
                    _editingOther = false;
                    Select(o);
                }, o == _selected && !_editingOther));
            }
            if (_otherPlaceholder != null)
            {
                _chips.Add(new Chip(otherText.Length == 0 ? _otherLabel : otherText, () =>
                {
                    _typed = Custom ?? "";
                    _editingOther = true;
                    Rebuild();
                }, showOther, !showOther));
            }
            foreach (var chip in _chips.Children()) chip.AddToClassList("chip-picker__chip");

            if (_other == null) return;
            _other.style.display = showOther ? DisplayStyle.Flex : DisplayStyle.None;
            if (_other.Value != otherText) _other.SetValueWithoutNotify(otherText);
        }
    }

    /// <summary>
    /// Edits a list of strings as chips. Typed text is committed on a comma, on Enter and on blur;
    /// tapping a chip removes it.
    /// </summary>
    public class ChipInput : VisualElement
    {
        readonly List<string> _values;
        readonly int? _max;
        readonly FieldLabel _label;
        readonly VisualElement _box;
        readonly TextInput _input;

        public event Action Changed;

        public ChipInput(List<string> values, string label = null, string placeholder = "Add", int? max = null,
            (string title, string body)? help = null)
        {
            _values = values;
            _max = max;
            AddToClassList("chip-input");
            if (label != null || max != null)
            {
                _label = new FieldLabel(label ?? "", help: help);
                Add(_label);
            }

            _box = Ui.Box("chip-input__box", TextInput.FieldAreaClass);
            _input = new TextInput(placeholder);
            _input.AddToClassList("chip-input__input");
            _input.Changed += OnTyped;
            _input.Submitted += CommitAll;
            _input.Blurred += CommitAll;
            _input.Focused += () => _box.AddToClassList("chip-input__box--focused");
            _input.Blurred += () => _box.RemoveFromClassList("chip-input__box--focused");
            _box.Add(_input);
            _box.RegisterCallback<ClickEvent>(evt =>
            {
                var target = evt.target as VisualElement;
                if (!(target is Chip) && target?.GetFirstAncestorOfType<Chip>() == null) _input.FocusField();
            });
            Add(_box);
            Refresh();
        }

        public IReadOnlyList<string> Values => _values;

        /// <summary>Redraws after the bound list was changed from outside.</summary>
        public void Refresh()
        {
            foreach (var chip in _box.Children().OfType<Chip>().ToList()) chip.RemoveFromHierarchy();
            for (var i = 0; i < _values.Count; i++)
            {
                var value = _values[i];
                var chip = new Chip(value, () => Remove(value), removable: true);
                chip.AddToClassList("chip-input__chip");
                _box.Insert(i, chip);
            }
            if (_max != null) _label.Trailing = _values.Count + " of " + _max;
            _box.EnableInClassList("chip-input__box--error", _max != null && _values.Count > _max);
        }

        void OnTyped(string text)
        {
            var (commit, rest) = ChipText.Split(text);
            if (commit.Count == 0) return;
            _input.SetValueWithoutNotify(rest);
            Commit(commit);
        }

        void CommitAll()
        {
            var (commit, _) = ChipText.Split(_input.Value + ",");
            if (_input.Value.Length > 0) _input.SetValueWithoutNotify("");
            Commit(commit);
        }

        void Commit(List<string> commit)
        {
            var added = commit.Where(v => !_values.Contains(v)).ToList();
            if (added.Count == 0) return;
            _values.AddRange(added);
            Refresh();
            Changed?.Invoke();
        }

        void Remove(string value)
        {
            _values.Remove(value);
            Refresh();
            Changed?.Invoke();
        }
    }
}
