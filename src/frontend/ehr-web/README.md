# ehr-web — Clinical Chart SPA

The EHR (Electronic Health Record) browser app: the **patient record, orders, notes and billing**. The richest feature surface in the platform. Backed by the **`Dialysis.EHR.Bff`** per-context BFF.

|                                |                                                                                                                                                                                     |
| ------------------------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Context base / router basename | `/ehr`                                                                                                                                                                              |
| Standalone dev port (Vite)     | `5332`                                                                                                                                                                              |
| Backing BFF                    | `Dialysis.EHR.Bff` (`ehr-bff`, port `5302`)                                                                                                                                         |
| BFF aggregations               | HIE, Lab, SmartConnect (DICOM)                                                                                                                                                      |
| Real-time push                 | **Yes** — `useBffNotifications` on `/ehr/events`; the BFF pushes PHI-light notifications (lab results, admissions, portal messages/requests) rendered as generic type-driven toasts |

## What it does

- **Patients** (`/ehr/patients`) and the **chart** (`/ehr/patients/:patientId`) — notes, care plan, care team, timeline, lab results, imaging/DICOM viewer, order sets / labs / prescriptions / referrals, clinical recommendations, quality gaps, safety advisories, after-visit-summary authoring, secure messaging, plus embedded HIE community-record and consent panels.
- **Workflows**, **care-coordination worklist**, **appointment requests**.
- **Billing admin** — dialysis charges (`/ehr/admin/billing/dialysis-charges`), fee schedule.
- **Population** quality (`/ehr/population/quality`) and **safety surveillance** (`/ehr/safety/surveillance`).

Route definitions live in `EHR_ROUTES` and are kept in lockstep with the nav by a test (`AppRouter.nav.test.tsx`).

## Stack & scripts

React 18 + Vite 8 + TypeScript 6 + TanStack Query 5, **npm** (Node ≥ 20.19); `react-router` v7 with `BrowserRouter basename="/ehr"`; Tailwind CSS 4 (CSS-first, no `tailwind.config.js`). Imaging via `pdfjs-dist` + DICOM; charts via `echarts`.

```bash
npm run dev        # Vite :5332
npm run build      # tsc -b && vite build
npm run lint
npm run typecheck
npm run test:e2e
```

## How it runs

Reached through the Gateway at `http://localhost:9090/ehr/`; the EHR BFF handles OIDC/cookie auth and proxies `/ehr/api`, `/ehr/hubs`, `/ehr/events` to the EHR module API (attaching the bearer). `enforceGatewayOrigin()` keeps the path-scoped cookie intact. UI gated by `PermissionGate`.

> Cross-context navigation (to `/his`, `/pdms`, …) must be a full-page hop, not a client-side route.

See [src/backend/EHR/ARCHITECTURE.md](../../backend/EHR/ARCHITECTURE.md) for the API/domain and [src/backend/Identity/ARCHITECTURE.md](../../backend/Identity/ARCHITECTURE.md) for the auth model. Shared frontend conventions are in the [root README](../../../README.md#frontend).
