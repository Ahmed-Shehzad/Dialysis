# SmartConnect — PDF source-of-truth tooling

## Dependencies

- **Python 3.11+**
- **PyPI:** `pip install -r tools/smartconnect/requirements.txt` (installs `pypdf`).

No OS packages are required for outline extraction; `pypdf` reads bookmarks in-process.

## Scripts

| Script | Purpose |
|--------|---------|
| `extract_pdf_toc.py` | Reads **only** `docs/book/mirth-connect-user-guide.pdf` (or path passed as argv[1]), writes `docs/book/guide-toc.json`. Exits non-zero if the file is missing, an LFS pointer, or unreadable. |
| `generate_traceability_md.py` | Builds `docs/smartconnect/guide-traceability.md` from `guide-toc.json` plus optional `traceability-overrides.json`. |
| `validate_traceability.py` | Ensures every matrix row cites a TOC `id`, no orphan ids, and no missing top-level chapters unless allowlisted. |
| `verify_toc_committed.py` | Re-runs extraction and fails if `guide-toc.json` is out of date vs the PDF. |

## Overrides

Edit `docs/smartconnect/traceability-overrides.json`:

```json
{
  "byId": {
    "YOUR12CHARID": {
      "mapping": "API route or path to implementation",
      "status": "Done",
      "evidence": "Test name or PR"
    }
  }
}
```

Keys must match `id` values from `guide-toc.json` after you run the extractor.

## Allowlist (missing chapters)

File `docs/smartconnect/traceability-toc-allowlist.json` lists top-level TOC `id` values that may be omitted from the matrix (empty object `{}` means none allowed).

## CI

GitHub Actions runs on push and pull request: checkout with `lfs: true`, verify the PDF is materialized, extract TOC, confirm `guide-toc.json` matches, then validate the traceability matrix.

Local parity with CI (from repo root):

```bash
./scripts/verify-smartconnect-pdf-sot.sh
```

## Team runbook

### When the PDF changes (new edition or different bookmarks)

1. Replace `docs/book/mirth-connect-user-guide.pdf` (same path only).
2. `python3 tools/smartconnect/extract_pdf_toc.py` — refreshes `docs/book/guide-toc.json` (`id` values may change if titles/pages shift).
3. Update `docs/smartconnect/traceability-overrides.json` so `byId` keys still line up with new `id`s where you care about mapping/status/evidence.
4. `python3 tools/smartconnect/generate_traceability_md.py` — refreshes `docs/smartconnect/guide-traceability.md`.
5. Commit **together** in one commit (or stacked commits): the PDF (LFS), `guide-toc.json`, `guide-traceability.md`, and any override edits.

### When to use the top-level allowlist

Edit `docs/smartconnect/traceability-toc-allowlist.json` only if a **top-level** PDF outline chapter (`level` 0 in `guide-toc.json`) is intentionally absent from the matrix. Add its `id` to `allowedMissingTopLevelIds`. Prefer filling overrides instead of allowlisting; allowlist entries should be rare and reviewed.

### Weak PDF bookmarks

If the outline is incomplete, extraction still reflects the PDF; consider expanding overrides and evidence text rather than maintaining a second “shadow” TOC file unless product governance requires it.

## Canonical SmartConnect artifact paths (for overrides)

Use repo-relative paths from the repository root when filling `mapping` / `evidence` after you have PDF `id`s from `guide-toc.json`.

| Area | Path |
|------|------|
| API host | `src/backend/SmartConnect/Api/Dialysis.SmartConnect.Api/Program.cs` |
| Inbound HTTP routes | `src/backend/SmartConnect/Inbound/Dialysis.SmartConnect.Inbound.AspNetCore/SmartConnectInboundEndpointExtensions.cs` |
| Inbound queue consumer | `src/backend/SmartConnect/Inbound/Dialysis.SmartConnect.Inbound.Hosting/SmartConnectInboundQueueConsumer.cs` |
| Inbound MLLP | `src/backend/SmartConnect/Inbound/Dialysis.SmartConnect.Inbound.Mllp/MllpInboundHostedService.cs` |
| Inbound transponder bridge | `src/backend/SmartConnect/Inbound/Dialysis.SmartConnect.Inbound.Transponder/TransponderInboundTransportBridge.cs` |
| Runtime engine | `src/backend/SmartConnect/Dialysis.SmartConnect.Core/FlowRuntimeEngine.cs` |
| EF persistence | `src/backend/SmartConnect/Persistence/Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Abstractions/SmartConnectDbContext.cs` |
| Management routes | `src/backend/SmartConnect/Management/Dialysis.SmartConnect.Management.AspNetCore/ManagementEndpointExtensions.cs` |
| Ledger routes | `src/backend/SmartConnect/Management/Dialysis.SmartConnect.Management.AspNetCore/LedgerEndpointExtensions.cs` |
| Management JWT (optional) | `src/backend/SmartConnect/Management/Dialysis.SmartConnect.Management.AspNetCore/ManagementSecurityExtensions.cs` |
| Outbound HTTP | `src/backend/SmartConnect/Dialysis.SmartConnect.Core/ExtendedPlugins/HttpOutboundAdapter.cs` |
| Outbound file | `src/backend/SmartConnect/Dialysis.SmartConnect.Core/ExtendedPlugins/FileOutboundAdapter.cs` |
| Outbound SMTP | `src/backend/SmartConnect/Dialysis.SmartConnect.Core/ExtendedPlugins/SmtpOutboundAdapter.cs` |
| JS transform stage | `src/backend/SmartConnect/Dialysis.SmartConnect.Core/ExtendedPlugins/JavascriptTransformStage.cs` |
| Operator UI | `src/backend/SmartConnect/Api/Dialysis.SmartConnect.Api/wwwroot/smartconnect/index.html` |
| HIS integration | `src/backend/HIS/Dialysis.HIS.Integration/SmartConnectHisIntegrationExtensions.cs` |
| JS transform tests | `src/backend/SmartConnect/Tests/Dialysis.SmartConnect.Tests/JavascriptTransformStageTests.cs` |
