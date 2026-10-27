using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace GrovsTestApp
{
    /// <summary>An open sheet or dialog.</summary>
    public sealed class OverlayHandle
    {
        readonly Action _dismiss;

        public bool IsOpen { get; private set; } = true;

        public event Action Closed;

        /// <summary>Gets the first say on a back press; returning true keeps the overlay open.</summary>
        public Func<bool> BackHandler { get; set; }

        internal OverlayHandle(Action dismiss)
        {
            _dismiss = dismiss;
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            _dismiss();
            Closed?.Invoke();
        }

        public void HandleBack()
        {
            if (!IsOpen) return;
            if (BackHandler != null && BackHandler()) return;
            Close();
        }

        // For an overlay removed without Close, for example when its screen is rebuilt.
        internal void MarkClosed()
        {
            if (!IsOpen) return;
            IsOpen = false;
            Closed?.Invoke();
        }
    }

    /// <summary>
    /// The full-size layer above the app that holds sheets, dialogs and toasts. Escape (the Android
    /// back button) closes the newest sheet or dialog.
    /// </summary>
    public static class Overlay
    {
        public const int AnimationMs = 250;
        const string LayerName = "overlay-layer";

        static readonly List<OverlayHandle> Open = new List<OverlayHandle>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Open.Clear();

        public static VisualElement DefaultHost =>
            AppRoot.Instance != null ? AppRoot.Instance.Root : throw new InvalidOperationException("No AppRoot to show the overlay on");

        /// <summary>The app root, or the document root holding the element when there is no app.</summary>
        public static VisualElement HostFor(VisualElement element)
        {
            if (AppRoot.Instance != null && AppRoot.Instance.Root != null) return AppRoot.Instance.Root;
            var top = element.panel?.visualTree;
            if (top == null) return DefaultHost;
            var root = element;
            while (root.parent != null && root.parent != top) root = root.parent;
            return root;
        }

        /// <summary>The newest open sheet or dialog, the one Escape closes.</summary>
        public static OverlayHandle Newest => Open.Count > 0 ? Open[Open.Count - 1] : null;

        public static VisualElement Layer(VisualElement host)
        {
            var layer = host.Q(LayerName);
            if (layer == null)
            {
                layer = Ui.Box("overlay-layer");
                layer.name = LayerName;
                layer.pickingMode = PickingMode.Ignore;
                host.Add(layer);
            }
            layer.BringToFront();
            return layer;
        }

        /// <summary>Adds an overlay item that closes on Escape while it is the newest one.</summary>
        public static OverlayHandle Push(VisualElement host, VisualElement item, Action animateOut)
        {
            OverlayHandle handle = null;
            handle = new OverlayHandle(() =>
            {
                Open.Remove(handle);
                animateOut();
                item.schedule.Execute(item.RemoveFromHierarchy).StartingIn(AnimationMs);
            });
            Open.Add(handle);
            item.RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                Open.Remove(handle);
                handle.MarkClosed();
            });
            CloseOnBack(item, handle);
            Layer(host).Add(item);
            return handle;
        }

        /// <summary>Bottom padding the host keeps clear for the navigation bar or home indicator.</summary>
        public static float SafeBottom(VisualElement host)
        {
            var padding = host.resolvedStyle.paddingBottom;
            return float.IsNaN(padding) ? 0 : padding;
        }

        static bool IsNewest(OverlayHandle handle) => Newest == handle;

#if ENABLE_LEGACY_INPUT_MANAGER
        // The Android back button reaches Unity as Escape but not as a UI Toolkit event, so it is
        // polled, only while the item is open.
        static void CloseOnBack(VisualElement item, OverlayHandle handle)
        {
            var handledFrame = -1;
            item.schedule.Execute(() =>
            {
                if (!handle.IsOpen || !IsNewest(handle) || !Input.GetKeyDown(KeyCode.Escape)) return;
                if (handledFrame == Time.frameCount) return;
                handledFrame = Time.frameCount;
                handle.HandleBack();
            }).Every(0);
        }
