# his-web — Front Desk SPA

The HIS (Hospital Information System) browser app: **patient access, scheduling, and the receptionist "today" queue**. Backed by the **`Dialysis.HIS.Bff`** per-context BFF.

|                                |                                                                                                  |
| ------------------------------ | ------------------------------------------------------------------------------------------------ |
| Context base / router basename | `/his`                                                                                           |
| Standalone dev port (Vite)     | `5331`                                                                                           |
| Backing BFF                    | `Dialysis.HIS.Bff` (`his-bff`, port `5301`)                                                      |
| BFF aggregations               | SmartConnect DICOMweb                                                                            |
| Real-time push                 | **Yes** — consumes `PatientAdmittedIntegrationEvent` → live today-board toasts via `/his/events` |

## What it does

- **Today board / queue** (`/his/today`) — the live receptionist queue: check-in, walk-in, assign-chair, eligibility. Queue mutations use the optimistic `useQueueMutation` helper (optimistic update → rollback on error → invalidate on settle).
- **Workflows** (`/his/workflows`) — operational workflow surface.
- **Admin** — billing exports (`/his/admin/billing/exports`), device registry (`/his/admin/devices`, with durable `IngestDeviceReading`).
- Manager dashboard cards + an integration-events table.

## Stack & scripts

React 18 + Vite 6 + TypeScript 5 + TanStack Query 5, **npm**. Routing via `react-router` with `BrowserRouter basename="/his"`. Realtime via `@microsoft/signalr`; charts via `echarts`; PDF via `pdfjs-dist`.

```bash
npm run dev        # Vite :5331 (predev runs npm install)
npm run build      # tsc -b && vite build
npm run lint       # eslint . --max-warnings=0
npm run typecheck  # tsc -b --noEmit
npm run test:e2e   # Playwright
```

## How it runs

Under the Aspire AppHost the app is reached **only through the Gateway at `http://localhost:9090/his/`** — the Gateway YARP-routes `/his/{**}` to this SPA and `/his/api`, `/his/hubs`, `/his/identity`, `/his/events` to the HIS BFF. `enforceGatewayOrigin()` bounces a raw `:5331` visit back to the Gateway so the BFF session cookie (path-scoped to `/his`) is never lost. Auth is a cookie/OIDC flow handled entirely by the BFF; the SPA calls `/his/identity/user` for `name/email/roles/permissions` and gates UI with `PermissionGate` (`permissions.includes(...)`).

> **Per-context routing:** in-app `<Link>`s stay unprefixed (the basename adds `/his`); navigating to another context (e.g. `/ehr`) must be a **full-page hop**, not a `navigate()`/`<Link>` — a client-side route there bounces through the Gateway and looks like a refresh.

See [src/backend/Identity/ARCHITECTURE.md](../../backend/Identity/ARCHITECTURE.md) for the BFF/gateway/auth model and [src/backend/HIS/ARCHITECTURE.md](../../backend/HIS/ARCHITECTURE.md) for the API it talks to. Shared conventions (Husky pre-commit, lint-staged, the module-shell building blocks copied into `src/shell/`, `humanizeError`, the durable-command toast pattern) are described in the [root README](../../../README.md#frontend).
