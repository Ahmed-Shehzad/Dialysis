#!/usr/bin/env bash
# NUKE bootstrapper (Unix). Runs the strongly-typed build pipeline in build/_build.csproj using
# the SDK pinned by global.json. Pass a target and parameters, e.g.:
#   ./build.sh Test
#   ./build.sh Pack --configuration Release
#   ./build.sh --help
set -eo pipefail
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
dotnet run --project "${SCRIPT_DIR}/build/_build.csproj" --no-launch-profile -- "$@"
