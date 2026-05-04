#!/usr/bin/env python3
"""Validate guide-traceability.md: TOC coverage, no orphan ids, optional top-level allowlist."""

from __future__ import annotations

import re
import sys

from pdf_sot_common import (
    ALLOWLIST_JSON,
    GUIDE_TOC_JSON,
    TRACEABILITY_MD,
    load_json,
    parse_traceability_ids,
)

_HEX12 = re.compile(r"^[a-f0-9]{12}$")


def main() -> int:
    if not TRACEABILITY_MD.is_file():
        print(f"error: missing {TRACEABILITY_MD}", file=sys.stderr)
        return 1
    md = TRACEABILITY_MD.read_text(encoding="utf-8")
    row_ids = parse_traceability_ids(md)
    toc = load_json(GUIDE_TOC_JSON, None)
    if not isinstance(toc, dict):
        print("error: guide-toc.json missing or invalid", file=sys.stderr)
        return 1
    entries = toc.get("entries") or []
    if not isinstance(entries, list):
        print("error: guide-toc.json 'entries' must be a list", file=sys.stderr)
        return 1

    toc_ids = {e["id"] for e in entries if isinstance(e, dict) and "id" in e}

    if not toc_ids:
        # Empty TOC: matrix must not claim PDF-backed hex ids.
        bad = [i for i in row_ids if _HEX12.match(i)]
        if bad:
            print(f"error: hex ids present in matrix but TOC is empty: {bad}", file=sys.stderr)
            return 1
        if "*(pending toc)*" not in md:
            print("error: empty TOC requires placeholder row with *(pending toc)*", file=sys.stderr)
            return 1
        print("ok (pending toc — add PDF and extract)")
        return 0

    missing = sorted(toc_ids - set(row_ids))
    orphans = [i for i in row_ids if _HEX12.match(i) and i not in toc_ids]

    allow = load_json(ALLOWLIST_JSON, {})
    allowed_missing = set()
    if isinstance(allow, dict):
        raw = allow.get("allowedMissingTopLevelIds") or []
        if isinstance(raw, list):
            allowed_missing = {str(x) for x in raw}

    top_level = {e["id"] for e in entries if isinstance(e, dict) and e.get("level") == 0 and "id" in e}
    missing_top = sorted((top_level & set(missing)) - allowed_missing)

    if orphans:
        print(f"error: orphan PDF ids in matrix (not in TOC): {orphans}", file=sys.stderr)
        return 1
    if missing:
        print(f"error: TOC entries missing from matrix: {missing}", file=sys.stderr)
        return 1
    if missing_top:
        print(
            f"error: top-level chapters missing from matrix (allowlist or add rows): {missing_top}",
            file=sys.stderr,
        )
        return 1
    print("ok")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
