using System;
using System.Collections.Generic;
using UnityEngine;

namespace GrovsTestApp
{
    /// <summary>What each less obvious control does, in plain words, for people new to Grovs.</summary>
    public static class Help
    {
        static readonly Dictionary<string, (string title, string body)> Shared = new Dictionary<string, (string, string)>
        {
            // Home
            ["sdkEnabled"] = ("SDK enabled",
                "The Grovs SDK is the library this app uses to talk to Grovs. This switch is the user's consent (Grovs.SetEnabled). When off, the SDK stays installed but sends nothing to Grovs: no links, events or push data."),
            ["openedFrom"] = ("Opened from",
                "Grovs links open your app when it is installed, and the SDK hands the link and its data to the app. The app gets it as an event (DeeplinkReceived) and can also read it later (LastDeeplink). This shows the last link that opened the app, and \"missed\" means one of the two never had it."),
            ["identity"] = ("Identity",
                "Grovs groups links opened, events and purchases by user. The identifier is your own user id, for example from your login (Grovs.SetUserIdentifier). Attributes are facts about the user, like plan or country, shown on their profile in the dashboard."),
            ["quickActions"] = ("Quick actions",
                "Shortcuts to the most common tests. One tap runs the test with the values you last used on its tab. Hold a button to change the values first."),

            // Links
            ["inspect"] = ("Inspect a link",
                "Every Grovs link is stored on the Grovs backend with its title, data and settings. Paste a link to load what is stored for it (Grovs.GetLinkDetailsAsync). Useful for checking a link made here or in the dashboard."),
            ["linkData"] = ("Data",
                "Custom values saved inside the link, like a promo code or the id of a product. When someone opens the link, your app gets this data back and can show the right screen. Tap the type badge to send a value as text, number, true/false or date."),
            ["linkTags"] = ("Tags",
                "Labels for organizing links in the Grovs dashboard, for example by campaign or channel. They do not change how the link behaves."),
            ["utm"] = ("UTM tracking",
                "UTM parameters are the standard way to name where traffic comes from: source (like newsletter), medium (like email) and campaign. Grovs uses them to show which campaign brought each open and install."),
            ["customRedirects"] = ("Custom redirects",
                "When someone opens a link without the app installed, Grovs sends them somewhere: by default the store or website set in your project. Turn this on to choose a different place per platform for this one link."),
            ["openAppIfInstalled"] = ("Open app if installed",
                "On: if the app is installed, the link opens it, and the URL above is only used when it is not. Off: the link always opens the URL above."),
            ["platformOverrides"] = ("Platform overrides",
                "Per-link versions of two project settings. Preview shows a page with the link's title and image before redirecting. Clipboard copies the link on that page, so after installing, the app can still find which link brought the user. \"Default\" keeps the project's setting."),

            // Events
            ["customEvent"] = ("Custom event",
                "Events are actions you report from your app, like signup or level_complete (Grovs.Track). They appear in the dashboard and can trigger in-app messages. Some names are used by Grovs itself and rejected; \"Send anyway\" checks that."),
            ["eventPropertiesAndTags"] = ("Properties and tags",
                "Properties are details sent with this one event, like level = 3. Tags are labels for filtering events in the dashboard, up to 20 per event."),
            ["screenView"] = ("Screen view",
                "Grovs records which screens people visit, for analytics and for in-app message rules. Most screens are reported on their own (see Auto track screens in Settings). This reports one by hand (Grovs.TrackScreenView). Hold Track to add properties."),
            ["globalTags"] = ("Global tags",
                "Tags added to every event this app sends from now on, like beta or region_eu (Grovs.SetGlobalTags). Handy for splitting all events by a group of users."),
            ["screenAliases"] = ("Screen aliases",
                "Automatically tracked screens are named after their scene, like TestApp. An alias gives a screen a readable name in the dashboard. The key is the scene name, the value is the name to show."),
            ["purchase"] = ("Purchase",
                "Grovs ties purchases to the link or campaign that brought the user, so you can see revenue per link. Use this for payments you handle yourself (Grovs.LogCustomPurchaseAsync). Buy, cancel and refund are the types; the amount is sent in cents."),
            ["startDate"] = ("Start date",
                "When the purchase happened. Choose an earlier date to test a purchase that is reported late."),

            // In-app
            ["showMessage"] = ("Show a message",
                "In-app messages are pop-ups you design in the Grovs dashboard. Each campaign has a trigger: an event name. This sends that event, and a message shows if a live campaign uses it as its trigger."),
            ["queue"] = ("Queue",
                "Messages wait in a queue until a good moment to show. Discard waiting drops queued messages. Close current also closes the one on screen. Both pause messages; switch to Running to resume."),
            ["blockAll"] = ("Block all messages",
                "Before showing a message, the SDK asks the app whether it may (Grovs.ShouldShowInAppMessage). On means this app always answers no, and the message is logged as blocked. Not saved across launches."),
            ["inAppDebug"] = ("Debug logging",
                "Makes the SDK explain its in-app message decisions in the device log (Logcat on Android, the Xcode console on iOS): why a message was or was not shown."),
            ["excluded"] = ("Excluded screens",
                "Screens where in-app messages never appear, like checkout. Use the scene name, like ExcludedScreen. Messages wait until the user leaves that screen."),

            // Settings
            ["apiKey"] = ("API key",
                "Identifies your Grovs project, so the SDK sends data to the right place. Copy it from your project in the Grovs dashboard. Changes apply after a rebuild."),
            ["environment"] = ("Environment",
                "Each Grovs project has a test environment and a live (Prod) one, with separate data. Use Test while building so you don't mix test data with real users. Use the API key that matches."),
            ["baseUrl"] = ("Base URL",
                "The server the SDK talks to. Leave it empty for Grovs' own servers. Set it only to test against another backend, like a staging server; it must start with http:// or https://."),
            ["clipboardDomains"] = ("Clipboard domains",
                "Deferred deep linking means the app finds the link that brought a user even after a fresh install. One way is through the clipboard. If your links use your own domain, list it here so the SDK trusts it. grovs.link and sqd.link always work."),
            ["autoTrack"] = ("Auto track screens",
                "When on, the SDK reports a screen view each time a scene loads, with no extra code. This is set in Grovs settings before the build."),
            ["startEnabled"] = ("Start with SDK enabled",
                "Whether the user's consent starts as given on each launch. Off starts the SDK without sending anything until you turn it on from Home, like an app waiting for a consent popup."),
            ["debugLevel"] = ("Debug level",
                "How much the SDK writes to the device log (Logcat on Android, the Xcode console on iOS). Info shows every step, Error only failures."),
            ["callStyle"] = ("Call style",
                "The SDK offers each link function in two forms: async (await) and classic callbacks. Pick which one the tests use; both should give the same result."),

            // Logs
            ["linkSources"] = ("Link sources",
                "The SDK hands an opened link to the app as an event (Grovs.DeeplinkReceived) and keeps it in Grovs.LastDeeplink. Every link should reach both."),
        };

