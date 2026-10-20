#!/bin/bash
# Runs the EditMode tests on the sample project. Optional arg: test filter.
set -euo pipefail

if [ -z "${UNITY:-}" ]; then
  echo "UNITY is not set. Point it at the Unity editor binary, e.g." >&2
  echo "  export UNITY=/Applications/Unity/Hub/Editor/2022.3.62f3/Unity.app/Contents/MacOS/Unity" >&2
  exit 1
fi

cd "$(dirname "$0")/.."
mkdir -p build

"$UNITY" -batchmode -nographics -projectPath sample -runTests -testPlatform EditMode \
  -testResults "$PWD/build/editmode.xml" ${1:+-testFilter "$1"} -logFile -
