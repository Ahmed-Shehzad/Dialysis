# hie-web — Health-Information-Exchange SPA

The HIE browser app: **FHIR partners, consent, subscriptions, documents and TEFCA/QHIN administration**. Backed by the **`Dialysis.HIE.Bff`** per-context BFF.

| | |
|---|---|
| Context base / router basename | `/hie` |
| Standalone dev port (Vite) | `5335` |
| Backing BFF | `Dialysis.HIE.Bff` (`hie-bff`, port `5305`) |
| BFF aggregations | SmartConnect (DICOM), HIS, EHR, PDMS (subscription/authoring catalogs) |
| Real-time push | No |

## What it does

- **FHIR exchange** (`/hie/fhir-exchange`) — outbound queue, inbound feed, partner status, consent and community-record panels.
- **FHIR authoring** (`/hie/fhir-authoring`) and **subscriptions** (`/hie/subscriptions`, with a live subscription stream).
- **Documents admin** (`/hie/admin/documents`) — `DocumentReference` list + PDF viewer drawer; **retention** policies (`/hie/admin/documents/retention`).
- **TEFCA partners** (`/hie/admin/tefca/partners`) — QHIN onboarding, trust anchors, mTLS, IAS-JWT minting.
- **MPI steward** (`/hie/admin/mpi/reviews`) and **terminology authoring** (`/hie/admin/terminology`).

## Stack & scripts

React 18 + Vite 6 + TypeScript 5 + TanStack Query 5, **npm**; `BrowserRouter basename="/hie"`. PDF via `pdfjs-dist`/`react-pdf`.

```bash
npm run dev        # Vite :5335
npm run build
npm run lint
npm run typecheck
npm run test:e2e
```

## How it runs

Reached through the Gateway at `http://localhost:9090/hie/`; the HIE BFF handles auth and proxies `/hie/api` (including the FHIR + admin surfaces) and `/hie/hubs` (subscription stream). `enforceGatewayOrigin()` keeps the cookie intact.

> Cross-context navigation must be a full-page hop.

See [src/backend/HIE/ARCHITECTURE.md](../../backend/HIE/ARCHITECTURE.md) for the FHIR/IHE/TEFCA domain and [src/backend/Identity/ARCHITECTURE.md](../../backend/Identity/ARCHITECTURE.md) for auth. Shared conventions: [root README](../../../README.md#frontend).
