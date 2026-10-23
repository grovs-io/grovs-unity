using Net.Codecrete.QrCodeGenerator;
using UnityEngine;
using UnityEngine.UIElements;

namespace GrovsTestApp
{
    /// <summary>A QR code of a link, to open it on another phone.</summary>
    public static class QrSheet
    {
        const int Border = 1;

        public static OverlayHandle Show(string link)
        {
            var texture = Render(Encode(link));
            var content = Ui.Box("qr");
            content.Add(Ui.Text("Scan on another phone", "t-heading"));
            content.Add(Ui.Text("Tests install and deferred deep links", "t-caption"));
            var image = new Image { image = texture, scaleMode = ScaleMode.ScaleToFit };
            image.AddToClassList("qr__image");
            content.Add(image);
            content.Add(Ui.Text(link, "t-mono", "qr__link"));

            var native = NativeServices.Current;
            var buttons = Ui.Box("button-row");
            buttons.Add(new AppButton("Copy link", () => Copy(content, link), AppButtonStyle.Outline));
            var command = Application.platform == RuntimePlatform.IPhonePlayer ? "Copy simctl" : "Copy adb";
            buttons.Add(new AppButton(command, () => Copy(content, native.ShareCommand(link)), AppButtonStyle.Outline));
            content.Add(buttons);

            var handle = Sheet.Show(content);
            handle.Closed += () => Object.Destroy(texture);
            return handle;
        }

        /// <summary>The dark modules of the code, [x, y] with y going down.</summary>
        public static bool[,] Encode(string text)
        {
            var code = QrCode.EncodeText(text, QrCode.Ecc.Medium);
            var modules = new bool[code.Size, code.Size];
            for (var y = 0; y < code.Size; y++)
            for (var x = 0; x < code.Size; x++)
                modules[x, y] = code.GetModule(x, y);
            return modules;
        }

        /// <summary>One pixel per module, black on white, with a one module border. Point filtered so it scales sharp.</summary>
        public static Texture2D Render(bool[,] modules)
        {
            var size = modules.GetLength(0);
            var full = size + 2 * Border;
            var pixels = new Color32[full * full];
            var white = new Color32(255, 255, 255, 255);
            var black = new Color32(0, 0, 0, 255);
            for (var y = 0; y < full; y++)
            for (var x = 0; x < full; x++)
            {
                var mx = x - Border;
                var my = y - Border;
                var dark = mx >= 0 && my >= 0 && mx < size && my < size && modules[mx, my];
                // Texture rows start at the bottom.
                pixels[(full - 1 - y) * full + x] = dark ? black : white;
            }

            var texture = new Texture2D(full, full, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        static void Copy(VisualElement from, string text)
        {
            GUIUtility.systemCopyBuffer = text;
            Toast.Show(Overlay.HostFor(from), "Copied");
        }
    }
}
