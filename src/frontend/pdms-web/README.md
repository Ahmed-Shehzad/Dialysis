# pdms-web — Chairside SPA

The PDMS (Patient Data Management System) browser app: **live treatment, real-time vitals, and machine alarms** at the dialysis chair. Backed by the **`Dialysis.PDMS.Bff`** per-context BFF.

|                                |                                                                                                             |
| ------------------------------ | ----------------------------------------------------------------------------------------------------------- |
| Context base / router basename | `/pdms`                                                                                                     |
| Standalone dev port (Vite)     | `5333`                                                                                                      |
| Backing BFF                    | `Dialysis.PDMS.Bff` (`pdms-bff`, port `5303`)                                                               |
| BFF aggregations               | EHR, HIE, SmartConnect (DICOM)                                                                              |
| Real-time push                 | **Yes** — `useBffNotifications` on `/pdms/events`; the BFF pushes PHI-light notifications (e.g. intradialytic adverse events) rendered as generic type-driven **error** toasts |

## What it does

- **Sessions** (`/pdms/sessions`) and the **live session view** (`/pdms/sessions/:sessionId`) — lifecycle controls (schedule/start/pause/resume/complete/abort), medications administration (MAR), session reports, documents/retention.
- **Chairside vitals** (`src/features/vitals/`) — `useVitalsStream` subscribes to the SignalR `VitalsHub` (`/pdms/hubs/vitals`), rendering live vitals charts, the latest-values panel, an audible alarm, and a live cost tile. The cost snapshot ticks every 5 s; a Valkey backplane fans out across BFF replicas.
- **Chair board** (`/pdms/chairs`) and admin surfaces — inventory, reporting templates, on-call rotation/policies/audit.
- Durable `RecordReading` writes use the `useDurableCommand` hook (202 → poll → toast).

## Stack & scripts

React 18 + Vite 8 + TypeScript 6 + TanStack Query 5, **npm** (Node ≥ 20.19); `react-router` v7 with `BrowserRouter basename="/pdms"`; Tailwind CSS 4 (CSS-first, no `tailwind.config.js`). Realtime via `@microsoft/signalr`; charts via `echarts`/`d3`.

```bash
npm run dev        # Vite :5333
npm run build
npm run lint
npm run typecheck
npm run test:e2e
```

## How it runs

Reached through the Gateway at `http://localhost:9090/pdms/`; the PDMS BFF handles auth and proxies `/pdms/api`, `/pdms/hubs` (the vitals stream), and `/pdms/events`. `enforceGatewayOrigin()` keeps the `/pdms` cookie intact.

> Cross-context navigation must be a full-page hop.

See [src/backend/PDMS/ARCHITECTURE.md](../../backend/PDMS/ARCHITECTURE.md) for the telemetry domain, durable command bus and the vitals/cost broadcast, and [src/backend/Identity/ARCHITECTURE.md](../../backend/Identity/ARCHITECTURE.md) for auth. Shared conventions: [root README](../../../README.md#frontend).
