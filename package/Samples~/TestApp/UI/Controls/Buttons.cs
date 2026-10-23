using System;
using UnityEngine.UIElements;

namespace GrovsTestApp
{
    public enum AppButtonStyle { Primary, Outline }

    /// <summary>
    /// A filled or outlined button. Disabled is only a look plus a click guard, so a long press
    /// still works on a disabled button and its saved values can be edited.
    /// </summary>
    public class AppButton : VisualElement
    {
        const long LongPressMs = 500;

        readonly Label _label;
        IVisualElementScheduledItem _pendingLongPress;
        bool _longPressed;
        bool _enabled = true;

        public event Action Clicked;
        public event Action LongPressed;

        public AppButton(string text, Action onClick = null, AppButtonStyle style = AppButtonStyle.Primary,
            bool small = false, Action onLongPress = null)
        {
            AddToClassList("app-button");
            AddToClassList(style == AppButtonStyle.Primary ? "app-button--primary" : "app-button--outline");
            EnableInClassList("app-button--small", small);
            _label = Ui.Text(text, small ? "t-caption" : "t-body-strong", "app-button__text");
            Add(_label);

            if (onClick != null) Clicked += onClick;
            if (onLongPress != null) LongPressed += onLongPress;

            RegisterCallback<PointerDownEvent>(OnPointerDown);
            RegisterCallback<PointerUpEvent>(_ => EndPress());
            RegisterCallback<PointerLeaveEvent>(_ => EndPress());
            RegisterCallback<PointerCancelEvent>(_ => EndPress());
            RegisterCallback<ClickEvent>(OnClick);
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
                EnableInClassList("app-button--disabled", !value);
            }
        }

        void OnPointerDown(PointerDownEvent evt)
        {
            _longPressed = false;
            if (_enabled) AddToClassList(Ui.PressedClass);
            if (LongPressed == null) return;
            _pendingLongPress?.Pause();
            _pendingLongPress = schedule.Execute(() =>
            {
                _longPressed = true;
                RemoveFromClassList(Ui.PressedClass);
                LongPressed?.Invoke();
            }).StartingIn(LongPressMs);
        }

        void EndPress()
        {
            _pendingLongPress?.Pause();
            _pendingLongPress = null;
            RemoveFromClassList(Ui.PressedClass);
        }

        void OnClick(ClickEvent evt)
        {
            if (_longPressed)
            {
                _longPressed = false;
                return;
            }
            if (_enabled) Clicked?.Invoke();
        }
    }
}
