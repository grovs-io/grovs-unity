# Grovs for Unity

Deep links, attribution, event tracking, revenue tracking, push campaigns and in-app messages for Unity games on iOS and Android. The package is a thin C# layer over the native [Grovs iOS](https://github.com/grovs-io/grovs-iOS) and [Grovs Android](https://github.com/grovs-io/grovs-Android) SDKs.

- Unity 2022.3 or newer (Unity 6 included).
- iOS 13 or newer. Android target API 34 or newer.
- Mono and IL2CPP.

## Install

Grovs needs [External Dependency Manager for Unity (EDM4U)](https://github.com/googlesamples/unity-jar-resolver) to pull in the native SDKs. Install it first, then add Grovs in one of these ways:

- **Git URL.** In Package Manager choose **Add package from git URL** and enter:
  ```
  https://github.com/grovs-io/grovs-unity.git#upm/1.0.0
  ```
- **OpenUPM.**
  ```
  openupm add com.grovs.sdk
  ```
- **.unitypackage.** Download `Grovs-X.Y.Z.unitypackage` from the [releases](https://github.com/grovs-io/grovs-unity/releases) and import it with **Assets > Import Package > Custom Package**.

Always install from a `upm/X.Y.Z` tag, not from the main branch: only the `upm` branch holds the prebuilt Android libraries.

## Quick start

1. Open **Edit > Project Settings > Grovs** and enter the API key, URI scheme and link domains from the Grovs dashboard.
2. Use the API:

```csharp
using GrovsSDK;

Grovs.DeeplinkReceived += deeplink => Debug.Log("Opened from " + deeplink.Link);
string link = await Grovs.GenerateLinkAsync(new GenerateLinkOptions { Title = "Join me" });
Grovs.Track("level_complete");
```

There is no init call: the native SDK starts before Unity, with the values from Project Settings.

The [full guide](package/Documentation~/index.md) covers every setting, dashboard setup, the whole API, consent, push notifications, platform differences and troubleshooting.

## Test app

The package ships a test app sample with a screen for every feature and a log of every call. Import it from the Grovs entry in Package Manager, under **Samples**.

## Repository layout

| Folder | What it holds |
|---|---|
| `package/` | The Unity package: C# runtime, Editor build tools, iOS bridge (Swift), tests, docs and the test app sample. |
| `native/android/` | Source of the Android bridge, built into the `.aar` files that ship in the package. |
| `native/ios/` | Tests for the iOS bridge, run through `Package.swift`. |
| `sample/` | A Unity project that uses `package/` directly. Used for tests, device builds and CI. See [sample/README.md](sample/README.md). |
| `scripts/` | Build and test helpers. |

## Development

Build the Android bridge (needs JDK 17). The `.aar` files are not kept in git:

```
scripts/build-android-bridge.sh
```

Run the Unity EditMode tests:

```
export UNITY=/Applications/Unity/Hub/Editor/2022.3.62f3/Unity.app/Contents/MacOS/Unity
scripts/run-unity-tests.sh
```

Run the native bridge tests:

```
(cd native/android && ./gradlew :bridge:testDebugUnitTest :firebase:testDebugUnitTest)
xcodebuild test -scheme GrovsUnityBridge-Package -destination "platform=iOS Simulator,name=iPhone 17"
```

Use any iPhone simulator listed by `xcrun simctl list devices available`.

## Releasing

1. Set the new `version` in `package/package.json` and add an entry to `package/CHANGELOG.md`.
2. Push a tag with the same version, without a `v`:
   ```
   git tag 1.0.1
   git push origin 1.0.1
   ```

The release workflow then checks the native SDKs are published, builds the Android libraries, publishes the `upm` branch with a `upm/1.0.1` tag, and creates a GitHub release with `Grovs-1.0.1.unitypackage`. It stops if the tag does not match `package.json`.

## License

MIT. See [LICENSE](LICENSE).
