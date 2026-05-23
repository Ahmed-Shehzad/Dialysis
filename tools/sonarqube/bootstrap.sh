#!/bin/sh
# SonarQube first-boot bootstrap.
#
# Runs inside the sonarqube-bootstrap container (curlimages/curl). Once the
# SonarQube health endpoint reports UP, the script:
#   1. Logs in with the documented admin/admin first-boot credential.
#   2. On first run only: rotates the password to a known dev value
#      ("DialysisDev!1") so subsequent runs of this script remain idempotent
#      without manual intervention. The new password is also written to
#      /state/admin-password.txt for tooling.
#   3. Creates the configured project (idempotent — POST returns 4xx when the
#      project already exists; we treat that as success).
#   4. Generates a long-lived analysis token (idempotent — if a token with the
#      same name already exists, SonarQube returns the existing one or we
#      regenerate it via revoke + create).
#   5. Writes the token to /state/scanner-token.txt — picked up by
#      tools/sonarqube/scan.sh and tools/sonarqube/README.md.
#
# Environment (set by AppHost):
#   SONAR_URL              http://sonarqube:9000 (DCP internal hostname)
#   SONAR_ADMIN_USER       admin
#   SONAR_ADMIN_PASSWORD   admin            (rotated on first run)
#   SONAR_PROJECT_KEY      dialysis
#   SONAR_PROJECT_NAME     Dialysis Modular Monolith
#
# This script is read-only mounted; persisted state lives in the bootstrap volume.

set -eu

STATE_DIR="/state"
NEW_ADMIN_PWD="DialysisDev!1"
TOKEN_NAME="dialysis-scanner"

mkdir -p "$STATE_DIR"

# ---- 1. Wait for SonarQube /api/system/status = UP ----------------------------
# Aspire's health check waits for the container to report healthy before
# starting us, but reindexing during cold boots can put the API in STARTING
# for an extra 10-30s. Poll until UP rather than racing the first call.
echo "Waiting for SonarQube at $SONAR_URL ..."
for i in $(seq 1 60); do
  status=$(curl -fsS "$SONAR_URL/api/system/status" 2>/dev/null | tr -d ' "{}' | sed -n 's/.*status:\([A-Z]*\).*/\1/p' || echo "")
  if [ "$status" = "UP" ]; then
    echo "SonarQube is UP."
    break
  fi
  echo "  attempt $i: status=$status"
  sleep 5
done

if [ "$status" != "UP" ]; then
  echo "ERROR: SonarQube did not become UP in time." >&2
  exit 1
fi

# ---- 2. Rotate admin password (idempotent) -----------------------------------
# If /state/admin-password.txt exists, the rotation already happened on a prior
# boot — load it. Otherwise, attempt the rotation; if the API responds 401, the
# password has already been rotated by something else (e.g., manual change in
# the UI) and we abort with a clear message rather than overwriting it.
if [ -f "$STATE_DIR/admin-password.txt" ]; then
  ADMIN_PWD=$(cat "$STATE_DIR/admin-password.txt")
  echo "Using stored admin password."
else
  echo "Rotating admin password..."
  http_code=$(curl -s -o /tmp/change.txt -w "%{http_code}" \
    -u "$SONAR_ADMIN_USER:$SONAR_ADMIN_PASSWORD" \
    -X POST "$SONAR_URL/api/users/change_password" \
    --data-urlencode "login=$SONAR_ADMIN_USER" \
    --data-urlencode "previousPassword=$SONAR_ADMIN_PASSWORD" \
    --data-urlencode "password=$NEW_ADMIN_PWD")
  if [ "$http_code" != "204" ] && [ "$http_code" != "200" ]; then
    cat /tmp/change.txt >&2
    echo "ERROR: failed to rotate admin password (http $http_code). If it was already changed manually, delete the bootstrap volume to reset." >&2
    exit 1
  fi
  echo -n "$NEW_ADMIN_PWD" > "$STATE_DIR/admin-password.txt"
  ADMIN_PWD="$NEW_ADMIN_PWD"
  echo "Admin password rotated; stored at $STATE_DIR/admin-password.txt."
fi

# ---- 3. Create the project (idempotent) --------------------------------------
echo "Ensuring project '$SONAR_PROJECT_KEY' exists..."
http_code=$(curl -s -o /tmp/create.txt -w "%{http_code}" \
  -u "$SONAR_ADMIN_USER:$ADMIN_PWD" \
  -X POST "$SONAR_URL/api/projects/create" \
  --data-urlencode "project=$SONAR_PROJECT_KEY" \
  --data-urlencode "name=$SONAR_PROJECT_NAME")
case "$http_code" in
  200|201) echo "Project created." ;;
  400)     # already exists — SonarQube returns 400 with error code
    if grep -q "already exists" /tmp/create.txt 2>/dev/null; then
      echo "Project already exists; reusing."
    else
      cat /tmp/create.txt >&2
      echo "ERROR: project creation failed (http 400)." >&2
      exit 1
    fi
    ;;
  *)
    cat /tmp/create.txt >&2
    echo "ERROR: project creation failed (http $http_code)." >&2
    exit 1
    ;;
esac

# ---- 4. Generate analysis token ----------------------------------------------
# Revoke any existing token with the same name first, then mint a fresh one.
# Tokens are scoped to projects/analysis; rotating per-boot keeps stale tokens
# from accumulating in long-lived setups.
echo "Generating analysis token '$TOKEN_NAME'..."
curl -fsS -u "$SONAR_ADMIN_USER:$ADMIN_PWD" \
  -X POST "$SONAR_URL/api/user_tokens/revoke" \
  --data-urlencode "name=$TOKEN_NAME" > /dev/null || true

token_response=$(curl -fsS -u "$SONAR_ADMIN_USER:$ADMIN_PWD" \
  -X POST "$SONAR_URL/api/user_tokens/generate" \
  --data-urlencode "name=$TOKEN_NAME" \
  --data-urlencode "type=GLOBAL_ANALYSIS_TOKEN")

# Naive JSON extract for the "token" field — avoids pulling jq into the
# minimal curl image.
TOKEN=$(echo "$token_response" | sed -n 's/.*"token":"\([^"]*\)".*/\1/p')
if [ -z "$TOKEN" ]; then
  echo "ERROR: could not parse token from response: $token_response" >&2
  exit 1
fi
echo -n "$TOKEN" > "$STATE_DIR/scanner-token.txt"
chmod 600 "$STATE_DIR/scanner-token.txt"

# ---- 5. Print the scan command for the dev -----------------------------------
cat <<EOF

================================================================================
SonarQube bootstrap complete.

  URL:           http://localhost:9000
  Admin login:   $SONAR_ADMIN_USER / $NEW_ADMIN_PWD
  Project:       $SONAR_PROJECT_KEY
  Token (file):  /state/scanner-token.txt   (inside the bootstrap volume)

Run the scanner from the repo root:

  SONAR_TOKEN=\$(docker run --rm -v dialysis-sonarqube-bootstrap:/state alpine cat /state/scanner-token.txt) \\
    tools/sonarqube/scan.sh

GitHub integration is a one-time setup. See tools/sonarqube/README.md.
================================================================================
EOF
