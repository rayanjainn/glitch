#!/usr/bin/env bash
# Runs the project's Unity EditMode and/or PlayMode tests headless (macOS/Linux).
# Usage: Tools/UnityTests/run-unity-tests.sh /path/to/Unity [EditMode|PlayMode|All]
#   macOS example: "/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/MacOS/Unity"
# Close the project in the Unity Editor first. Results go to TestResults/<Platform>.xml and .log.
set -uo pipefail
UNITY="${1:?path to the Unity executable required}"
PLATFORM="${2:-All}"
PROJECT="$(cd "$(dirname "$0")/../.." && pwd)"
OUT="$PROJECT/TestResults"
mkdir -p "$OUT"

if [ "$PLATFORM" = "All" ]; then PLATFORMS="EditMode PlayMode"; else PLATFORMS="$PLATFORM"; fi
WORST=0
for P in $PLATFORMS; do
  echo "Running $P tests..."
  "$UNITY" -batchmode -projectPath "$PROJECT" -runTests -testPlatform "$P" \
    -testResults "$OUT/$P.xml" -logFile "$OUT/$P.log"
  CODE=$?
  echo "$P exit code: $CODE  (results: $OUT/$P.xml, log: $OUT/$P.log)"
  [ "$CODE" -ne 0 ] && WORST=$CODE
done
exit $WORST
