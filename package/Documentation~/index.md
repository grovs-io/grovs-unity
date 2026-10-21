# Grovs for Unity

Grovs adds deep links, attribution, event tracking, revenue tracking, push campaigns and in-app messages to Unity games on iOS and Android. The package is a thin C# layer over the native Grovs iOS and Android SDKs.

- Unity 2022.3 or newer (Unity 6 included).
- iOS 13 or newer. Android target API 34 or newer.
- Mono and IL2CPP, with or without Android minify.
- In the Editor and on other platforms every call goes to a stub: it logs once, drops events, fails link generation with `UnsupportedPlatform` and resolves purchases to false.

## Contents

1. [Install](#install)
2. [EDM4U settings](#edm4u-settings)
3. [Project Settings](#project-settings)
4. [Dashboard setup](#dashboard-setup)
5. [Using the API](#using-the-api)
6. [Consent](#consent)
7. [Push notifications](#push-notifications)
8. [iOS Notification Service Extension](#ios-notification-service-extension)
9. [Platform differences](#platform-differences)
10. [Demo sample](#demo-sample)
11. [Troubleshooting](#troubleshooting)

## Install

Grovs needs **External Dependency Manager for Unity (EDM4U)** to pull in the native SDKs. Install it first, from [OpenUPM](https://openupm.com/packages/com.google.external-dependency-manager/) or from [its GitHub releases](https://github.com/googlesamples/unity-jar-resolver). When EDM4U is missing, Grovs logs a warning once per Editor session.

The native SDK versions are pinned by the package:

- CocoaPods: `Grovs ~> 3.1`
- Maven: `io.grovs:Grovs:[3.1.0,3.2)`

Pick one of these ways to add Grovs:

**Git URL.** In Package Manager choose **Add package from git URL** and enter the release tag of the `upm` branch:

```
https://github.com/grovs-io/grovs-unity.git#upm/v1.0.0
```

Or add it to `Packages/manifest.json`:

```json
"com.grovs.sdk": "https://github.com/grovs-io/grovs-unity.git#upm/v1.0.0"
```

Use the `upm/vX.Y.Z` tags, not the main branch: only the `upm` branch holds the prebuilt Android bridge libraries.

**OpenUPM.**

```
openupm add com.grovs.sdk
```

**.unitypackage.** Download `Grovs-X.Y.Z.unitypackage` from the GitHub release and import it with **Assets > Import Package > Custom Package**.

## EDM4U settings

Open **Assets > External Dependency Manager > iOS Resolver > Settings** and check:

- **Add pods to Unity framework target**: on. The Grovs bridge lives in `UnityFramework`, so the pod must be linked there.
- **Link frameworks statically**: on (the default).

On Android, run **Assets > External Dependency Manager > Android Resolver > Resolve** after installing, or keep auto-resolution on. Grovs works with Unity's custom Gradle templates (`mainTemplate.gradle`, `gradleTemplate.properties`), which EDM4U writes the dependencies into.

## Project Settings

Open **Edit > Project Settings > Grovs**. The values are stored in `Assets/Grovs/GrovsSettings.asset` and are only read at build time, never at runtime. Commit this asset with your project.

| Setting | Default | What it does |
|---|---|---|
| API key | empty | The key from the Grovs dashboard. An empty key gives a build warning, and the app then fails to authenticate. |
| Environment | Test for development builds | Test for development builds and Production for the rest, or always Production, or always Test. |
| Base URL | empty | A custom API base URL. Leave it empty for the default. |
| Start enabled | on | Whether Grovs runs before the player has made a consent choice. A choice saved on the device wins. See [Consent](#consent). |
| Clipboard domains | empty | Domains whose links are read from the clipboard on first open, for deferred deep links. |
| URI scheme | empty | The URI scheme from the dashboard. **Required**: the build fails when it is empty, because iOS authentication fails without it. |
| Link domains | empty | Your link hosts. List both the production host (`x.sqd.link`) and the test host (`x.test-sqd.link`), plus any custom domain. They become iOS `applinks:` entries and Android `https` intent filters with `autoVerify`. |
| Push enabled | off | Turns on Grovs push notifications. See [Push notifications](#push-notifications). |
| Android push mode | Auto | Auto, Firebase Unity or Standalone. See [Push on Android](#push-on-android). |
| iOS own notification delegate | on | Grovs installs a forwarding notification-center delegate so it sees push taps. |
| Android small icon | empty | Drawable resource name for the small notification icon. |
| Google services JSON path | empty | Android Standalone push only: the path to your `google-services.json`. |
| Track scenes | off | Reports each active scene change as a screen view, with the scene name. |
| Log level | Error | The native SDK log level: Info, Warn or Error. |

What the build does with these values:

- **iOS**: writes the Grovs keys into `Info.plist`, adds a URL type for the scheme, adds `applinks:` associated domains to the entitlements, and sets the Swift build settings on `UnityFramework`.
- **Android**: generates `Assets/Plugins/Android/GrovsConfig.androidlib` with the meta-data (you can gitignore it, it is rebuilt on every build), and adds the scheme and link intent filters to your launcher activity, whatever its class name.
- **Android target API**: the build fails if the target API level is below 34.

## Dashboard setup

Set up your app in the Grovs dashboard the same way as a native app.

**iOS**

- The bundle id and your Apple team id.
- For push campaigns: an APNs `.p8` key.
- For revenue: the App Store IAP API access key.

**Android**

- The package name.
- The SHA-256 fingerprints of every key that signs your app. Register all of these:
  - The Unity debug keystore, used for builds without a custom keystore. Get it with:
    ```
    keytool -list -v -keystore ~/.android/debug.keystore -alias androiddebugkey -storepass android
    ```
  - Your upload keystore (the one set in **Player Settings > Publishing Settings**).
  - The Play App Signing key, from **Play Console > Setup > App signing**. Apps installed from Google Play are signed with this key, not your upload key, so App Links fail without it.
- For push campaigns: the Firebase service account.
- For revenue: the Play API access key.

The dashboard serves the `apple-app-site-association` and `assetlinks.json` files from these values. Copy the URI scheme and link domains it shows into Project Settings.

## Using the API

Everything lives in the `GrovsSDK` namespace, on the static class `Grovs`.

```csharp
using GrovsSDK;
```

There is no init call. The native SDK starts before Unity does, using the build-time settings, so links that open the app are never missed.

**Threads.** Call the API from the Unity main thread. Every `Task` completes, and every callback and event runs, on the main thread. The one exception is `ShouldShowInAppMessage` (see [In-app messages](#in-app-messages)). Pending tasks have no timeout.

**Tasks or callbacks.** Each async call has a `Task` version and a callback version. A failed task throws `GrovsException`, which holds a `GrovsError`. The callback version passes the error instead.

### State and errors

```csharp
Debug.Log(Grovs.SdkVersion);           // the Unity package version
Debug.Log(Grovs.IsSupportedPlatform);  // true on iOS and Android players

Grovs.SetLogLevel(GrovsLogLevel.Info);

Grovs.ErrorOccurred += error => Debug.LogWarning("Grovs: " + error.Code + " " + error.Message);
```

`GrovsError.Code` is one of: `AuthenticationFailed`, `NetworkRequestFailed`, `EventDispatchFailed`, `LinkGenerationFailed`, `InAppDisplayFailed`, `LinkDetailsFailed`, `NotificationsError`, `SdkNotInitialized`, `SdkDisabled`, `InvalidArgument`, `UnsupportedPlatform`, `Unknown`.

### Deep links

```csharp
void OnEnable() => Grovs.DeeplinkReceived += OnDeeplink;
void OnDisable() => Grovs.DeeplinkReceived -= OnDeeplink;

void OnDeeplink(GrovsDeeplink deeplink)
{
    // Data is never null. Whole numbers arrive as long on both platforms.
    if (deeplink.Data.TryGetValue("level", out var level))
        LoadLevel((long)level);

    Debug.Log("Link: " + deeplink.Link + ", from push: " + deeplink.FromPush);
    if (deeplink.Tracking != null) Debug.Log("Campaign: " + deeplink.Tracking.Campaign);
}
```

- Links that arrive before anyone subscribes are kept (up to 20) and delivered in order to the first subscriber. Subscribe early, for example in your first scene.
- `Grovs.LastDeeplink` holds the most recent link, or null.
- `FromPush` is true when the link came from a Grovs push notification.

To open a link tapped inside your own game UI:

```csharp
bool handled = Grovs.HandleUrl("https://x.sqd.link/abc");
```

The URL must be absolute with a scheme and a host, otherwise it throws `ArgumentException`. It returns false when Grovs is disabled.

### Generating links

```csharp
try
{
    string link = await Grovs.GenerateLinkAsync(new GenerateLinkOptions
    {
        Title = "Join my team",
        Subtitle = "Level 3 is waiting",
        ImageUrl = "https://example.com/share.png",
        Data = new Dictionary<string, object> { { "level", 3 }, { "inviter", "player-42" } },
        Tags = new List<string> { "invite" },
        AndroidRedirect = new GrovsRedirect { Url = "https://example.com/android", OpenAppIfInstalled = true },
        TrackingCampaign = "spring",
        TrackingSource = "in_game",
        TrackingMedium = "share_button"
    });
    GUIUtility.systemCopyBuffer = link;
}
catch (GrovsException e)
{
    Debug.LogWarning("Link failed: " + e.Error);
}
```

The callback version:

```csharp
Grovs.GenerateLink(options, (link, error) =>
{
    if (error != null) Debug.LogWarning(error);
    else Debug.Log(link);
});
```

Other options: `IosRedirect`, `DesktopRedirect`, `ShowPreviewIos`, `ShowPreviewAndroid`, and `CopyToClipboardIos` / `CopyToClipboardAndroid` (null uses the project setting). `Data` values must be JSON-serializable (strings, numbers, booleans, null, lists and string-keyed dictionaries of these), otherwise the call throws `ArgumentException`.

To read the details of a link:

```csharp
IReadOnlyDictionary<string, object> details = await Grovs.GetLinkDetailsAsync("/abc");
// or
Grovs.GetLinkDetails("/abc", (details, error) => { /* ... */ });
```

### Identity

```csharp
Grovs.SetUserIdentifier("player-42");   // null clears it
Grovs.SetUserAttributes(new Dictionary<string, object> { { "plan", "pro" }, { "level", 12 } });
```

`Grovs.SetPushToken(token)` is only needed when your game gets push tokens itself and Grovs cannot see them.

### Events and screens

```csharp
Grovs.Track("level_complete",
    new Dictionary<string, object> { { "level", 3 }, { "stars", 2 } },
    new List<string> { "progress" });

Grovs.TrackScreenView("Shop");

Grovs.SetGlobalTags(new List<string> { "beta" });   // null clears them
Grovs.SetScreenAliases(new Dictionary<string, string> { { "MainMenu", "Home" } });
```

Grovs logs a warning (and still sends the call) when an event breaks the native rules:

- Reserved names: `view`, `open`, `install`, `reinstall`, `app_open`, `time_spent`, `reactivation`, `user_referred`, `custom`, `screen_view`, `push_open`, `push_delivered`, `push_held`, `in_app_viewed`, `in_app_clicked`, `in_app_dismissed`, `in_app_answered`, `in_app_commented`, `in_app_held`, `journey_entered`, `journey_held`, `journey_path`.
- Properties up to 8192 bytes of JSON, at most 20 tags, each up to 255 characters.

To report scenes automatically, turn on **Track scenes** in Project Settings. Each active scene change is then sent as a screen view with the scene name, and screen aliases still apply.

### In-app messages

```csharp
Grovs.InAppMessageEvent += e =>
{
    switch (e.Type)
    {
        case InAppEventType.Clicked:
            if (e.Payload != null && e.Payload.TryGetValue("reward", out var reward)) GiveReward(reward);
            break;
        case InAppEventType.Dismissed:
            Debug.Log("Dismissed: " + e.DismissReason);
            break;
        case InAppEventType.Answered:
            Debug.Log("Answer: " + e.AnswerValue + " " + e.Comment);
            break;
        case InAppEventType.Failed:
            Debug.LogWarning(e.FailureMessage);
            break;
    }
};

Grovs.PauseInAppMessages();     // for example during a boss fight
Grovs.ResumeInAppMessages();
Grovs.DiscardInAppMessages(dismissCurrent: true);
Grovs.SetInAppExcludedScreens(new List<string> { "Gameplay" });
Grovs.SetInAppDebugLogging(true);
```

Event fields: `Type` (Shown, Clicked, Dismissed, Answered, Failed), `CampaignId`, `ButtonId` (null when the body was tapped), `Payload` (for key_value actions), `DismissReason` (`close_button`, `backdrop`, `swipe`, `app` or `orientation`), `AnswerValue` (a `long` for stars or a `string` option id), `Comment` and `FailureMessage`.

To hold a message back, set a veto. Return false to skip it:

```csharp
// Read a plain field here, set from your game code.
static volatile bool inMatch;

Grovs.ShouldShowInAppMessage = campaignId => !inMatch;
```

**Important:** `ShouldShowInAppMessage` is called synchronously on a native thread. On iOS that is the main thread. On Android it is the Android UI thread, which is **not** Unity's thread. Keep it fast and do not call Unity APIs from it. Read a field you update from game code instead. Null (the default) shows every message.

### Revenue

Log a purchase from the store yourself:

```csharp
// iOS: the StoreKit transaction id. Android: the Play Purchase.originalJson.
bool logged = await Grovs.LogInAppPurchaseAsync(storeTransaction);
```

Or a purchase that did not go through a store:

```csharp
bool logged = await Grovs.LogCustomPurchaseAsync(GrovsTransactionType.Buy, 199, "USD", "coins_100");
// GrovsTransactionType: Buy, Cancel, Refund. The optional last argument is the date (null means now).
```

**Unity IAP helper.** When `com.unity.purchasing` 4.0 or newer is installed, the `GrovsSDK.IAP` assembly compiles and adds `GrovsUnityIAP`. It reads the Unity IAP receipt and supports the Apple stores and Google Play. Other stores resolve false with a warning.

IAP 4, or the IAP 5 `Product` path:

```csharp
public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
{
    _ = GrovsUnityIAP.LogPurchaseAsync(args.purchasedProduct);
    return PurchaseProcessingResult.Complete;
}
```

IAP 5 `Order` path (needs `com.unity.purchasing` 5.0 or newer):

```csharp
void OnPurchasePending(PendingOrder order)
{
    _ = GrovsUnityIAP.LogOrderAsync(order);
    storeController.ConfirmPurchase(order);
}
```

- On IAP 5 on iOS, log **before** you confirm a consumable. After confirming, the receipt can be null.
- A null product or order throws `ArgumentNullException`.
- The task resolves false, with a warning, when the receipt cannot be used.

### Push helpers

Most games do not need these, because Grovs sees push taps on its own (see [Push notifications](#push-notifications)). They are for taps that reach your code another way, for example from Unity Mobile Notifications' last responded notification:

```csharp
if (Grovs.IsGrovsNotification(notificationData))   // IDictionary<string, string>
    Grovs.HandleNotificationOpen(payload);          // IDictionary<string, object>
```

### Testing links in the Editor

Open **Grovs > Simulate Deep Link**, enter Play Mode, type a link and a JSON object for the data, and press **Send**. `DeeplinkReceived` fires exactly as it would for a real link. This is the way to test your routing code without a device.

## Consent

Grovs can stay off until the player agrees.

1. Turn off **Start enabled** in Project Settings. Grovs then does nothing on first launch.
2. When the player agrees, call:
   ```csharp
   Grovs.SetEnabled(true);
   ```
   Grovs starts, and the link that opened the app (if any) is delivered to `DeeplinkReceived`.
3. To withdraw consent, call `Grovs.SetEnabled(false)`.

The choice is saved on the device (iOS UserDefaults key `grovs_unity_enabled`, Android SharedPreferences `grovs_unity`, key `enabled`) and used on every later launch, before Unity starts. A saved choice always wins over **Start enabled**. Read the current state with `Grovs.IsEnabled`.

While disabled, `HandleUrl` returns false and async calls fail with `SdkDisabled`.

## Push notifications

Push campaigns need the APNs `.p8` key (iOS) and the Firebase service account (Android) on the dashboard. Then turn on **Push enabled** in Project Settings.

When push is on, the Android manifest gets `android.permission.POST_NOTIFICATIONS`, needed on Android 13 and newer.

### Push on iOS

- The build adds the `aps-environment` entitlement (development for development builds, production otherwise) and the `remote-notification` background mode, and sets `UNITY_USES_REMOTE_NOTIFICATIONS=1` so Unity forwards the push token.
- Grovs picks up the device token on its own, whoever calls `registerForRemoteNotifications` (your game, Firebase or Unity Mobile Notifications).
- With **iOS own notification delegate** on (the default), Grovs installs a forwarding `UNUserNotificationCenterDelegate` at launch. A delegate set later, for example by Firebase or Unity Mobile Notifications, still gets every call. Grovs only looks at taps on its own notifications.
- With the setting off, Grovs installs nothing. Call `Grovs.HandleNotificationOpen(data)` yourself when a notification is tapped.
- Turning push off later does not remove the entitlement, background mode or `UNITY_USES_REMOTE_NOTIFICATIONS` from an Xcode project built with **Append**. Use **Replace**, or remove them by hand.

### Push on Android

Android delivers Firebase messages to only one service, so Grovs has three modes:

- **Auto** (default): Firebase Unity mode when the `Firebase.Messaging` assembly is in the project, Standalone otherwise.
- **Firebase Unity**: for games that use the Firebase Unity SDK. Grovs adds a small extra library with `io.grovs.unity.GrovsFirebaseUnityService`, which extends Firebase Unity's `ListenerService` and replaces it in the manifest. It hands Grovs messages to Grovs and then always passes every message on, so your `FirebaseMessaging.MessageReceived` still fires. Tested with Firebase Unity 12.10.1. Forcing this mode without Firebase Messaging installed fails the build.
- **Standalone**: for games without Firebase Unity. Set **Google services JSON path** to your `google-services.json`. The build turns it into Android resources and adds `com.google.firebase:firebase-messaging` through EDM4U (in the generated file `Assets/Grovs/Editor/GrovsPushDependencies.xml`). The build warns when the path is empty, because Firebase then does not start.

In both modes a tap opens your launcher activity, and the link arrives in `DeeplinkReceived` with `FromPush = true`.

## iOS Notification Service Extension

Rich push images need a Notification Service Extension. Grovs does not add it for you yet. After each **Replace** build of the Xcode project (Append keeps it):

1. In Xcode choose **File > New > Target > Notification Service Extension**. Name it, for example, `GrovsNotificationService`, and pick your team. Set its deployment target to iOS 13 or newer.
2. Replace the generated `NotificationService.swift` with:
   ```swift
   import GrovsNotificationService

   class NotificationService: GrovsNotificationServiceExtension {
   }
   ```
   Make sure the target's `NSExtensionPrincipalClass` in its `Info.plist` is `$(PRODUCT_MODULE_NAME).NotificationService` (the Xcode template already sets this).
3. Add the pod to that target in the generated `Podfile`:
   ```ruby
   target 'GrovsNotificationService' do
     pod 'GrovsNotificationService', '~> 3.1'
   end
   ```
4. Run `pod install` in the Xcode project folder and open `Unity-iPhone.xcworkspace`.
5. Give the extension the same bundle id prefix as the app (for example `com.company.game.GrovsNotificationService`) and a provisioning profile.

The extension downloads and attaches the image of each Grovs push. If you need your own extension logic too, subclass `GrovsNotificationServiceExtension` and call `super.didReceive(...)`, or call `GrovsNotificationContent.attachImage(to:session:completion:)` from your own extension.

## Platform differences

| Area | iOS | Android |
|---|---|---|
| `ErrorOccurred` | All async SDK errors | Only in-app failures and bridge errors |
| `LogInAppPurchaseAsync` result | The native result | `true` once the call is handed over |
| `GenerateLink` while authentication is retrying | Waits until authenticated | Fails right away |
| Log level `Warn` | Warn | Info (Android has no warn level) |
| In-app veto thread | Main thread (Unity's thread) | Android UI thread (not Unity's) |
| Clipboard deferred link | The iOS paste prompt may show on first launch | Needs window focus. Works, because Grovs starts before the activity |
| In-app message over the game | Shown in a window above Unity's | Shown in a dialog over the Unity activity. Unity may get `OnApplicationFocus(false)` while it is shown |

## Demo sample

In Package Manager, select Grovs, open **Samples** and import **Demo**. Open the `GrovsDemo` scene, set your keys in Project Settings and build to a device. The scene has one button per feature: generate and copy a link, show the last deep link, track an event and a screen, set the user id and attributes, toggle consent, pause, resume and discard in-app messages, and log a custom purchase. It draws its UI with IMGUI, so it needs no UI package.

## Troubleshooting

**"Grovs needs External Dependency Manager for Unity"** in the Console: install EDM4U (see [Install](#install)).

**Build fails with "URI scheme is empty"**: set **URI scheme** in Project Settings to the value from the dashboard.

**Build warns "API key is empty"**: set **API key**. The app builds but cannot authenticate.

**Build fails with "Android target API level must be 34 or higher"**: set **Player Settings > Android > Target API Level** to 34 or newer (or Automatic with SDK 34 installed).

**Build fails with "Android push mode is Firebase Unity but Firebase Messaging is not installed"**: install Firebase Messaging, or set the mode to Auto or Standalone.

**Android build fails while dexing (D8 or R8 errors) on Unity 2022.3**: Unity 2022.3 uses Android Gradle plugin 7.4.2, whose built-in R8 cannot process the Kotlin 1.9 and AndroidX libraries Grovs uses. Grovs patches the generated root `build.gradle` with a newer R8 (`com.android.tools:r8:8.5.35`). If your custom base Gradle template sets the plugin version through a variable, Grovs cannot read it and logs a warning instead. Then add this to the `buildscript` dependencies of your root template yourself:

```groovy
classpath 'com.android.tools:r8:8.5.35'
```

**Android: links open the browser instead of the app**: check that the SHA-256 of the key that signed this build is on the dashboard (see [Dashboard setup](#dashboard-setup)). Play Store installs need the Play App Signing key.

**iOS: "No such module 'Grovs'" or link errors**: check the [EDM4U settings](#edm4u-settings), then open `Unity-iPhone.xcworkspace` (not the `.xcodeproj`) and run `pod install` again if needed.

**iOS: links do not open the app**: check the link domains in Project Settings, the associated domains capability on your App ID, and the team id on the dashboard.

**No deep link in my handler**: subscribe to `DeeplinkReceived` early. Links that arrived before you subscribed are delivered to the first subscriber. Check that Grovs is enabled (`Grovs.IsEnabled`).

**A Task never completes**: pending tasks have no timeout. Make sure you call Grovs from the main thread and that the game is not paused in a way that stops the player loop.

**Push opens are not attributed on Android with Firebase Unity**: check the build log for the chosen push mode and that the `Firebase.Messaging` assembly is present.
