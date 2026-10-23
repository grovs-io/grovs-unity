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

        // The keyboard slides in over a few hundred milliseconds, so the field is revealed again once it settles.
        static readonly long[] RevealDelaysMs = { 50, 350, 700 };

        static readonly HashSet<IPanel> WatchedPanels = new HashSet<IPanel>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => WatchedPanels.Clear();

        /// <summary>Scrolls a focused field inside the scroll view into view with room above and below it.</summary>
        public static void Track(ScrollView scroll)
        {
            scroll.RegisterCallback<AttachToPanelEvent>(evt => WatchTaps(evt.destinationPanel));
            if (scroll.panel != null) WatchTaps(scroll.panel);

            scroll.contentContainer.RegisterCallback<FocusInEvent>(evt =>
            {
                var field = FieldArea(evt.target as VisualElement);
                if (field == null) return;
                foreach (var delay in RevealDelaysMs)
                    scroll.schedule.Execute(() => Reveal(scroll, field)).StartingIn(delay);
            }, TrickleDown.TrickleDown);
            scroll.contentContainer.RegisterCallback<FocusOutEvent>(_ =>
                scroll.schedule.Execute(() => ClearRoom(scroll)).StartingIn(RevealDelaysMs[0]), TrickleDown.TrickleDown);
        }

        /// <summary>Moves an element anchored to the bottom of the screen up by the keyboard height while one of its fields is focused.</summary>
        public static void Lift(VisualElement element)
        {
            element.RegisterCallback<AttachToPanelEvent>(evt => WatchTaps(evt.destinationPanel));
            element.RegisterCallback<FocusInEvent>(evt =>
            {
                if (FieldArea(evt.target as VisualElement) == null) return;
                foreach (var delay in RevealDelaysMs)
                    element.schedule.Execute(() => LiftBy(element, HeightInPanel(element))).StartingIn(delay);
            }, TrickleDown.TrickleDown);
            element.RegisterCallback<FocusOutEvent>(_ =>
                element.schedule.Execute(() =>
                {
                    if (!HasFocusedField(element)) LiftBy(element, 0);
                }).StartingIn(RevealDelaysMs[0]), TrickleDown.TrickleDown);
        }

        // Also caps the height, so a lifted element never runs off the top of the screen.
        static void LiftBy(VisualElement element, float keyboard)
        {
            if (keyboard <= 0 || element.panel == null)
            {
                element.style.bottom = StyleKeyword.Null;
                element.style.maxHeight = StyleKeyword.Null;
                return;
            }
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
        // Unity's single line input strip that sits above the Android keyboard.
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
            if (!HasFocusedField(scroll.contentContainer)) Spacer(scroll).style.height = 0;
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
                var focused = panel.focusController.focusedElement as VisualElement;
                if (FieldArea(focused) == null || FieldArea(evt.target as VisualElement) != null) return;
                focused.Blur();
            }, TrickleDown.TrickleDown);
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
