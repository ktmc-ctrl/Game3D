#!/usr/bin/env bash
# Development Editor build of SengokuLife via UnrealBuildTool (macOS).
# This is the normal (non Live Coding) build that T001-A requires.
#
#   UE_ROOT=/Users/Shared/Epic\ Games/UE_5.8 tools/build_sengokulife_mac.sh
#
# UE_ROOT defaults to the launcher install path for 5.8.
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROJECT="$REPO_ROOT/SengokuLife/SengokuLife.uproject"
UE_ROOT="${UE_ROOT:-/Users/Shared/Epic Games/UE_5.8}"
BUILD_SH="$UE_ROOT/Engine/Build/BatchFiles/Mac/Build.sh"

if [[ ! -x "$BUILD_SH" ]]; then
  echo "Build.sh not found at: $BUILD_SH (set UE_ROOT to your UE 5.8.2 install)" >&2
  exit 1
fi

"$BUILD_SH" SengokuLifeEditor Mac Development -Project="$PROJECT" -WaitMutex -NoHotReload "$@"