        const string PushTokenBody =
            "Firebase gives every install a token that push notifications are sent to. The SDK passes it to Grovs so you can reach this phone from a dashboard campaign. Copy it to send a test push to just this phone.";

        static readonly Dictionary<string, (string title, string body)> Android = new Dictionary<string, (string, string)>
        {
            ["pushToken"] = ("Push token", PushTokenBody),
            ["storeReceipt"] = ("Google Play purchase JSON",
                "For purchases made with Google Play Billing. Paste the purchase's originalJson and the SDK sends it to Grovs to log the purchase (Grovs.LogInAppPurchaseAsync)."),
        };

        static readonly Dictionary<string, (string title, string body)> Ios = new Dictionary<string, (string, string)>
        {
            ["pushToken"] = ("Push token", PushTokenBody + " Not available on iOS in this test app."),
            ["storeReceipt"] = ("App Store transaction id",
                "For purchases made with the App Store. Paste the transaction id and the SDK sends it to Grovs to log the purchase (Grovs.LogInAppPurchaseAsync)."),
        };

        public static IReadOnlyList<string> Keys { get; } = new List<string>(Shared.Keys) { "pushToken", "storeReceipt" };

        public static (string title, string body) Get(string key) => Get(key, Application.platform == RuntimePlatform.IPhonePlayer);

        public static (string title, string body) Get(string key, bool ios)
        {
            if (key != null && (ios ? Ios : Android).TryGetValue(key, out var platformText)) return platformText;
            if (key != null && Shared.TryGetValue(key, out var text)) return text;
            throw new ArgumentException("No help text for \"" + key + "\"", nameof(key));
        }
    }
}
