#!/usr/bin/env bash
# Auto-fix the mechanically-fixable analyzer issues across the solution and clear
# the stale .sonarqube/ artifact that produces phantom warnings.
#
# Honest scope: NO tool can "resolve all SonarQube issues" — Sonar rules without a
# Roslyn code-fix provider (cognitive complexity, design smells, security hotspots)
# need human judgement. This script applies every fix that *is* automatable and
# leaves the build green so the remaining issues stand out for manual triage.
#
# What it does:
#   1. Removes .sonarqube/. A scanner run leaves this behind; the globally-installed
#      SonarQube.Integration.ImportBefore.targets MSBuild hook then injects the
#      server's Sonar-cs.ruleset into EVERY `dotnet build`, overriding .editorconfig
#      and snapping every tuned-down rule back to `warning` — the "hundreds of
#      phantom issues" source. Clearing it keeps builds honoring .editorconfig.
#   2. `dotnet format` (whitespace + style + analyzers) over Dialysis.slnx, applying
#      every code-fix provider the active analyzer set offers, honoring .editorconfig
#      severities (tuned-down rules are left alone, by design).
#   3. Re-builds Release to assert TreatWarningsAsErrors still passes (no half-fix).
#
# Severity scoping matters here. The default is `error`, so the only *analyzer*
# fixes applied are the ones that actually break the build/CI. (The whitespace +
# import-ordering pass is always on — it is not severity-gated — so a default run
# still normalizes formatting/`using` order to .editorconfig; expect that diff, not
# a literal no-op.) Widen with `--severity warn` (or `info`) to also sweep the
# editorconfig style rules — note that `dotnet build` does NOT execute the
# name-simplification IDE analyzers (IDE0001-3 etc.), so `--severity warn` can
# rewrite hundreds of files even though the build is green. Preview with `--check`.
#
# SonarAnalyzer.CSharp is declared in Directory.Packages.props but not referenced,
# so it does NOT run in a normal build — Sonar Sxxxx diagnostics come only from the
# server scanner. `--with-sonar` temporarily references it solution-wide so its
# code-fix providers run during the format pass, then restores Directory.Build.props.
#
# Usage:
#   tools/sonarqube/fix.sh                  # clean + format (error-level) + verify build
#   tools/sonarqube/fix.sh --severity warn  # widen to the editorconfig style sweep (big diff)
#   tools/sonarqube/fix.sh --with-sonar     # also apply SonarAnalyzer.CSharp code fixes
#   tools/sonarqube/fix.sh --check          # report-only: fail if anything would change (CI gate)
#   tools/sonarqube/fix.sh --no-verify      # skip the Release verify build (faster)

set -euo pipefail

SOLUTION="Dialysis.slnx"
SEVERITY="error"
MODE="fix"            # fix | check
WITH_SONAR=0
VERIFY=1

while [ $# -gt 0 ]; do
  case "$1" in
    --check)        MODE="check" ;;
    --with-sonar)   WITH_SONAR=1 ;;
    --no-verify)    VERIFY=0 ;;
    --severity)     shift; SEVERITY="${1:?--severity needs a value}" ;;
    --severity=*)   SEVERITY="${1#*=}" ;;
    -h|--help)      sed -n '2,40p' "$0"; exit 0 ;;
    *) echo "Unknown argument: $1 (try --help)" >&2; exit 2 ;;
  esac
  shift
done

REPO_ROOT="$(git rev-parse --show-toplevel)"
cd "$REPO_ROOT"

BUILD_PROPS="$REPO_ROOT/Directory.Build.props"
PROPS_BACKUP=""

# Restore Directory.Build.props (if we patched it) and always clear .sonarqube/
# on exit — success, failure, or Ctrl-C.
cleanup() {
  if [ -n "$PROPS_BACKUP" ] && [ -f "$PROPS_BACKUP" ]; then
    mv -f "$PROPS_BACKUP" "$BUILD_PROPS"
  fi
  rm -rf "$REPO_ROOT/.sonarqube"
}
trap cleanup EXIT

echo "→ clearing stale .sonarqube/ (phantom-warning source)"
rm -rf "$REPO_ROOT/.sonarqube"

if [ "$WITH_SONAR" = 1 ]; then
  echo "→ temporarily referencing SonarAnalyzer.CSharp solution-wide"
  PROPS_BACKUP="$(mktemp)"
  cp "$BUILD_PROPS" "$PROPS_BACKUP"
  # Insert the analyzer reference right after the threading-analyzer one so it
  # lands inside the existing ItemGroup. Version comes from CPM (Directory.Packages.props).
  awk '
    { print }
    /Microsoft.VisualStudio.Threading.Analyzers/ && !done {
      print "    <PackageReference Include=\"SonarAnalyzer.CSharp\" PrivateAssets=\"all\" />"
      done = 1
    }
  ' "$PROPS_BACKUP" > "$BUILD_PROPS"
fi

if [ "$MODE" = "check" ]; then
  echo "→ dotnet format --verify-no-changes (severity: $SEVERITY)"
  dotnet format "$SOLUTION" --verify-no-changes --severity "$SEVERITY"
  echo
  echo "✓ No auto-fixable issues outstanding."
  exit 0
fi

echo "→ dotnet format (severity: $SEVERITY)"
dotnet format "$SOLUTION" --severity "$SEVERITY"

# Drop the temporary Sonar reference before verifying, so the verify build matches
# what CI actually enforces. The trap is a safety net for the failure path.
if [ -n "$PROPS_BACKUP" ] && [ -f "$PROPS_BACKUP" ]; then
  mv -f "$PROPS_BACKUP" "$BUILD_PROPS"
  PROPS_BACKUP=""
fi

if [ "$VERIFY" = 1 ]; then
  echo "→ verify: dotnet build $SOLUTION -c Release --no-incremental"
  dotnet build "$SOLUTION" --configuration Release --no-incremental
fi

echo
echo "✓ Auto-fixes applied and the build is green."
echo "  Review the diff: git diff --stat"
echo "  Remaining Sonar issues with no automated fix live on the server —"
echo "  run tools/sonarqube/scan.sh and open the dashboard to triage them."
