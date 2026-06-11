# patient-portal-web — Patient Portal SPA

The patient-facing browser app: **appointments, messages, medications, results and admissions** for the patient themselves. Backed by the **`Dialysis.PatientPortal.Bff`** per-context BFF.

|                                |                                                                                                                                       |
| ------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------- |
| Context base / router basename | `/portal`                                                                                                                             |
| Standalone dev port (Vite)     | `5337`                                                                                                                                |
| Backing BFF                    | `Dialysis.PatientPortal.Bff` (`portal-bff`, port `5307`)                                                                              |
| BFF primary API                | HIS (appointments / admissions)                                                                                                       |
| BFF aggregations               | EHR, PDMS, HIE, SmartConnect (DICOM)                                                                                                  |
| Real-time push                 | **Yes** — `PatientPortalSecureMessageReceived`, `PatientPortalAppointmentResolved`, `AfterVisitSummaryPublished` via `/portal/events` |

## What it does

A single page (`/portal` → `PatientPortalPage`) composing patient-facing panels: secure **messages**, **appointment requests** (with a book-appointment dialog), **after-visit summaries**, care plan, lab results, recent treatments, reminders, consents, and an outside-records (HIE) card.

Its notification hook is distinctive: on each pushed `BffNotification` it not only toasts but **invalidates the matching TanStack query** (`secure-message` / `appointment-request` / `after-visit-summary`) so the panel refetches authoritative data through the synchronous API.

> **Dev access:** the dev Keycloak realm seeds only a staff `demo` user (no patient persona). Patient-self routes return 403 unless the dev staff-impersonation flag is on. The patient claim is `his_patient_id` (supplied by the `dialysis-portal-bff` client mapper), falling back to `sub`.

## Stack & scripts

React 18 + Vite 8 + TypeScript 6 + TanStack Query 5, **npm** (Node ≥ 20.19); `react-router` v7 with `BrowserRouter basename="/portal"`; Tailwind CSS 4 (CSS-first, no `tailwind.config.js`).

```bash
npm run dev        # Vite :5337
npm run build
npm run lint
npm run typecheck
npm run test:e2e
```

## How it runs

Reached through the Gateway at `http://localhost:9090/portal/`; the portal BFF handles auth and proxies `/portal/api`, `/portal/hubs`, `/portal/events`. `enforceGatewayOrigin()` keeps the `/portal` cookie intact.

> Cross-context navigation must be a full-page hop.

See [src/backend/EHR/ARCHITECTURE.md](../../backend/EHR/ARCHITECTURE.md) (portal slice) and [src/backend/Identity/ARCHITECTURE.md](../../backend/Identity/ARCHITECTURE.md) (auth + patient-claim scoping). Shared conventions: [root README](../../../README.md#frontend).
