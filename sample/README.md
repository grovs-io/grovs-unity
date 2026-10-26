# Grovs Unity sample project

A Unity 2022.3 project that uses the Grovs package straight from `../package` (a `file:` dependency). It is used to run the EditMode tests, to make device builds, and by CI.

## Test app

The project's scenes are the package's test app sample. `Assets/TestApp` is a link to `package/Samples~/TestApp`, so edits here change the sample that ships. On Windows, run `git config --global core.symlinks true` and turn on Developer Mode (or run Git as admin) before you clone, or the link is checked out as a plain file. The build has two scenes: `TestApp`, then `ExcludedScreen`.

The app has four tabs and a settings screen:

- **Home**: SDK status, where the app was opened from, quick actions and the user identity.
- **Links**: generate a link, show it as a QR code, open it with `HandleUrl` and inspect it.
- **Events**: events, screen views, purchases, global tags and screen aliases.
- **In-app**: show, pause, resume and discard in-app messages, the excluded screens and a jump to the `ExcludedScreen` scene.
- **Settings**: the config baked into the build (including "Start with SDK enabled", read only) and developer settings. The "SDK enabled" switch is on Home.

Every action writes an entry to the log. To build it with your own Grovs project:

```
export GROVS_PROD_KEY=... GROVS_TEST_KEY=... GROVS_URI_SCHEME=... GROVS_LINK_DOMAINS=... GROVS_CLIPBOARD_DOMAINS=...
export GROVS_BASE_URL=https://api.example.com
"$UNITY" -batchmode -quit -projectPath sample -buildTarget Android -executeMethod GrovsSampleBuild.BuildAndroid
```

## Run the tests

```
export UNITY=/Applications/Unity/Hub/Editor/2022.3.62f3/Unity.app/Contents/MacOS/Unity
scripts/run-unity-tests.sh            # all EditMode tests
scripts/run-unity-tests.sh GrovsJson  # only tests whose name matches
```

Results go to `build/editmode.xml`.

## Build

The entry points are in `Assets/Editor/GrovsSampleBuild.cs`:

```
"$UNITY" -batchmode -quit -projectPath sample -buildTarget Android -executeMethod GrovsSampleBuild.BuildAndroid
"$UNITY" -batchmode -quit -projectPath sample -buildTarget iOS -executeMethod GrovsSampleBuild.BuildIOS
"$UNITY" -batchmode -quit -projectPath sample -buildTarget iOS -executeMethod GrovsSampleBuild.BuildIOSSimulator
```

Android builds go to `build/android/sample.apk`, iOS exports to `build/ios`. Build the Android bridge first with `scripts/build-android-bridge.sh`: its `.aar` files are not in git.

### Environment variables

The committed `Assets/Grovs/GrovsSettings.asset` holds placeholders only. Real values come from environment variables. When set, they change the settings for that one build and are never written to the asset:

| Variable | Used for |
|---|---|
| `GROVS_TEST_KEY` | API key when the build uses the test environment |
| `GROVS_PROD_KEY` | API key when the build uses the production environment (the batch builds are release builds, so with the default "Test for development builds" this is the one used) |
| `GROVS_URI_SCHEME` | URI scheme |
| `GROVS_LINK_DOMAINS` | Link domains, as a comma list, for example `x.sqd.link,x.test-sqd.link` |
| `GROVS_BASE_URL` | SDK server, for a backend other than production |
| `GROVS_CLIPBOARD_DOMAINS` | Clipboard link domains, as a comma list |
| `GROVS_ANDROID_BACKEND` | `IL2CPP` or `Mono` (Mono builds 32-bit ARM only) |
| `GROVS_ANDROID_MINIFY` | `true` or `1` turns on release minify |

A Mono build makes EDM4U rewrite the ABI exclusions in `Assets/Plugins/Android/mainTemplate.gradle`. The next IL2CPP build writes them back. Do not commit that change.

Each value can also be passed on the Unity command line as `-NAME value`, for example `-GROVS_ANDROID_BACKEND Mono`. The environment variable wins when both are set. Never commit real keys.

## Local native SDK setup

The sample can build against local checkouts of the native SDKs ([grovs-iOS](https://github.com/grovs-io/grovs-iOS) and [grovs-Android](https://github.com/grovs-io/grovs-Android)), kept next to this repo.

**iOS.** Set `GROVS_IOS_POD_PATH` to your `grovs-iOS` checkout before an iOS build:

```
export GROVS_IOS_POD_PATH="$(cd ../grovs-iOS && pwd)"   # run from the repo root
```

Use an absolute path: Unity resolves a relative one against its own working folder.

`Assets/Editor/GrovsDevOverrides.cs` then rewrites the `pod 'Grovs'` line of the generated Podfile to `pod 'Grovs', :path => '...'`. Without the variable, the Podfile is left alone.

**Android.** Publish the native SDK to your local Maven repository:

```
cd ../grovs-Android/Grovs
./gradlew :Grovs:publishMavenPublicationToMavenLocal -x :Grovs:signMavenPublication
```

This publishes the `maven` publication, whose pom lists the runtime dependencies. Skipping the signing task means no signing key is needed.

`Assets/Editor/GrovsDevDependencies.xml` adds that repository to the build. It uses an absolute path, `file:///path/to/.m2/repository`. **Edit it to your own home folder** (`file:///Users/<you>/.m2/repository`), and do not commit that change.

The same file pins `io.grovs:Grovs:3.1.0` instead of the package's `[3.1.0,3.2)` range. A local Maven repository has no `maven-metadata.xml`, so Gradle cannot match a version range against it. The file also declares the native SDK's runtime dependencies by hand, so it still works with a pom published from the `release` publication, which lists none.
