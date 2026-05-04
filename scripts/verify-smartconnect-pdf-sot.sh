#!/usr/bin/env bash
# Run the same checks as .github/workflows/smartconnect-pdf-sot.yml (local / CI helper).
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"
python3 -m pip install -q -r tools/smartconnect/requirements.txt
python3 tools/smartconnect/verify_toc_committed.py
python3 tools/smartconnect/generate_traceability_md.py --check
python3 tools/smartconnect/validate_traceability.py
echo "SmartConnect PDF SoT checks passed."
