#!/usr/bin/env python3
"""Re-extract TOC from disk and fail if docs/book/guide-toc.json differs."""

from __future__ import annotations

import json
import sys
from pdf_sot_common import (
    DEFAULT_PDF,
    GUIDE_TOC_JSON,
    REPO_ROOT,
    build_toc_document,
    extract_outline_entries,
    is_lfs_pointer,
)


def normalize(doc: dict) -> dict:
    """Compare structural equality ignoring generatedAt."""
    return {
        "sourcePdf": doc.get("sourcePdf"),
        "entries": doc.get("entries"),
    }


def main() -> int:
    if not DEFAULT_PDF.is_file():
        print(f"error: PDF not found at {DEFAULT_PDF}", file=sys.stderr)
        return 1
    if DEFAULT_PDF.stat().st_size < 512:
        print(
            "error: PDF is too small (incomplete checkout or LFS pointer without pull).",
            file=sys.stderr,
        )
        return 1
    if is_lfs_pointer(DEFAULT_PDF):
        print("error: PDF path is still an LFS pointer", file=sys.stderr)
        return 1
    if not GUIDE_TOC_JSON.is_file():
        print(f"error: committed {GUIDE_TOC_JSON} missing", file=sys.stderr)
        return 1
    entries = extract_outline_entries(DEFAULT_PDF)
    try:
        rel = DEFAULT_PDF.relative_to(REPO_ROOT)
        rel_s = str(rel).replace("\\", "/")
    except ValueError:
        rel_s = str(DEFAULT_PDF)
    fresh = build_toc_document(rel_s, entries)
    committed = json.loads(GUIDE_TOC_JSON.read_text(encoding="utf-8"))
    if normalize(fresh) != normalize(committed):
        print(
            "error: guide-toc.json is out of date vs PDF; run tools/smartconnect/extract_pdf_toc.py",
            file=sys.stderr,
        )
        print(json.dumps({"fresh": normalize(fresh), "committed": normalize(committed)}, indent=2), file=sys.stderr)
        return 1
    print("ok (guide-toc.json matches PDF)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
