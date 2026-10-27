using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace GrovsTestApp
{
    /// <summary>Keeps the focused field visible above the touch keyboard, and closes it on a tap outside a field.</summary>
    public static class Keyboard
    {
        const float Above = 28;
        const float Below = 16;
        const string SpacerName = "keyboard-spacer";
        const string CoverName = "keyboard-cover";

        const long FirstCheckMs = 50;
        // The keyboard slides in at its own pace, so its height is checked again while a field has focus.
        const long CheckEveryMs = 150;

        static readonly HashSet<IPanel> WatchedPanels = new HashSet<IPanel>();
        static bool _pointerHeld;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            WatchedPanels.Clear();
            _pointerHeld = false;
        }

        /// <summary>Scrolls a focused field inside the scroll view into view with room above and below it.</summary>
        public static void Track(ScrollView scroll)
        {
            scroll.RegisterCallback<AttachToPanelEvent>(evt => WatchTaps(evt.destinationPanel));
            if (scroll.panel != null) WatchTaps(scroll.panel);

            VisualElement focusedField = null;
            var watch = WatchHeight(scroll, _ =>
            {
                if (focusedField != null) Reveal(scroll, focusedField);
            });
            scroll.contentContainer.RegisterCallback<FocusInEvent>(evt =>
            {
                focusedField = FieldArea(evt.target as VisualElement);
                if (focusedField != null) watch.Start();
            }, TrickleDown.TrickleDown);
            scroll.contentContainer.RegisterCallback<FocusOutEvent>(_ =>
            {
                watch.Stop();
                WhenReleased(scroll, () => ClearRoom(scroll));
            }, TrickleDown.TrickleDown);
        }

        /// <summary>Moves an element anchored to the bottom of the screen up by the keyboard height while one of its fields is focused.</summary>
        public static void Lift(VisualElement element)
        {
            element.RegisterCallback<AttachToPanelEvent>(evt => WatchTaps(evt.destinationPanel));
            var watch = WatchHeight(element, keyboard => LiftBy(element, keyboard));
            element.RegisterCallback<FocusInEvent>(evt =>
            {
                if (FieldArea(evt.target as VisualElement) != null) watch.Start();
            }, TrickleDown.TrickleDown);
            element.RegisterCallback<FocusOutEvent>(_ =>
            {
                watch.Stop();
                WhenReleased(element, () =>
                {
                    if (!HasFocusedField(element)) LiftBy(element, 0);
                });
            }, TrickleDown.TrickleDown);
        }

        sealed class HeightWatch
        {
            public Action Start;
            public Action Stop;
        }

        // Calls back with the keyboard height each time it changes while started.
        static HeightWatch WatchHeight(VisualElement owner, Action<float> changed)
        {
            var applied = -1f;
            IVisualElementScheduledItem item = null;
            void Check()
            {
                var keyboard = HeightInPanel(owner);
                if (Mathf.Abs(keyboard - applied) < 1f) return;
                applied = keyboard;
                changed(keyboard);
            }
            return new HeightWatch
            {
                Start = () =>
                {
                    applied = -1f;
                    item?.Pause();
                    item = owner.schedule.Execute(Check).StartingIn(FirstCheckMs).Every(CheckEveryMs);
                },
                Stop = () => item?.Pause()
            };
        }

        // A tap outside a field closes the keyboard on touch down. The layout waits for the finger to lift,
        // so the button under it stays put and still gets the click.
        static void WhenReleased(VisualElement element, Action action)
        {
            IVisualElementScheduledItem wait = null;
            wait = element.schedule.Execute(() =>
            {
                if (_pointerHeld) return;
                wait.Pause();
                action();
            }).StartingIn(FirstCheckMs).Every(16);
        }

        // Also caps the height, so a lifted element never runs off the top of the screen.
        static void LiftBy(VisualElement element, float keyboard)
        {
            if (keyboard <= 0 || element.panel == null)
            {
                element.style.bottom = StyleKeyword.Null;
                element.style.maxHeight = StyleKeyword.Null;
                Cover(element, 0);
                return;
            }
            Cover(element, keyboard);
            element.style.bottom = keyboard;
            element.style.maxHeight = Mathf.Max(0, element.panel.visualTree.layout.height - keyboard - Above);
        }

        /// <summary>Height of the visible touch keyboard in panel units, or 0.</summary>
        public static float HeightInPanel(VisualElement element)
        {
            if (!TouchScreenKeyboard.isSupported || !TouchScreenKeyboard.visible || element.panel == null) return 0;
            var panelHeight = element.panel.visualTree.layout.height;
            if (float.IsNaN(panelHeight) || Screen.height <= 0) return 0;
            return CoveredPixels() * panelHeight / Screen.height;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        // Unity keeps its input strip window above the Android keyboard even when the strip is hidden, and that window takes the touches.
        const float InputStripDp = 56;

        // TouchScreenKeyboard.area reads 0 on Android, so the window's visible frame is used instead.
        static float CoveredPixels()
        {
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var window = activity.Call<AndroidJavaObject>("getWindow"))
                using (var decor = window.Call<AndroidJavaObject>("getDecorView"))
                using (var frame = new AndroidJavaObject("android.graphics.Rect"))
                {
                    decor.Call("getWindowVisibleDisplayFrame", frame);
                    var height = decor.Call<int>("getHeight");
                    var covered = height - frame.Get<int>("bottom");
                    if (height <= 0 || covered < height * 0.15f) return 0;
                    var strip = InputStripDp * Screen.dpi / 160f;
                    return (covered + strip) * Screen.height / height;
                }
            }
            catch (Exception)
            {
                return 0;
            }
        }
