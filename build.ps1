#!/usr/bin/env pwsh
# NUKE bootstrapper (PowerShell / cross-platform). Runs build/_build.csproj using the SDK pinned
# by global.json.
#   ./build.ps1 Test
#   ./build.ps1 Pack --configuration Release
$ErrorActionPreference = "Stop"
dotnet run --project "$PSScriptRoot/build/_build.csproj" --no-launch-profile -- @args
exit $LASTEXITCODE
