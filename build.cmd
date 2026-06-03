@echo off
:: NUKE bootstrapper (Windows). Runs build/_build.csproj using the SDK pinned by global.json.
::   build.cmd Test
::   build.cmd Pack --configuration Release
dotnet run --project "%~dp0build\_build.csproj" --no-launch-profile -- %*
