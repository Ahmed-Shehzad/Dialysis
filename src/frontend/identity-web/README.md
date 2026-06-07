# identity-web — Admin & Compliance Console

> **Naming note:** the folder is `identity-web` but the app is the cross-cutting **Admin console** (router base `/admin`) — identity/role administration plus HIPAA & GDPR surfaces. It is **not** the login service; sign-in is handled by the BFFs + Keycloak (see [Identity ARCHITECTURE.md](../../backend/Identity/ARCHITECTURE.md)).

| | |
|---|---|
| Context base / router basename | `/admin` |
| Standalone dev port (Vite) | `5336` |
| Backing BFF | `Dialysis.Admin.Bff` (`admin-bff`, port `5306`) |
| BFF primary API | HIE host (data-protection / HIPAA live there) |
| BFF aggregations | HIS, EHR, PDMS, SmartConnect (DICOM) |
| Real-time push | No |

## What it does

- **Admin hub** (`/admin`) and **identity admin** (`/admin/identity`) — users, roles.
- **HIPAA dashboard** (`/admin/hipaa`).
- **GDPR / data protection** — RoPA (`/admin/data-protection/ropa`), consents (`/admin/data-protection/consents`), and **data-subject-rights** (`/admin/data-protection/data-subject-rights`) where the DPO approves Art. 15/17 requests (the approve-and-execute erasure pipeline).
- **Demo control panel** (`/admin/demo`) — drives the dev DataSimulator.

## Stack & scripts

React 18 + Vite 6 + TypeScript 5 + TanStack Query 5, **npm**; `BrowserRouter basename="/admin"`.

```bash
npm run dev        # Vite :5336
npm run build
npm run lint
npm run typecheck
npm run test:e2e
```

## How it runs

Reached through the Gateway at `http://localhost:9090/admin/`; the Admin BFF handles auth and proxies `/admin/api` to the HIE host (which hosts the data-protection & HIPAA endpoints) plus cross-context aggregations for embedded EHR/PDMS panels. `enforceGatewayOrigin()` keeps the cookie intact.

> Cross-context navigation must be a full-page hop.

See [src/backend/HIE/ARCHITECTURE.md](../../backend/HIE/ARCHITECTURE.md) (Art. 17 pipeline + `IErasureRequestStore`) and [src/backend/Identity/ARCHITECTURE.md](../../backend/Identity/ARCHITECTURE.md) (auth). Shared conventions: [root README](../../../README.md#frontend).