#else
        static float CoveredPixels() => TouchScreenKeyboard.area.height;
#endif

        static void Reveal(ScrollView scroll, VisualElement field)
        {
            if (field.panel == null || !HasFocusedField(field)) return;
            var keyboard = HeightInPanel(scroll);
            var spacer = Spacer(scroll);
            spacer.BringToFront();
            spacer.style.height = keyboard;
            Cover(scroll, keyboard);
            scroll.schedule.Execute(() => ScrollTo(scroll, field, keyboard)).StartingIn(16);
        }

        static void ScrollTo(ScrollView scroll, VisualElement field, float keyboard)
        {
            var view = scroll.contentViewport.worldBound;
            var visibleTop = view.yMin;
            var visibleBottom = Mathf.Min(view.yMax, scroll.panel.visualTree.layout.height - keyboard);
            var top = field.worldBound.yMin - Above;
            var bottom = field.worldBound.yMax + Below;

            var delta = 0f;
            if (bottom > visibleBottom) delta = bottom - visibleBottom;
            if (top - delta < visibleTop) delta = top - visibleTop;
            if (Mathf.Abs(delta) > 0.5f) scroll.scrollOffset += new Vector2(0, delta);
        }

        static void ClearRoom(ScrollView scroll)
        {
            if (HasFocusedField(scroll.contentContainer)) return;
            Spacer(scroll).style.height = 0;
            Cover(scroll, 0);
        }

        // Unity's input strip window above the keyboard takes touches even where nothing is drawn,
        // so that band is painted in the background colour instead of showing content that cannot be tapped.
        static void Cover(VisualElement element, float height)
        {
            if (element.panel == null) return;
            var host = Overlay.HostFor(element);
            var cover = host.Q(CoverName);
            if (cover == null)
            {
                if (height <= 0) return;
                cover = new VisualElement { name = CoverName, pickingMode = PickingMode.Ignore };
                cover.AddToClassList(CoverName);
                host.Add(cover);
            }
            cover.style.height = height;
            cover.style.display = height > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            cover.BringToFront();
        }

        // Room at the end of the content so the last fields can scroll above the keyboard.
        static VisualElement Spacer(ScrollView scroll)
        {
            var spacer = scroll.contentContainer.Q(SpacerName);
            if (spacer != null) return spacer;
            spacer = new VisualElement { name = SpacerName, pickingMode = PickingMode.Ignore };
            spacer.style.flexShrink = 0;
            scroll.contentContainer.Add(spacer);
            return spacer;
        }

        static void WatchTaps(IPanel panel)
        {
            if (panel == null || !WatchedPanels.Add(panel)) return;
            panel.visualTree.RegisterCallback<PointerDownEvent>(evt =>
            {
                _pointerHeld = true;
                var focused = panel.focusController.focusedElement as VisualElement;
                if (FieldArea(focused) == null || FieldArea(evt.target as VisualElement) != null) return;
                focused.Blur();
            }, TrickleDown.TrickleDown);
            panel.visualTree.RegisterCallback<PointerUpEvent>(_ => _pointerHeld = false, TrickleDown.TrickleDown);
            panel.visualTree.RegisterCallback<PointerCancelEvent>(_ => _pointerHeld = false, TrickleDown.TrickleDown);
            panel.visualTree.RegisterCallback<DetachFromPanelEvent>(_ => WatchedPanels.Remove(panel));
        }

        static bool HasFocusedField(VisualElement within)
        {
            var focused = within.panel?.focusController.focusedElement as VisualElement;
            return focused != null && FieldArea(focused) != null && (within == focused || within.Contains(focused));
        }

        /// <summary>The bordered area around a text field, or the field itself, that holds the element.</summary>
        static VisualElement FieldArea(VisualElement element)
        {
            VisualElement textField = null;
            for (var e = element; e != null; e = e.parent)
            {
                if (e.ClassListContains(TextInput.FieldAreaClass)) return e;
                if (textField == null && e is TextField) textField = e;
            }
            return textField;
        }
    }
}
