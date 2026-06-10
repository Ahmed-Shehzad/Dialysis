#!/usr/bin/env bash
# Drift gate for the frontend duplication convention.
#
# The seven SPAs under src/frontend/ deliberately share NO npm package; instead a
# small set of cross-cutting files is duplicated byte-for-byte across the apps that
# need them (see CLAUDE.md "Frontend (per-module SPAs)"). This script holds the
# canonical list of those files and fails when any copy diverges, so "keep the
# copies in sync by hand" is mechanically enforced in CI (frontend.yml) instead of
# by code review alone.
#
# Manifest format below: "relative/path" (compared across every app that contains
# the file) or "relative/path|app1,app2" (compared only across that explicit
# subset — used for the patients helpers, where hie-web and patient-portal-web
# intentionally diverge; those copies carry an "Intentionally diverges" header
# comment and are excluded here).
#
# No dependencies beyond bash + coreutils (md5sum, diff).

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
FRONTEND="$ROOT/src/frontend"
APPS=(his-web ehr-web pdms-web smartconnect-web hie-web identity-web patient-portal-web)

MANIFEST=(
  "eslint.config.js"
  "tsconfig.json"
  "src/shared/lazyPage.ts"
  "src/shared/ErrorBoundary.tsx"
  "src/shared/ErrorBoundary.test.tsx"
  "src/features/theme/ThemeProvider.tsx"
  "src/features/auth/api/authApi.ts"
  "src/features/auth/components/AuthProvider.tsx"
  "src/features/durable-commands/api/durableCommandsApi.ts"
  "src/features/durable-commands/components/DurableCommandProgress.tsx"
  "src/features/durable-commands/components/ToastHost.tsx"
  "src/features/durable-commands/components/toastBus.ts"
  "src/features/durable-commands/hooks/useDurableCommand.ts"
  "src/features/durable-commands/index.ts"
  # Patients helpers: aligned subset only. hie-web (own patientDirectoryApi backing
  # module) and patient-portal-web (per-id fetch, no batch loader) intentionally
  # diverge — see the header comments in those files.
  "src/features/patients/PatientLabel.tsx|ehr-web,pdms-web,identity-web,hie-web"
  "src/features/patients/patientLoader.ts|ehr-web,pdms-web,identity-web"
  "src/features/patients/patientLoader.test.ts|ehr-web,pdms-web,identity-web"
  "src/features/patients/usePatientName.ts|ehr-web,pdms-web,identity-web"
)

failures=0

for entry in "${MANIFEST[@]}"; do
  rel="${entry%%|*}"
  subset="${entry#*|}"
  if [[ "$subset" == "$entry" ]]; then
    apps=("${APPS[@]}")
    explicit=0
  else
    IFS=',' read -r -a apps <<<"$subset"
    explicit=1
  fi

  present=()
  for app in "${apps[@]}"; do
    if [[ -f "$FRONTEND/$app/$rel" ]]; then
      present+=("$app")
    elif [[ "$explicit" == 1 ]]; then
      echo "FAIL  $rel — expected in $app (listed in the sync manifest) but the file is missing."
      failures=$((failures + 1))
    fi
  done

  # A file held by zero or one app has nothing to drift against.
  if ((${#present[@]} < 2)); then
    continue
  fi

  declare -A by_hash=()
  for app in "${present[@]}"; do
    hash="$(md5sum "$FRONTEND/$app/$rel" | cut -d' ' -f1)"
    by_hash["$hash"]+="$app "
  done

  if ((${#by_hash[@]} > 1)); then
    failures=$((failures + 1))
    echo "FAIL  $rel has diverged across the apps that duplicate it:"
    for hash in "${!by_hash[@]}"; do
      echo "        $hash  ${by_hash[$hash]}"
    done
    # Show the actual drift between the first two differing copies.
    first_app=""
    for hash in "${!by_hash[@]}"; do
      app="${by_hash[$hash]%% *}"
      if [[ -z "$first_app" ]]; then
        first_app="$app"
      else
        echo "      diff $first_app/$rel $app/$rel:"
        diff -u "$FRONTEND/$first_app/$rel" "$FRONTEND/$app/$rel" | sed 's/^/        /' || true
        break
      fi
    done
    echo "      Fix: pick the intended copy and re-copy it byte-for-byte to the other apps."
  else
    only_hash="${!by_hash[*]}"
    printf "ok    %-55s %s  (%d apps)\n" "$rel" "$only_hash" "${#present[@]}"
  fi
  unset by_hash
done

if ((failures > 0)); then
  echo
  echo "$failures duplicated file(s) out of sync. The duplication convention is byte-for-byte —"
  echo "edit one copy, then propagate the identical bytes to every other app that holds it."
  exit 1
fi

echo
echo "All duplicated files are in sync."
