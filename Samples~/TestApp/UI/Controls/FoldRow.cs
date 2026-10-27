using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace GrovsTestApp
{
    /// <summary>A row with a title and a summary that expands to show its content.</summary>
    public class FoldRow : VisualElement
    {
        const long AnimationMs = 200;

        readonly Func<string> _summary;
        readonly Label _summaryLabel;
        readonly VisualElement _body;
        readonly VisualElement _inner;
        IVisualElementScheduledItem _settle;
        bool _expanded;

        public event Action<bool> Toggled;

        public FoldRow(string title, Func<string> summary, VisualElement content, bool initiallyExpanded = false,
            (string title, string body)? help = null)
        {
            _summary = summary;
            AddToClassList("fold-row");

            var header = Ui.Box("fold-row__header");
            header.Add(Ui.LabelWithHelp(title, "t-body-strong", help));
            _summaryLabel = Ui.Text("", "t-caption", "fold-row__summary");
            header.Add(_summaryLabel);
            header.Add(new Chevron());
            Ui.OnTap(header, () => Expanded = !Expanded);
            Add(header);

            _body = Ui.Box("fold-row__body");
            _inner = Ui.Box("fold-row__content");
            _inner.Add(content);
            _body.Add(_inner);
            Add(_body);
            Add(Ui.Divider());

            _expanded = initiallyExpanded;
            EnableInClassList("fold-row--expanded", _expanded);
            _body.style.height = _expanded ? StyleKeyword.Auto : (StyleLength)0;
            _body.style.visibility = _expanded ? Visibility.Visible : Visibility.Hidden;
            RefreshSummary();
        }

        public bool Expanded
        {
            get => _expanded;
            set
            {
                if (value == _expanded) return;
                SetExpandedWithoutNotify(value);
                Toggled?.Invoke(value);
            }
        }

        public void SetExpandedWithoutNotify(bool expanded)
        {
            if (expanded == _expanded) return;
            _expanded = expanded;
            EnableInClassList("fold-row--expanded", expanded);
            RefreshSummary();
            Animate(expanded);
        }

        public void RefreshSummary()
        {
            _summaryLabel.text = _summary?.Invoke() ?? "";
            _summaryLabel.style.display = _expanded ? DisplayStyle.None : DisplayStyle.Flex;
        }

        // Height transitions need pixel values at both ends, so "auto" is swapped for the measured height around them.
        void Animate(bool expand)
        {
            _settle?.Pause();
            var measured = _inner.layout.height;
            if (float.IsNaN(measured) || panel == null)
            {
                _body.style.height = expand ? StyleKeyword.Auto : (StyleLength)0;
                _body.style.visibility = expand ? Visibility.Visible : Visibility.Hidden;
                return;
            }

            if (expand)
            {
                _body.style.visibility = Visibility.Visible;
                _body.style.height = measured;
                _settle = schedule.Execute(() => _body.style.height = StyleKeyword.Auto).StartingIn(AnimationMs + 50);
            }
            else
            {
                _body.style.height = _body.layout.height;
                schedule.Execute(() => _body.style.height = 0).StartingIn(16);
                _settle = schedule.Execute(() => _body.style.visibility = Visibility.Hidden).StartingIn(AnimationMs + 50);
            }
        }
    }

    /// <summary>A down arrow drawn in the element's --icon-color.</summary>
    public class Chevron : VisualElement
    {
        static readonly CustomStyleProperty<Color> IconColorProperty = new CustomStyleProperty<Color>("--icon-color");

        Color _color = Color.gray;

        public Chevron()
        {
            AddToClassList("chevron");
            pickingMode = PickingMode.Ignore;
            RegisterCallback<CustomStyleResolvedEvent>(_ =>
            {
                if (customStyle.TryGetValue(IconColorProperty, out var color)) _color = color;
                MarkDirtyRepaint();
            });
            generateVisualContent += Draw;
        }

        void Draw(MeshGenerationContext context)
        {
            var size = layout.width;
            if (float.IsNaN(size) || size <= 0) return;
            var unit = size / 24f;
            var painter = context.painter2D;
            painter.strokeColor = _color;
            painter.lineWidth = 2f * unit;
            painter.lineCap = LineCap.Round;
            painter.lineJoin = LineJoin.Round;
            painter.BeginPath();
            painter.MoveTo(new Vector2(7f, 9.5f) * unit);
            painter.LineTo(new Vector2(12f, 14.5f) * unit);
            painter.LineTo(new Vector2(17f, 9.5f) * unit);
            painter.Stroke();
        }
    }

    /// <summary>A row with a value that is copied on tap.</summary>
    public class CopyRow : SettingRow
    {
        readonly string _emptyText;
        readonly Label _value;
        readonly Label _copy;
        string _raw;

        public CopyRow(string label, string value, string emptyText = "Not set", (string title, string body)? help = null)
            : base(label, help: help)
        {
            _emptyText = emptyText;
            _value = Ui.Text("", "t-mono", "copy-row__value");
            _copy = Ui.Text("Copy", "t-label", "copy-row__action");
            Trailing.Add(_value);
            Trailing.Add(_copy);
            MakeTappable(Copy);
            Value = value;
        }

        public string Value
        {
            get => _raw;
            set
            {
                _raw = value;
                _value.text = value != null ? TextFormat.MiddleEllipsis(value) : _emptyText;
                _copy.style.display = value != null ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        void Copy()
        {
            if (_raw == null) return;
            GUIUtility.systemCopyBuffer = _raw;
            Toast.Show(Overlay.HostFor(this), "Copied");
        }
    }
}
