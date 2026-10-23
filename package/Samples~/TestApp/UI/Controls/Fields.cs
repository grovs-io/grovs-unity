using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace GrovsTestApp
{
    /// <summary>A borderless TextField with a placeholder, set up for the touch keyboard.</summary>
    public class TextInput : VisualElement
    {
        public const string FieldAreaClass = "field-area";

        readonly Label _placeholder;

        public TextField Field { get; }

        public event Action<string> Changed;
        public event Action Submitted;
        public event Action Focused;
        public event Action Blurred;

        public TextInput(string placeholder = "", bool mono = false,
            TouchScreenKeyboardType keyboard = TouchScreenKeyboardType.Default, bool multiline = false)
        {
            AddToClassList("text-input");
            EnableInClassList("text-input--mono", mono);
            EnableInClassList("text-input--multiline", multiline);

            Field = new TextField { multiline = multiline, isPasswordField = false };
            Field.AddToClassList("text-input__field");
            Field.textEdition.keyboardType = keyboard;
            Field.textEdition.autoCorrection = false;
            Field.textEdition.hideMobileInput = false;
            Add(Field);

            _placeholder = Ui.Text(placeholder, "text-input__placeholder");
            _placeholder.pickingMode = PickingMode.Ignore;
            Add(_placeholder);

            Field.RegisterValueChangedCallback(evt =>
            {
                UpdatePlaceholder();
                Changed?.Invoke(evt.newValue);
            });
            Field.RegisterCallback<KeyDownEvent>(evt =>
            {
                var enter = evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter || evt.character == '\n';
                if (enter && !multiline) Submitted?.Invoke();
            }, TrickleDown.TrickleDown);
            Field.RegisterCallback<FocusInEvent>(_ => Focused?.Invoke());
            Field.RegisterCallback<FocusOutEvent>(_ => Blurred?.Invoke());
            UpdatePlaceholder();
        }

        public string Value
        {
            get => Field.value;
            set
            {
                SyncKeyboard(value);
                Field.value = value ?? "";
            }
        }

        public void SetValueWithoutNotify(string value)
        {
            SyncKeyboard(value);
            Field.SetValueWithoutNotify(value ?? "");
            UpdatePlaceholder();
        }

        /// <summary>Focuses the field unless it already has focus, which would reopen the touch keyboard.</summary>
        public void FocusField()
        {
            var focused = Field.panel?.focusController.focusedElement as VisualElement;
            if (focused != Field && !Field.Contains(focused)) Field.Focus();
        }

        public string Placeholder
        {
            get => _placeholder.text;
            set => _placeholder.text = value;
        }

        // The open touch keyboard owns the text while typing, so a value set from code must reach it too.
        void SyncKeyboard(string value)
        {
            var keyboard = Field.textEdition.touchScreenKeyboard;
            if (keyboard != null && keyboard.active) keyboard.text = value ?? "";
        }

        void UpdatePlaceholder() =>
            _placeholder.style.display = string.IsNullOrEmpty(Field.value) ? DisplayStyle.Flex : DisplayStyle.None;
    }

    /// <summary>A bordered text field. The border uses the accent while focused and the error colour on error.</summary>
    public class AppTextField : VisualElement
    {
        readonly TextInput _input;
        bool _error;

        public VisualElement Leading { get; }
        public VisualElement Trailing { get; }
        public TextField Input => _input.Field;

        public event Action<string> Changed
        {
            add => _input.Changed += value;
            remove => _input.Changed -= value;
        }

        public event Action Submitted
        {
            add => _input.Submitted += value;
            remove => _input.Submitted -= value;
        }

        public event Action Blurred
        {
            add => _input.Blurred += value;
            remove => _input.Blurred -= value;
        }

        public AppTextField(string placeholder = "", bool mono = false,
            TouchScreenKeyboardType keyboard = TouchScreenKeyboardType.Default, bool multiline = false)
        {
            AddToClassList("app-field");
            AddToClassList(TextInput.FieldAreaClass);
            EnableInClassList("app-field--multiline", multiline);

            Leading = Ui.Box("app-field__leading");
            _input = new TextInput(placeholder, mono, keyboard, multiline);
            _input.AddToClassList("app-field__input");
            Trailing = Ui.Box("app-field__trailing");
            Add(Leading);
            Add(_input);
            Add(Trailing);

            _input.Focused += () => AddToClassList("app-field--focused");
            _input.Blurred += () => RemoveFromClassList("app-field--focused");
            RegisterCallback<ClickEvent>(evt =>
            {
                if (!Trailing.Contains(evt.target as VisualElement)) _input.FocusField();
            });
        }

        public string Value
        {
            get => _input.Value;
            set => _input.Value = value;
        }

        public void SetValueWithoutNotify(string value) => _input.SetValueWithoutNotify(value);

        public bool Error
        {
            get => _error;
            set
            {
                _error = value;
                EnableInClassList("app-field--error", value);
            }
        }
    }

    public class SuggestField : VisualElement
    {
        readonly ChipRow _suggestions;
        readonly HelpText _error;
        readonly TextAction _clear;

        public AppTextField Field { get; }

        public event Action<string> Changed;

        public SuggestField(IList<string> suggestions, string label = null, string placeholder = "", bool mono = false,
            (string title, string body)? help = null)
        {
            AddToClassList("suggest-field");
            if (label != null) Add(new FieldLabel(label, help: help));

            Field = new AppTextField(placeholder, mono);
            _clear = new TextAction("×", () => Value = "", faint: true);
            Field.Trailing.Add(_clear);
            Add(Field);

            _error = new HelpText();
            Add(_error);

            _suggestions = new ChipRow(suggestions.Distinct().ToList(), null, v => Value = v);
            _suggestions.AddToClassList("suggest-field__chips");
            Add(_suggestions);

            Field.Changed += v =>
            {
                Refresh();
                Changed?.Invoke(v);
            };
            Refresh();
        }

        public string Value
        {
            get => Field.Value;
            set => Field.Value = value;
        }

        public void SetValueWithoutNotify(string value)
        {
            Field.SetValueWithoutNotify(value);
            Refresh();
        }

        public string Error
        {
            set
            {
                _error.Set(value, true);
                Field.Error = value != null;
            }
        }

        public void SetSuggestions(IList<string> suggestions)
        {
            _suggestions.SetValues(suggestions.Distinct().ToList());
            Refresh();
        }

        void Refresh()
        {
            _clear.style.display = string.IsNullOrEmpty(Value) ? DisplayStyle.None : DisplayStyle.Flex;
            _suggestions.Selected = Value;
            _suggestions.style.display = _suggestions.Count == 0 ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }

    public class LinkField : VisualElement
    {
        readonly bool _showPath;
        readonly HelpText _help;

        public AppTextField Field { get; }

        public event Action<string> Changed;
        public event Action Done;

        public LinkField(string label = null, string placeholder = "https://", bool showPath = false,
            (string title, string body)? help = null)
        {
            _showPath = showPath;
            AddToClassList("link-field");
            if (label != null) Add(new FieldLabel(label, help: help));

            Field = new AppTextField(placeholder, true, TouchScreenKeyboardType.URL);
            Field.Trailing.Add(new TextAction("Paste", Paste));
            Field.Submitted += () => Done?.Invoke();
            Field.Changed += v =>
            {
                Refresh();
                Changed?.Invoke(v);
            };
            Add(Field);

            _help = new HelpText();
            Add(_help);
            Refresh();
        }

        public string Value
        {
            get => Field.Value;
            set => Field.Value = value;
        }

        public void SetValueWithoutNotify(string value)
        {
            Field.SetValueWithoutNotify(value);
            Refresh();
        }

        void Paste()
        {
            var text = GUIUtility.systemCopyBuffer?.Trim();
            if (string.IsNullOrEmpty(text)) return;
            Value = text;
            if (_showPath) Done?.Invoke();
        }

        void Refresh()
        {
            var value = Value;
            if (string.IsNullOrWhiteSpace(value))
            {
                _help.Set(null);
            }
            else if (_showPath)
            {
                var path = LinkPath.Extract(value);
                _help.Set(path != null ? "Path: " + path : "No path found", path == null);
            }
            else
            {
                _help.Set(TextFormat.Url(value) ? null : "Must start with http:// or https://", true);
            }
        }
    }

    public class MoneyField : VisualElement
    {
        readonly Label _symbol;
        readonly HelpText _help;

        public AppTextField Field { get; }

        public event Action<string> Changed;

        public MoneyField(string currency)
        {
            AddToClassList("money-field");
            Field = new AppTextField("", false, TouchScreenKeyboardType.DecimalPad);
            Field.AddToClassList("money-field__field");
            _symbol = Ui.Text("", "t-amount", "money-field__symbol");
            Field.Leading.Add(_symbol);
            Field.Changed += OnTyped;
            Add(Field);

            _help = new HelpText();
            Add(_help);
            Currency = currency;
            Refresh();
        }

        public string Currency
        {
            set => _symbol.text = Symbol(value);
        }

        public string Value
        {
            get => Field.Value;
            set => Field.Value = value;
        }

        public void SetValueWithoutNotify(string value)
        {
            Field.SetValueWithoutNotify(value);
            Refresh();
        }

        void OnTyped(string typed)
        {
            var clean = new string((typed ?? "").Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray());
            if (clean != typed)
            {
                Field.SetValueWithoutNotify(clean);
            }
            Refresh();
            Changed?.Invoke(clean);
        }

        void Refresh()
        {
            var cents = Money.ParseCents(Value);
            Field.Error = cents == null;
            _help.Set(cents != null ? "Sent as " + cents + " cents" : "Enter an amount like 4.99", cents == null);
        }

        static string Symbol(string currency)
        {
            switch (currency)
            {
                case "USD": return "$";
                case "EUR": return "€";
                case "GBP": return "£";
                case "JPY": return "¥";
                default: return currency ?? "";
            }
        }
    }
}
