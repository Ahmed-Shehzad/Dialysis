# SonarQube tooling

Helper scripts for the Aspire-hosted SonarQube server and for keeping the local
tree analyzer-clean. Static analysis for this repo is owned by the SonarQube
server that the AppHost auto-starts in dev (port `9000`); these scripts drive it
and clean up after it.

| Script | Purpose |
|---|---|
| [`bootstrap.sh`](bootstrap.sh) | First-boot provisioning of the local SonarQube (admin-credential rotation, the `dialysis` project, scanner token). Invoked by the AppHost's `sonarqube-bootstrap` container — you rarely run it by hand. |
| [`scan.sh`](scan.sh) | Runs a **server-side analysis**: `sonarscanner begin` → build → coverage → `sonarscanner end`, uploading results to the dashboard. |
| [`fix.sh`](fix.sh) | **Auto-fixes** the mechanically-fixable analyzer issues locally and clears the stale `.sonarqube/` artifact that causes phantom warnings. |

---

## `fix.sh` — auto-fix what's automatable

> **Honest scope:** no tool can "resolve all SonarQube issues." Rules without a
> Roslyn code-fix provider (cognitive complexity, design smells, security
> hotspots) need human judgement. `fix.sh` applies every fix that *is* automatable
> and leaves the build green so the rest stand out for manual triage.

### What it does

1. **Clears `.sonarqube/`.** A prior scanner run leaves this directory behind; the
   globally-installed `SonarQube.Integration.ImportBefore.targets` MSBuild hook then
   injects the server's `Sonar-cs.ruleset` into **every** `dotnet build`, overriding
   `.editorconfig` and snapping every tuned-down rule back to `warning`. That is the
   source of the "hundreds of phantom issues" — clearing it makes builds honor
   `.editorconfig` again. (`scan.sh` cleans this up too, via an `EXIT` trap.)
2. **Runs `dotnet format`** (whitespace + style + analyzers) over `Dialysis.slnx`,
   applying every code-fix provider the active analyzer set offers, honoring the
   `.editorconfig` severities.
3. **Re-builds in Release** to assert `TreatWarningsAsErrors` still passes, so you
   never end up with a half-applied fix.

### Run it

```bash
# From anywhere in the repo. No running AppHost/SonarQube server needed —
# fix.sh works purely off the local analyzers + .editorconfig.
tools/sonarqube/fix.sh
```

That default is intentionally **conservative** on the *analyzer* front: only
`error`-severity diagnostics (the ones that actually break the build/CI) are fixed.
The whitespace + `using`-ordering pass is always on (it is not severity-gated), so
a default run still normalizes formatting/import order to `.editorconfig` — expect
that diff plus the `.sonarqube/` cleanup and a green verify build, not a literal
zero-change no-op. Preview exactly what it would touch with `--check`.

### Flags

| Flag | Effect |
|---|---|
| *(none)* | Clean + `dotnet format` at **`error`** severity + Release verify build. |
| `--check` | **Report-only.** Runs `dotnet format --verify-no-changes`; exits non-zero if anything *would* change. Makes no edits — use it as a preview or a CI gate. |
| `--severity warn` (or `info`) | **Widen the fix.** Also sweeps the `.editorconfig` style rules. ⚠️ `dotnet build` does **not** execute the name-simplification IDE analyzers (`IDE0001`–`IDE0003`, …), so this can rewrite **hundreds of files** even though the build is green. Preview with `--check` first. |
| `--with-sonar` | Temporarily inserts a `SonarAnalyzer.CSharp` reference solution-wide so its code-fix providers also run, then restores `Directory.Build.props`. (Largely redundant today: `Directory.Build.props` already references the analyzer solution-wide, so its fixes run in every pass; the flag remains for checkouts without that reference.) |
| `--no-verify` | Skip the Release verify build (faster; skips the safety check). |
| `-h`, `--help` | Print the header/usage. |

### Recommended workflow

```bash
# 1. Preview the widened style sweep without touching anything:
tools/sonarqube/fix.sh --check --severity warn

# 2. If the diff looks right, apply it:
tools/sonarqube/fix.sh --severity warn

# 3. Review before committing (this repo auto-stages on commit):
git diff --stat
```

> **⚠️ This workspace auto-stages and auto-pushes on commit.** A widened
> (`--severity warn`) run can produce a large diff — always `git diff --stat`
> and stage deliberately before committing so an unintended reformat doesn't ship.

### After running

Anything `fix.sh` can't auto-fix lives on the SonarQube server. Run an analysis and
triage the rest in the dashboard:

```bash
tools/sonarqube/scan.sh
# → open http://localhost:9000/dashboard?id=dialysis
```

---

## `scan.sh` — server-side analysis

Uploads a full analysis (issues + coverage) to the local SonarQube. Prerequisites:

- The **Aspire AppHost is running** (`dotnet run --project src/aspire/Dialysis.AppHost`)
  and the `sonarqube` + `postgres-sonarqube` + `sonarqube-bootstrap` containers are
  healthy in the dashboard.
- `dotnet-sonarscanner` and `dotnet-coverage` installed as global tools:
  ```bash
  dotnet tool install --global dotnet-sonarscanner
  dotnet tool install --global dotnet-coverage
  ```

```bash
tools/sonarqube/scan.sh                  # reads the scanner token from the bootstrap volume
SONAR_TOKEN=xxx tools/sonarqube/scan.sh  # CI / scripted use
```

It tears down its own `.sonarqube/` working dir on exit (success, failure, or
Ctrl-C) so the next `dotnet build` keeps honoring `.editorconfig`.
