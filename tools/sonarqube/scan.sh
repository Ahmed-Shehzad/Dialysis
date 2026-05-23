#!/usr/bin/env bash
# Run SonarScanner for .NET against the local Aspire-hosted SonarQube.
#
# Prerequisites:
#   • Aspire AppHost is running (`dotnet run --project src/aspire/Dialysis.AppHost`)
#   • The sonarqube + postgres-sonarqube + sonarqube-bootstrap containers all
#     reported healthy in the Aspire dashboard.
#   • dotnet-sonarscanner installed:
#       dotnet tool install --global dotnet-sonarscanner
#   • dotnet-coverage installed (for code-coverage collection):
#       dotnet tool install --global dotnet-coverage
#
# Usage:
#   tools/sonarqube/scan.sh                # interactive: pulls token from the bootstrap volume
#   SONAR_TOKEN=xxx tools/sonarqube/scan.sh  # CI / scripted use

set -euo pipefail

SONAR_URL="${SONAR_URL:-http://localhost:9000}"
SONAR_PROJECT_KEY="${SONAR_PROJECT_KEY:-dialysis}"
BOOTSTRAP_VOLUME="${BOOTSTRAP_VOLUME:-dialysis-sonarqube-bootstrap}"

# If SONAR_TOKEN isn't set in the env, read it from the bootstrap volume that
# the AppHost's sonarqube-bootstrap container populated on first boot. The
# trick: mount the volume into a throwaway alpine container and cat the file.
if [ -z "${SONAR_TOKEN:-}" ]; then
  echo "SONAR_TOKEN not set; reading from $BOOTSTRAP_VOLUME ..."
  SONAR_TOKEN=$(docker run --rm -v "${BOOTSTRAP_VOLUME}:/state" alpine cat /state/scanner-token.txt 2>/dev/null || true)
  if [ -z "$SONAR_TOKEN" ]; then
    echo "ERROR: no token in $BOOTSTRAP_VOLUME. Is the AppHost running?" >&2
    exit 1
  fi
fi

REPO_ROOT="$(git rev-parse --show-toplevel)"
cd "$REPO_ROOT"

COVERAGE_OUT="$REPO_ROOT/artifacts/coverage/sonarqube.xml"
mkdir -p "$(dirname "$COVERAGE_OUT")"

echo "→ sonarscanner begin (project: $SONAR_PROJECT_KEY, host: $SONAR_URL)"
dotnet sonarscanner begin \
  /k:"$SONAR_PROJECT_KEY" \
  /n:"Dialysis Modular Monolith" \
  /d:sonar.host.url="$SONAR_URL" \
  /d:sonar.token="$SONAR_TOKEN" \
  /d:sonar.cs.vscoveragexml.reportsPaths="$COVERAGE_OUT" \
  /d:sonar.scanner.scanAll=false \
  /d:sonar.exclusions="**/bin/**,**/obj/**,**/node_modules/**,**/dist/**,**/Migrations/**"

echo "→ dotnet build Dialysis.slnx"
dotnet build Dialysis.slnx --configuration Release --no-incremental

echo "→ dotnet-coverage collect (running test suite)"
dotnet-coverage collect \
  "dotnet test Dialysis.slnx --configuration Release --no-build \
    --filter 'FullyQualifiedName!~ClamAvAttachmentBlobScannerTests&FullyQualifiedName!~AzureBlobAttachmentBlobStoreTests&FullyQualifiedName!~S3AttachmentBlobStoreTests'" \
  -f xml -o "$COVERAGE_OUT"

echo "→ sonarscanner end"
dotnet sonarscanner end /d:sonar.token="$SONAR_TOKEN"

echo
echo "Analysis complete. Open $SONAR_URL/dashboard?id=$SONAR_PROJECT_KEY"
