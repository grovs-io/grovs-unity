# Grovs for Unity

Deep links, attribution, event tracking, revenue tracking, push campaigns and in-app messages for Unity games on iOS and Android, powered by [Grovs](https://grovs.io). The package is a thin C# layer over the native Grovs [iOS](https://github.com/grovs-io/grovs-iOS) and [Android](https://github.com/grovs-io/grovs-Android) SDKs.

## Requirements

- Unity 2022.3 or newer (Unity 6 included), Mono or IL2CPP.
- iOS 13 or newer. Android target API 34 or newer.
- [External Dependency Manager for Unity (EDM4U)](https://github.com/googlesamples/unity-jar-resolver), which pulls in the native SDKs.

In the Editor and on other platforms every call goes to a safe stub, so your game still compiles and runs everywhere.

## Install

Install EDM4U first, then add Grovs in one of these ways:

- **Git URL.** In Package Manager choose **Add package from git URL** and enter:
  ```
  https://github.com/grovs-io/grovs-unity.git#upm/1.0.0
  ```
- **OpenUPM.**
  ```
  openupm add com.grovs.sdk
  ```
- **.unitypackage.** Download `Grovs-X.Y.Z.unitypackage` from the [releases](https://github.com/grovs-io/grovs-unity/releases) and import it with **Assets > Import Package > Custom Package**.

For the git URL, always use a `upm/X.Y.Z` tag: only those tags hold the prebuilt Android libraries.

In **Assets > External Dependency Manager > iOS Resolver > Settings**, turn on **Add pods to Unity framework target**.

## Set up

1. **Dashboard.** Add your app in the [Grovs dashboard](https://grovs.io): the iOS bundle id and Apple team id, and the Android package name with the SHA-256 fingerprints of every key that signs your app (debug, upload and Play App Signing).
2. **Project Settings.** Open **Edit > Project Settings > Grovs** and enter:
   - **API key** from the dashboard.
   - **URI scheme** from the dashboard. The build fails without it.
   - **Link domains**: your production and test link hosts (for example `x.sqd.link` and `x.test-sqd.link`) and any custom domain.

That is all. There is no init call: the native SDK starts before Unity, so links that open the app are never missed. At build time Grovs writes the `Info.plist` keys, URL types and associated domains on iOS, and the meta-data and intent filters on Android.

## Use it

Everything is on the static class `Grovs` in the `GrovsSDK` namespace. Call it from the main thread. Every task, callback and event comes back on the main thread.

### Receive deep links

```csharp
using GrovsSDK;

void OnEnable() => Grovs.DeeplinkReceived += OnDeeplink;
void OnDisable() => Grovs.DeeplinkReceived -= OnDeeplink;

void OnDeeplink(GrovsDeeplink deeplink)
{
    if (deeplink.Data.TryGetValue("level", out var level))
        LoadLevel((long)level);
}
```

Links that arrive before anyone subscribes are kept and delivered to the first subscriber, so subscribing in your first scene is enough. This covers app opens, deferred links after a fresh install, and push taps (`deeplink.FromPush`).

### Create links

```csharp
string link = await Grovs.GenerateLinkAsync(new GenerateLinkOptions
{
    Title = "Join my team",
    Subtitle = "Level 3 is waiting",
    Data = new Dictionary<string, object> { { "level", 3 }, { "inviter", "player-42" } },
    TrackingCampaign = "spring"
});
```

Every async call also has a callback version, for example `Grovs.GenerateLink(options, (link, error) => { })`.

### Track players and events

```csharp
Grovs.SetUserIdentifier("player-42");
Grovs.SetUserAttributes(new Dictionary<string, object> { { "plan", "pro" } });

Grovs.Track("level_complete", new Dictionary<string, object> { { "level", 3 } });
Grovs.TrackScreenView("Shop");
```

Turn on **Track scenes** in Project Settings to report every scene change as a screen view.

### Track revenue

```csharp
// With Unity IAP 4 or 5 installed:
_ = GrovsUnityIAP.LogPurchaseAsync(args.purchasedProduct);

// Or a purchase outside the stores:
await Grovs.LogCustomPurchaseAsync(GrovsTransactionType.Buy, 199, "USD", "coins_100");
```

### In-app messages

Messages from your dashboard campaigns show on their own. You can react to them and choose when they appear:

```csharp
Grovs.InAppMessageEvent += e => Debug.Log(e.Type + " " + e.CampaignId);

Grovs.PauseInAppMessages();    // for example during a match
Grovs.ResumeInAppMessages();
Grovs.SetInAppExcludedScreens(new List<string> { "Gameplay" });
```

### Consent

To keep Grovs off until the player agrees, turn off **Start enabled** in Project Settings, then call `Grovs.SetEnabled(true)` when they do. The choice is saved on the device.

### Push notifications

Add the APNs key and the Firebase service account in the dashboard, then turn on **Push enabled** in Project Settings. Grovs works next to the Firebase Unity SDK and Unity Mobile Notifications, or on its own with your `google-services.json` on Android.

## Test before you ship

- **In the Editor:** open **Grovs > Simulate Deep Link**, enter Play Mode and send a link with JSON data. `DeeplinkReceived` fires just like on a device.
- **On a device:** import the **Test app** sample from the Grovs entry in Package Manager. It has a screen for every feature and a log of every call.

## Documentation

The [full guide](package/Documentation~/index.md) covers every setting and API in detail:

- [Project Settings](package/Documentation~/index.md#project-settings)
- [Dashboard setup](package/Documentation~/index.md#dashboard-setup)
- [Using the API](package/Documentation~/index.md#using-the-api)
- [Consent](package/Documentation~/index.md#consent)
- [Push notifications](package/Documentation~/index.md#push-notifications) and the [iOS Notification Service Extension](package/Documentation~/index.md#ios-notification-service-extension)
- [Platform differences](package/Documentation~/index.md#platform-differences)
- [Troubleshooting](package/Documentation~/index.md#troubleshooting)

## Support

Questions or problems: open an [issue](https://github.com/grovs-io/grovs-unity/issues) or write to [support@grovs.io](mailto:support@grovs.io).

## License

MIT. See [LICENSE](LICENSE).
