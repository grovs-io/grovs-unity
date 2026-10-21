# Grovs Unity sample project

A Unity 2022.3 project that uses the Grovs package straight from `../package` (a `file:` dependency). It is used to run the EditMode tests, to make device builds, and by CI. Its one scene, `Assets/Scenes/Smoke.unity`, calls a few Grovs APIs on start and logs the results.

The package's demo scene lives in `package/Samples~/Demo` and is not part of this project. Import it with Package Manager (Grovs > Samples > Demo) if you want it here.

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
| `GROVS_ANDROID_BACKEND` | `IL2CPP` or `Mono` (Mono builds 32-bit ARM only) |
| `GROVS_ANDROID_MINIFY` | `true` or `1` turns on release minify |

A Mono build makes EDM4U rewrite the ABI exclusions in `Assets/Plugins/Android/mainTemplate.gradle`. The next IL2CPP build writes them back. Do not commit that change.

Each value can also be passed on the Unity command line as `-NAME value`, for example `-GROVS_ANDROID_BACKEND Mono`. The environment variable wins when both are set. Never commit real keys.

## Local native SDK setup

Until native Grovs 3.1.0 is public, the sample builds against local checkouts of the native SDKs, kept next to this repo (`../grovs-iOS` and `../grovs-Android`).

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
