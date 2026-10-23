using UnityEngine.UIElements;

namespace GrovsTestApp
{
    /// <summary>A "?" next to a label that opens a short explanation.</summary>
    public class InfoButton : VisualElement
    {
        public InfoButton(string title, string body)
        {
            AddToClassList("info-button");
            var ring = Ui.Box("info-button__ring");
            ring.Add(Ui.Text("?", "info-button__mark"));
            Add(ring);
            Ui.TrackPress(this);
            // A "?" often sits inside a tappable row, which must not react to it.
            RegisterCallback<PointerDownEvent>(evt => evt.StopPropagation());
            RegisterCallback<ClickEvent>(evt =>
            {
                evt.StopPropagation();
                InfoDialog.Show(Overlay.HostFor(this), title, body);
            });
        }
    }

    public static class InfoDialog
    {
        public static OverlayHandle Show(string title, string body) => Show(Overlay.DefaultHost, title, body);

        public static OverlayHandle Show(VisualElement host, string title, string body)
        {
            var root = Ui.Box("dialog");
            var scrim = Ui.Box("dialog__scrim");
            var card = Ui.Box("dialog__card");
            card.Add(Ui.Text(title, "t-heading", "dialog__title"));
            card.Add(Ui.Text(body, "t-body", "dialog__body"));
            var ok = new TextAction("Got it");
            ok.AddToClassList("dialog__action");
            card.Add(ok);
            root.Add(scrim);
            root.Add(card);

            var handle = Overlay.Push(host, root, () => root.RemoveFromClassList("dialog--open"));
            ok.Clicked += handle.Close;
            scrim.RegisterCallback<ClickEvent>(_ => handle.Close());
            root.schedule.Execute(() => root.AddToClassList("dialog--open")).StartingIn(16);
            return handle;
        }
    }
}