#else
        static void CloseOnBack(VisualElement item, OverlayHandle handle)
        {
            item.RegisterCallback<AttachToPanelEvent>(evt =>
                evt.destinationPanel.visualTree.RegisterCallback<KeyDownEvent>(OnKey, TrickleDown.TrickleDown));
            item.RegisterCallback<DetachFromPanelEvent>(evt =>
                evt.originPanel.visualTree.UnregisterCallback<KeyDownEvent>(OnKey, TrickleDown.TrickleDown));

            void OnKey(KeyDownEvent evt)
            {
                if (evt.keyCode != KeyCode.Escape || !IsNewest(handle)) return;
                evt.StopPropagation();
                handle.HandleBack();
            }
        }
#endif
    }

    /// <summary>
    /// A bottom sheet over the whole app. Without a height fraction it wraps its content and scrolls.
    /// Closes on a tap on the scrim or the handle, a drag down past a quarter of its height, or Escape.
    /// </summary>
    public static class Sheet
    {
        const float DismissFraction = 0.25f;
        const float DismissVelocity = 430f;
        const float TapSlop = 4f;

        public static OverlayHandle Show(VisualElement content, float? fraction = null) =>
            Show(Overlay.DefaultHost, content, fraction);

        public static OverlayHandle Show(VisualElement host, VisualElement content, float? fraction = null)
        {
            var root = Ui.Box("sheet");
            var scrim = Ui.Box("sheet__scrim");
            var panel = Ui.Box("sheet__panel");
            panel.focusable = true;
            root.Add(scrim);
            root.Add(panel);

            var handleArea = Ui.Box("sheet__handle-area");
            handleArea.Add(Ui.Box("sheet__handle"));
            panel.Add(handleArea);

            VisualElement body;
            if (fraction.HasValue)
            {
                panel.style.height = Length.Percent(Mathf.Clamp01(fraction.Value) * 100f);
                body = Ui.Box("sheet__body", "sheet__body--fill");
                panel.Add(body);
            }
            else
            {
                var scroll = new ScrollView(ScrollViewMode.Vertical);
                scroll.AddToClassList("sheet__scroll");
                panel.Add(scroll);
                body = Ui.Box("sheet__body");
                scroll.Add(body);
            }
            body.style.paddingBottom = 14 + Overlay.SafeBottom(host);
            body.Add(content);
            Keyboard.Lift(panel);

            var handle = Overlay.Push(host, root, () => root.RemoveFromClassList("sheet--open"));
            scrim.RegisterCallback<ClickEvent>(_ => handle.Close());
            Drag(handleArea, panel, handle);
            root.schedule.Execute(() =>
            {
                root.AddToClassList("sheet--open");
                panel.Focus();
            }).StartingIn(16);
            return handle;
        }

        static void Drag(VisualElement handleArea, VisualElement panel, OverlayHandle handle)
        {
            var dragging = false;
            float startY = 0, offset = 0, lastY = 0, lastTime = 0, velocity = 0;

            handleArea.RegisterCallback<PointerDownEvent>(evt =>
            {
                dragging = true;
                startY = lastY = evt.position.y;
                lastTime = Time.realtimeSinceStartup;
                offset = velocity = 0;
                handleArea.CapturePointer(evt.pointerId);
                panel.AddToClassList("sheet__panel--dragging");
            });
            handleArea.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (!dragging) return;
                var now = Time.realtimeSinceStartup;
                if (now > lastTime) velocity = (evt.position.y - lastY) / (now - lastTime);
                lastY = evt.position.y;
                lastTime = now;
                offset = Mathf.Max(0, evt.position.y - startY);
                panel.style.translate = new Translate(0, offset);
            });

            void End(int pointerId)
            {
                if (!dragging) return;
                dragging = false;
                handleArea.ReleasePointer(pointerId);
                panel.RemoveFromClassList("sheet__panel--dragging");
                var tapped = offset < TapSlop;
                var farEnough = offset > panel.layout.height * DismissFraction || velocity > DismissVelocity;
                // Animates back to the stylesheet position: open, or off screen once closed.
                panel.style.translate = StyleKeyword.Null;
                if (tapped || farEnough) handle.Close();
            }

            handleArea.RegisterCallback<PointerUpEvent>(evt => End(evt.pointerId));
            handleArea.RegisterCallback<PointerCancelEvent>(evt => End(evt.pointerId));
        }
    }
}
