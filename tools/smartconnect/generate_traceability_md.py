#!/usr/bin/env python3
"""Generate docs/smartconnect/guide-traceability.md from guide-toc.json and overrides."""

from __future__ import annotations

import argparse
import sys

from pdf_sot_common import (
    GUIDE_TOC_JSON,
    OVERRIDES_JSON,
    TRACEABILITY_MD,
    load_json,
    md_escape_cell,
)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true", help="Exit 1 if markdown would change")
    args = parser.parse_args()
    if not GUIDE_TOC_JSON.is_file():
        print(f"error: missing {GUIDE_TOC_JSON}", file=sys.stderr)
        return 1
    toc = load_json(GUIDE_TOC_JSON, None)
    if not isinstance(toc, dict) or "entries" not in toc:
        print("error: guide-toc.json must be an object with 'entries'", file=sys.stderr)
        return 1
    entries = toc["entries"]
    overrides = load_json(OVERRIDES_JSON, {})
    by_id = {}
    if isinstance(overrides, dict):
        by_id = overrides.get("byId") or {}
        if not isinstance(by_id, dict):
            by_id = {}

    rows: list[str] = []
    if not entries:
        rows.append(
            "| *(pending toc)* | No outline entries extracted yet. | — | "
            "Add `docs/book/mirth-connect-user-guide.pdf`, run `extract_pdf_toc.py`, re-run this script. | "
            "N/A | — |"
        )
    for e in entries:
        eid = e.get("id", "")
        title = md_escape_cell(str(e.get("title", "")))
        page = e.get("page")
        page_s = "—" if page is None else str(page)
        o = by_id.get(eid, {}) if isinstance(by_id.get(eid, {}), dict) else {}
        mapping = md_escape_cell(str(o.get("mapping", "—")))
        status = md_escape_cell(str(o.get("status", "In progress")))
        evidence = md_escape_cell(str(o.get("evidence", "—")))
        rows.append(f"| `{eid}` | {title} | {page_s} | {mapping} | {status} | {evidence} |")

    gen_line = "<!-- gen: generated from docs/book/guide-toc.json + traceability-overrides.json; do not hand-edit data rows without updating overrides or re-running extract -->"
    body = f"""# SmartConnect — user guide traceability matrix

{gen_line}

**Source of truth:** [mirth-connect-user-guide.pdf](../book/mirth-connect-user-guide.pdf) (Git LFS). Each row is anchored to a **PDF outline** entry (`id` = stable hash of title, level, page). Thematic groupings alone are **not** sufficient for requirements coverage.

| PDF id | Title | Page | SmartConnect mapping | Status | Evidence |
|--------|-------|------|----------------------|--------|----------|
{chr(10).join(rows)}
"""

    if args.check:
        cur = TRACEABILITY_MD.read_text(encoding="utf-8") if TRACEABILITY_MD.is_file() else ""
        if cur != body:
            print("error: guide-traceability.md is out of date; run generate_traceability_md.py", file=sys.stderr)
            return 1
        return 0

    TRACEABILITY_MD.parent.mkdir(parents=True, exist_ok=True)
    TRACEABILITY_MD.write_text(body, encoding="utf-8")
    print(f"wrote {TRACEABILITY_MD}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
