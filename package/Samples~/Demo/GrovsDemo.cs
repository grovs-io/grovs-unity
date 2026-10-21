using System;
using System.Collections.Generic;
using UnityEngine;

namespace GrovsSDK.Samples
{
    /// <summary>One button per Grovs feature. Drawn with IMGUI so the scene needs no UI package.</summary>
    public class GrovsDemo : MonoBehaviour
    {
        const float ReferenceWidth = 480f;
        const int MaxLogLines = 40;

        readonly List<string> _log = new List<string>();
        string _lastLink = "";
        string _userId = "demo-user";
        string _eventName = "demo_button";
        string _screenName = "DemoScreen";
        string _priceInCents = "199";
        Vector2 _scroll;

        void OnEnable()
        {
            Grovs.DeeplinkReceived += OnDeeplink;
            Grovs.ErrorOccurred += OnError;
            Grovs.InAppMessageEvent += OnInAppEvent;
        }

        void OnDisable()
        {
            Grovs.DeeplinkReceived -= OnDeeplink;
            Grovs.ErrorOccurred -= OnError;
            Grovs.InAppMessageEvent -= OnInAppEvent;
        }

        void Start()
        {
            Log("Grovs " + Grovs.SdkVersion + ", supported platform: " + Grovs.IsSupportedPlatform +
                ", enabled: " + Grovs.IsEnabled);
        }

        void OnDeeplink(GrovsDeeplink deeplink)
        {
            Log("Deep link: " + (deeplink.Link ?? "(none)") + ", from push: " + deeplink.FromPush +
                ", data: " + Describe(deeplink.Data));
        }

        void OnError(GrovsError error) => Log("Error: " + error);

        void OnInAppEvent(InAppMessageEvent e) => Log("In-app " + e.Type + " for campaign " + e.CampaignId);

        async void GenerateLink()
        {
            try
            {
                _lastLink = await Grovs.GenerateLinkAsync(new GenerateLinkOptions
                {
                    Title = "Join me in the game",
                    Subtitle = "Sent from the Grovs demo",
                    Data = new Dictionary<string, object> { { "level", 3 }, { "invitedBy", _userId } },
                    Tags = new List<string> { "demo" }
                });
                Log("Generated link: " + _lastLink);
            }
            catch (GrovsException e)
            {
                Log("Link failed: " + e.Error);
            }
        }

        void CopyLink()
        {
            if (string.IsNullOrEmpty(_lastLink))
            {
                Log("Generate a link first.");
                return;
            }
            GUIUtility.systemCopyBuffer = _lastLink;
            Log("Copied " + _lastLink);
        }

        void ShowLastDeeplink()
        {
            var deeplink = Grovs.LastDeeplink;
            Log(deeplink == null
                ? "No deep link yet."
                : "Last deep link: " + (deeplink.Link ?? "(none)") + ", data: " + Describe(deeplink.Data));
        }

        void TrackEvent()
        {
            Grovs.Track(_eventName, new Dictionary<string, object> { { "source", "demo" } }, new List<string> { "demo" });
            Log("Tracked event " + _eventName);
        }

        void TrackScreen()
        {
            Grovs.TrackScreenView(_screenName);
            Log("Tracked screen " + _screenName);
        }

        void SetUser()
        {
            Grovs.SetUserIdentifier(_userId);
            Grovs.SetUserAttributes(new Dictionary<string, object> { { "plan", "free" }, { "level", 3 } });
            Log("User id set to " + _userId + " with attributes plan and level");
        }

        void ToggleConsent()
        {
            Grovs.SetEnabled(!Grovs.IsEnabled);
            Log("Grovs enabled: " + Grovs.IsEnabled);
        }

        async void LogPurchase()
        {
            if (!int.TryParse(_priceInCents, out var cents))
            {
                Log("Price must be a whole number of cents.");
                return;
            }
            try
            {
                var logged = await Grovs.LogCustomPurchaseAsync(GrovsTransactionType.Buy, cents, "USD", "demo.coins");
                Log("Custom purchase logged: " + logged);
            }
            catch (GrovsException e)
            {
                Log("Purchase failed: " + e.Error);
            }
        }

        void OnGUI()
        {
            var scale = Screen.width / ReferenceWidth;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            var height = Screen.height / scale;

            GUILayout.BeginArea(new Rect(8, 8, ReferenceWidth - 16, height - 16));
            _scroll = GUILayout.BeginScrollView(_scroll);

            Section("Links");
            Row(("Generate link", GenerateLink), ("Copy link", CopyLink));
            GUILayout.Label(string.IsNullOrEmpty(_lastLink) ? "No link generated yet." : _lastLink);

            Section("Deep links");
            Row(("Show last deep link", ShowLastDeeplink));

            Section("Events");
            _eventName = Field("Event name", _eventName);
            _screenName = Field("Screen name", _screenName);
            Row(("Track event", TrackEvent), ("Track screen", TrackScreen));

            Section("Identity");
            _userId = Field("User id", _userId);
            Row(("Set user id and attributes", SetUser));

            Section("Consent");
            Row((Grovs.IsEnabled ? "Disable Grovs" : "Enable Grovs", ToggleConsent));

            Section("In-app messages");
            Row(("Pause", Grovs.PauseInAppMessages), ("Resume", Grovs.ResumeInAppMessages),
                ("Discard", () => Grovs.DiscardInAppMessages(true)));

            Section("Revenue");
            _priceInCents = Field("Price in cents", _priceInCents);
            Row(("Log custom purchase", LogPurchase));

            Section("Log");
            for (var i = _log.Count - 1; i >= 0; i--) GUILayout.Label(_log[i]);

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        static void Section(string title)
        {
            GUILayout.Space(8);
            GUILayout.Label("<b>" + title + "</b>", new GUIStyle(GUI.skin.label) { richText = true });
        }

        static string Field(string label, string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(120));
            value = GUILayout.TextField(value);
            GUILayout.EndHorizontal();
            return value;
        }

        static void Row(params (string label, Action action)[] buttons)
        {
            GUILayout.BeginHorizontal();
            foreach (var (label, action) in buttons)
                if (GUILayout.Button(label, GUILayout.Height(36))) action();
            GUILayout.EndHorizontal();
        }

        void Log(string line)
        {
            Debug.Log("[GrovsDemo] " + line);
            _log.Add(DateTime.Now.ToString("HH:mm:ss") + "  " + line);
            if (_log.Count > MaxLogLines) _log.RemoveAt(0);
        }

        static string Describe(IReadOnlyDictionary<string, object> data)
        {
            if (data == null || data.Count == 0) return "{}";
            var parts = new List<string>();
            foreach (var pair in data) parts.Add(pair.Key + "=" + pair.Value);
            return "{" + string.Join(", ", parts) + "}";
        }
    }
}
