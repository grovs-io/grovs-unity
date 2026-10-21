# Grovs for Unity

Deep links, attribution, event tracking, revenue tracking, push campaigns and in-app messages for Unity games on iOS and Android. A thin C# layer over the native Grovs iOS and Android SDKs.

- Unity 2022.3 or newer, iOS 13 or newer, Android target API 34 or newer.
- Needs [External Dependency Manager for Unity](https://github.com/googlesamples/unity-jar-resolver).
- Native SDKs: CocoaPods `Grovs ~> 3.1`, Maven `io.grovs:Grovs:[3.1.0,3.2)`.

## Quick start

1. Install EDM4U, then this package (git URL `https://github.com/grovs-io/grovs-unity.git#upm/v1.0.0`, OpenUPM `com.grovs.sdk`, or the `.unitypackage` from the GitHub release).
2. Open **Edit > Project Settings > Grovs** and enter the API key, URI scheme and link domains from the Grovs dashboard.
3. Listen for links:

```csharp
using GrovsSDK;

Grovs.DeeplinkReceived += deeplink => Debug.Log("Opened from " + deeplink.Link);
string link = await Grovs.GenerateLinkAsync(new GenerateLinkOptions { Title = "Join me" });
Grovs.Track("level_complete");
```

There is no init call: the native SDK starts before Unity, with the values from Project Settings.

See [Documentation~/index.md](Documentation~/index.md) for the full guide: settings, dashboard setup, every API, push, the iOS Notification Service Extension, platform differences and troubleshooting.
