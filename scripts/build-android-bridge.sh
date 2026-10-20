#!/usr/bin/env bash
# Builds the Android bridge AARs and copies them into the Unity package.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
ANDROID_DIR="$ROOT/native/android"
OUT_DIR="$ROOT/package/Runtime/Plugins/Android"

if [ -z "${JAVA_HOME:-}" ] && [ -x /usr/libexec/java_home ]; then
  JAVA_HOME="$(/usr/libexec/java_home -v 17)"
  export JAVA_HOME
fi

tasks=(:bridge:assembleRelease)
if [ -d "$ANDROID_DIR/firebase" ]; then
  tasks+=(:firebase:assembleRelease)
fi

(cd "$ANDROID_DIR" && ./gradlew "${tasks[@]}")

mkdir -p "$OUT_DIR"

copy_aar() {
  local source="$1" target="$2"
  if [ -f "$source" ]; then
    cp "$source" "$OUT_DIR/$target"
    echo "Copied $target"
  fi
}

copy_aar "$ANDROID_DIR/bridge/build/outputs/aar/bridge-release.aar" grovs-unity-bridge.aar
copy_aar "$ANDROID_DIR/firebase/build/outputs/aar/firebase-release.aar" grovs-unity-firebase.aar
