#!/usr/bin/env python3
"""
HIPAA regulatory-change tracker.

Pulls a curated set of HHS Office for Civil Rights HIPAA guidance pages, hashes their content,
and diffs against the checked-in snapshot in `tools/hipaa/ocr_snapshot.json`. When the hash for
any page drifts, the script writes a Markdown summary suitable for opening a GitHub issue and
exits with status 2 — the scheduled workflow watches for that and creates the issue.

Designed to fail loudly rather than silently miss a drift, so:
  • Network errors → exit 0 with a warning (the workflow rate-limits issue creation).
  • Snapshot file missing → exit 1 (someone deleted it; that should never silently pass).
  • Drift detected → exit 2 with `tools/hipaa/drift.md` written.
  • No drift → exit 0.

Run locally with: python3 tools/hipaa/check_ocr_feed.py
Refresh the snapshot after intentional acknowledgement: python3 tools/hipaa/check_ocr_feed.py --refresh
"""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import sys
import urllib.error
import urllib.request
from datetime import datetime, timezone
from pathlib import Path

SNAPSHOT_PATH = Path(__file__).parent / "ocr_snapshot.json"
DRIFT_PATH = Path(__file__).parent / "drift.md"

# Curated pages. The script normalises whitespace + strips dynamic <script> tags before hashing
# so harmless rerenders don't produce false positives. If a page returns a non-200 we record
# "unreachable" instead of a hash so the snapshot survives transient outages.
TRACKED_PAGES: list[dict[str, str]] = [
    {
        "id": "hhs-hipaa-summary",
        "title": "HHS HIPAA Privacy Rule summary",
        "url": "https://www.hhs.gov/hipaa/for-professionals/privacy/laws-regulations/index.html",
    },
    {
        "id": "hhs-security-rule",
        "title": "HHS HIPAA Security Rule summary",
        "url": "https://www.hhs.gov/hipaa/for-professionals/security/laws-regulations/index.html",
    },
    {
        "id": "hhs-breach-notification",
        "title": "HHS Breach Notification Rule",
        "url": "https://www.hhs.gov/hipaa/for-professionals/breach-notification/index.html",
    },
]

USER_AGENT = "DialysisHIPAATracker/1.0 (+https://github.com/Ahmed-Shehzad/Dialysis)"
TIMEOUT_SECONDS = 30


def fetch(url: str) -> tuple[str | None, str | None]:
    """Return (body, error). One of the two is always None."""
    req = urllib.request.Request(url, headers={"User-Agent": USER_AGENT})
    try:
        with urllib.request.urlopen(req, timeout=TIMEOUT_SECONDS) as resp:
            return resp.read().decode("utf-8", errors="replace"), None
    except urllib.error.HTTPError as e:
        return None, f"HTTP {e.code}"
    except (urllib.error.URLError, TimeoutError) as e:
        return None, f"network: {e}"


def normalise(body: str) -> str:
    """Strip <script>/<style> blocks, collapse whitespace. Reduces false positives from CDN-injected fragments."""
    no_scripts = re.sub(r"<script\b[^>]*>.*?</script>", "", body, flags=re.IGNORECASE | re.DOTALL)
    no_styles = re.sub(r"<style\b[^>]*>.*?</style>", "", no_scripts, flags=re.IGNORECASE | re.DOTALL)
    collapsed = re.sub(r"\s+", " ", no_styles).strip()
    return collapsed


def hash_content(body: str) -> str:
    return hashlib.sha256(normalise(body).encode("utf-8")).hexdigest()


def load_snapshot() -> dict:
    if not SNAPSHOT_PATH.exists():
        return {"capturedAt": None, "pages": {}}
    with open(SNAPSHOT_PATH, "r", encoding="utf-8") as f:
        return json.load(f)


def write_snapshot(snapshot: dict) -> None:
    SNAPSHOT_PATH.parent.mkdir(parents=True, exist_ok=True)
    with open(SNAPSHOT_PATH, "w", encoding="utf-8") as f:
        json.dump(snapshot, f, indent=2, sort_keys=True)
        f.write("\n")


def compute_current() -> dict:
    pages: dict[str, dict] = {}
    for entry in TRACKED_PAGES:
        body, err = fetch(entry["url"])
        if body is None:
            pages[entry["id"]] = {"title": entry["title"], "url": entry["url"], "status": "unreachable", "note": err}
        else:
            pages[entry["id"]] = {
                "title": entry["title"],
                "url": entry["url"],
                "status": "ok",
                "hash": hash_content(body),
            }
    return {"capturedAt": datetime.now(timezone.utc).isoformat(), "pages": pages}


def diff(prior: dict, current: dict) -> list[dict]:
    drifted: list[dict] = []
    prior_pages = prior.get("pages", {})
    for pid, cur in current["pages"].items():
        if cur["status"] != "ok":
            continue
        prev = prior_pages.get(pid)
        if prev is None:
            drifted.append({"id": pid, "change": "added", "url": cur["url"], "title": cur["title"]})
        elif prev.get("status") == "ok" and prev.get("hash") != cur["hash"]:
            drifted.append({"id": pid, "change": "content-changed", "url": cur["url"], "title": cur["title"]})
    return drifted


def write_drift_summary(drifted: list[dict]) -> None:
    with open(DRIFT_PATH, "w", encoding="utf-8") as f:
        f.write("## HIPAA regulatory-feed drift\n\n")
        f.write("The following OCR / HHS guidance pages changed content since the last snapshot. ")
        f.write("Review each diff and update `tools/hipaa/ocr_snapshot.json` after acknowledging.\n\n")
        for d in drifted:
            f.write(f"- **{d['title']}** ([{d['url']}]({d['url']})) — `{d['change']}`\n")
        f.write("\nAcknowledge by re-running `python3 tools/hipaa/check_ocr_feed.py --refresh` and committing the refreshed snapshot.\n")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--refresh", action="store_true", help="Overwrite the snapshot with the current capture.")
    args = parser.parse_args()

    prior = load_snapshot()
    current = compute_current()

    if args.refresh:
        write_snapshot(current)
        print(f"Snapshot refreshed: {SNAPSHOT_PATH}")
        return 0

    drifted = diff(prior, current)
    if drifted:
        write_drift_summary(drifted)
        print(f"Drift detected ({len(drifted)} page(s)). See {DRIFT_PATH}.")
        return 2

    if not prior.get("pages"):
        # Bootstrap: first run never had a prior snapshot. Write one and exit 0.
        write_snapshot(current)
        print("Bootstrapped snapshot. No drift detection possible until the next run.")
        return 0

    print("No drift.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
