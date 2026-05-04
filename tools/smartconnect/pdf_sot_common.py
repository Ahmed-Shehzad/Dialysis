"""Shared helpers for SmartConnect PDF source-of-truth tooling."""

from __future__ import annotations

import hashlib
import json
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

REPO_ROOT = Path(__file__).resolve().parents[2]
DEFAULT_PDF = REPO_ROOT / "docs" / "book" / "mirth-connect-user-guide.pdf"
GUIDE_TOC_JSON = REPO_ROOT / "docs" / "book" / "guide-toc.json"
TRACEABILITY_MD = REPO_ROOT / "docs" / "smartconnect" / "guide-traceability.md"
OVERRIDES_JSON = REPO_ROOT / "docs" / "smartconnect" / "traceability-overrides.json"
ALLOWLIST_JSON = REPO_ROOT / "docs" / "smartconnect" / "traceability-toc-allowlist.json"


def is_lfs_pointer(path: Path) -> bool:
    try:
        head = path.read_bytes()[:200]
    except OSError:
        return False
    return head.startswith(b"version https://git-lfs.github.com/spec/v1")


def stable_entry_id(title: str, level: int, page: int | None) -> str:
    page_s = str(page if page is not None else -1)
    raw = f"{level}|{title.strip()}|{page_s}".encode("utf-8")
    return hashlib.sha256(raw).hexdigest()[:12]


def walk_outline(items: Any, level: int = 0):
    """Yield (destination_or_item, level) for pypdf outline trees (lists + Destinations)."""
    if not items:
        return
    for item in items:
        if isinstance(item, list):
            yield from walk_outline(item, level + 1)
        else:
            yield item, level


def extract_outline_entries(pdf_path: Path) -> list[dict[str, Any]]:
    from pypdf import PdfReader

    reader = PdfReader(str(pdf_path))
    outline = reader.outline
    entries: list[dict[str, Any]] = []
    for dest, level in walk_outline(outline, 0):
        title = getattr(dest, "title", None)
        if title is None and hasattr(dest, "get"):
            title = dest.get("/Title")
        if title is None:
            title = str(dest)
        if isinstance(title, bytes):
            title = title.decode("utf-8", errors="replace")
        title = str(title).strip()
        if not title:
            continue
        try:
            page0 = reader.get_destination_page_number(dest)
            page = page0 + 1
        except Exception:
            page = None
        eid = stable_entry_id(title, level, page)
        entries.append({"id": eid, "title": title, "level": level, "page": page})
    return entries


def build_toc_document(pdf_rel: str, entries: list[dict[str, Any]]) -> dict[str, Any]:
    return {
        "sourcePdf": pdf_rel.replace("\\", "/"),
        "generatedAt": datetime.now(timezone.utc).isoformat(),
        "entries": entries,
    }


def md_escape_cell(text: str) -> str:
    return text.replace("|", "\\|").replace("\n", " ")


def load_json(path: Path, default: Any) -> Any:
    if not path.is_file():
        return default
    return json.loads(path.read_text(encoding="utf-8"))


def parse_traceability_ids(md_text: str) -> list[str]:
    """First-column PDF ids from the traceability markdown table."""
    ids: list[str] = []
    in_table = False
    for line in md_text.splitlines():
        if line.strip().startswith("| PDF id |"):
            in_table = True
            continue
        if not in_table:
            continue
        if line.strip().startswith("|--------"):
            continue
        if not line.strip().startswith("|"):
            break
        parts = [p.strip() for p in line.strip().split("|")]
        parts = [p for p in parts if p]
        if len(parts) < 1:
            continue
        ids.append(parts[0].strip().strip("`"))
    return ids
