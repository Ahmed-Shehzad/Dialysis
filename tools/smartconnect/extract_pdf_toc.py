#!/usr/bin/env python3
"""Emit docs/book/guide-toc.json from the canonical user-guide PDF only."""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

from pdf_sot_common import (
    DEFAULT_PDF,
    GUIDE_TOC_JSON,
    REPO_ROOT,
    build_toc_document,
    extract_outline_entries,
    is_lfs_pointer,
)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "pdf",
        nargs="?",
        default=str(DEFAULT_PDF),
        help="Path to mirth-connect-user-guide.pdf (default: docs/book/... under repo root)",
    )
    args = parser.parse_args()
    pdf_path = Path(args.pdf).expanduser().resolve()
    if not pdf_path.is_file():
        print(f"error: PDF not found: {pdf_path}", file=sys.stderr)
        return 1
    if pdf_path.stat().st_size < 512:
        print("error: PDF is too small to be a real document (or checkout incomplete).", file=sys.stderr)
        return 1
    if is_lfs_pointer(pdf_path):
        print(
            "error: file is a Git LFS pointer, not the PDF. Run `git lfs pull` or checkout with LFS enabled.",
            file=sys.stderr,
        )
        return 1
    try:
        entries = extract_outline_entries(pdf_path)
    except Exception as ex:
        print(f"error: failed to read PDF outline: {ex}", file=sys.stderr)
        return 1
    try:
        rel = pdf_path.relative_to(REPO_ROOT)
        rel_s = str(rel).replace("\\", "/")
    except ValueError:
        rel_s = str(pdf_path)
    doc = build_toc_document(rel_s, entries)
    GUIDE_TOC_JSON.parent.mkdir(parents=True, exist_ok=True)
    GUIDE_TOC_JSON.write_text(json.dumps(doc, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(f"wrote {GUIDE_TOC_JSON} ({len(entries)} outline nodes)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
