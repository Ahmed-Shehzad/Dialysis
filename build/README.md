# Build automation (NUKE)

A strongly-typed, locally-runnable build pipeline for the Dialysis platform, built with
[NUKE](https://nuke.build). It drives the same `dotnet` steps the GitHub workflows run, but as a
single C# program you can execute and debug like any other .NET project.

## Running

From the repository root (the SDK is pinned by `global.json`):

```bash
./build.sh <Target> [parameters]     # Linux / macOS
build.cmd  <Target> [parameters]     # Windows
./build.ps1 <Target> [parameters]    # PowerShell, cross-platform
```

The default target is **Compile**. `./build.sh --help` lists every target and parameter.

## Targets

| Target      | Depends on | What it does |
|-------------|------------|--------------|
| `Clean`     | —          | Deletes all `bin`/`obj` under `src` + `tests` and empties `artifacts/`. |
| `Restore`   | —          | `dotnet restore Dialysis.slnx`. |
| `Compile`   | Restore    | `dotnet build Dialysis.slnx` (warnings are errors, per `Directory.Build.props`). |
| `Test`      | Compile    | Runs the whole test suite; TRX logs land in `artifacts/test-results`. |
| `Pack`      | Compile    | Packs the projects marked `<IsPackable>true</IsPackable>` into `artifacts/packages` (with symbol `.snupkg`s). |
| `Publish`   | Pack       | Pushes the packed `.nupkg`s to a NuGet feed (requires `--nuget-api-key`). |

## Parameters

| Parameter          | Default                              | Notes |
|--------------------|--------------------------------------|-------|
| `--configuration`  | `Debug` locally, `Release` on CI     | `Debug` or `Release`. |
| `--version`        | project-defined                      | Overrides the package version emitted by `Pack` (e.g. `1.4.0-preview.1`). |
| `--nuget-source`   | `https://api.nuget.org/v3/index.json`| Feed `Publish` pushes to. |
| `--nuget-api-key`  | —                                    | Secret; required by `Publish`. |

## Examples

```bash
./build.sh Test                                  # build + run the suite
./build.sh Pack --configuration Release          # produce NuGet packages
./build.sh Pack --version 1.4.0-preview.1        # versioned packages
./build.sh Publish --nuget-api-key "$NUGET_KEY"  # pack + push to nuget.org
```

## Design notes

- **`.slnx` solution.** Every step passes the `Dialysis.slnx` path straight to the dotnet CLI,
  which understands the XML solution format natively. The build never relies on a third-party
  `.slnx` parser.
- **`Pack` scope.** Only projects that opt in with `<IsPackable>true</IsPackable>` are packed —
  packing the whole solution would also pack internal host/app libraries, some of which depend on
  prerelease packages (`NU5104`).
- **Isolation.** `build/_build.csproj` is *not* part of `Dialysis.slnx`. It opts out of central
  package management and warnings-as-errors (via the empty `build/Directory.Build.props` /
  `.targets` stubs) so the build tool can pin `Nuke.Common` independently of the product's
  package graph.
- **CI.** `.github/workflows/nuke-ci.yml` runs `Pack` and uploads the packages as an artifact —
  the packaging path the plain-dotnet workflows don't cover. The existing per-module and
  solution workflows continue to own the build + test gates.
