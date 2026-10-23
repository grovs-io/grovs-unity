using UnityEngine.UIElements;

namespace GrovsTestApp
{
    /// <summary>A short message at the bottom of the screen for two seconds.</summary>
    public static class Toast
    {
        const long VisibleMs = 2000;
        const float BottomOffset = 80;
        const string ToastName = "toast";

        public static void Show(string text) => Show(Overlay.DefaultHost, text);

        public static void Show(VisualElement host, string text)
        {
            var layer = Overlay.Layer(host);
            layer.Q(ToastName)?.RemoveFromHierarchy();

            var toast = Ui.Box("toast");
            toast.name = ToastName;
            toast.pickingMode = PickingMode.Ignore;
            toast.style.bottom = BottomOffset + Overlay.SafeBottom(host);
            var bubble = Ui.Box("toast__bubble");
            bubble.pickingMode = PickingMode.Ignore;
            bubble.Add(Ui.Text(text, "t-body", "toast__text"));
            toast.Add(bubble);
            layer.Add(toast);

            toast.schedule.Execute(() => toast.AddToClassList("toast--visible")).StartingIn(16);
            toast.schedule.Execute(() => toast.RemoveFromClassList("toast--visible")).StartingIn(VisibleMs);
            toast.schedule.Execute(toast.RemoveFromHierarchy).StartingIn(VisibleMs + Overlay.AnimationMs);
        }
    }
}
