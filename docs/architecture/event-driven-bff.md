# Event-driven BFF push

Combines the BFF pattern with event-driven architecture: each per-context BFF stays a stateless
OIDC + YARP proxy for **reads** (synchronous aggregation is unchanged), and additionally **consumes
integration events off RabbitMQ and pushes them to its SPA over SignalR** as live toasts/badges.

This is **real-time push only**. The notification payload is a "go look" signal (metadata, no
clinical record); the SPA refetches the authoritative data through the BFF's synchronous,
permission-checked API. There is **no** BFF-side read model or cache — a BFF never originates a
state change, so it has no DbContext and no outbox (the "no event sourcing" rule in `CLAUDE.md`
still holds).

## Flow

```
module API (producer)
   │  state change + IntegrationEvent in the same EF transaction
   ▼
Transponder outbox ──relay──▶ RabbitMQ  exchange: dialysis.<producer>.events  (direct, routing key = event FullName)
                                  │  fan-out: one copy per bound queue
                                  ├──▶ owning module's consumer queue        (unchanged)
                                  └──▶ bff-<ctx> queue  ──▶ IConsumer<TEvent> in the BFF
                                                              │  map → BffNotification
                                                              ▼
                                          IBffNotifier ──▶ SignalR group  patient:{id} / user:{sub}
                                                              │  (Valkey backplane fans across BFF replicas)
                                                              ▼
                                          SPA  /<ctx>/events hub ──▶ <ToastHost/> toast + nav badge
                                                              │
                                                              ▼
                                          SPA refetches via /<ctx>/api/... (synchronous, authorized)
```

## Why this is safe to bolt on

- **Consume-only, no DB.** `AddConsumer<TEvent,TConsumer>()` is enough; the registered consume route
  drives the RabbitMQ queue binding. No outbox/inbox, no `DbContext`.
- **Own copy, no theft.** The transport uses a **direct** exchange with routing key = the event's
  `Type.FullName`, and each host binds its **own** queue. A unique `bff-<ctx>` queue therefore gets
  its own copy of each event and never competes with the owning module's consumers for deliveries.
- **Replica-safe.** Multiple replicas of one BFF share the `bff-<ctx>` queue (one processes each
  message); the **Valkey SignalR backplane** then routes the push to whichever replica holds the
  client connection.

## Building block: `Dialysis.Module.Bff.Events`

Pairs with `Dialysis.Module.Bff`. A BFF host opts in with two calls:

```csharp
builder.AddModuleBff();
builder.AddModuleBffEvents(t =>
    t.AddConsumer<LabResultReceivedIntegrationEvent, LabResultNotificationConsumer>());
// ...
app.MapModuleBff();
app.MapModuleBffEvents();   // maps the SignalR hub at {BasePath}/events
```

| Type | Role |
|---|---|
| `NotificationsHub` | `[Authorize]` SignalR hub at `/<ctx>/events`; auto-joins `user:{sub}` on connect; `WatchPatientAsync`/`UnwatchPatientAsync` join/leave `patient:{id}`. |
| `BffNotification` | PHI-light DTO: `Type`, `Title`, `Summary?`, `PatientId?`, `Link?`, `OccurredOn`. |
| `IBffNotifier` / `SignalRBffNotifier` | `PushToPatientAsync` / `PushToUserAsync` over `IHubContext<NotificationsHub>`. |
| `AddModuleBffEvents(...)` | Registers SignalR (+ Valkey backplane when configured), `IBffNotifier`, the consumers, and — only when `Bff:Events:RabbitMq:ConnectionUri` is set — the RabbitMQ consume transport on queue `bff-<slug>`. |
| `MapModuleBffEvents()` | Maps the hub at `{BasePath}/events`. |

Each consumer is a stateless `IConsumer<TEvent>` that maps its event to a `BffNotification` and calls
`IBffNotifier.PushToPatientAsync(...)`.

## Configuration (`Bff:Events`)

| Key | Meaning | Dev/test default |
|---|---|---|
| `Bff:Events:RabbitMq:ConnectionUri` | AMQP URI. Unset ⇒ in-process bus, no broker. | unset (Aspire injects in prod) |
| `Bff:Events:RabbitMq:QueueName` | Override; defaults to `bff-<slug>`. | `bff-<slug>` |
| `Bff:Events:SignalR:BackplaneConnectionString` | Valkey/Redis backplane. Unset ⇒ in-process SignalR. | unset (Aspire injects Valkey) |

The Aspire AppHost wires both centrally in `AddContextBff` (`.WithReference(rabbit)` /
`.WithReference(valkey)` + the `Bff__Events__…` env vars), so every BFF gets the transport and any
that calls `AddModuleBffEvents` lights up.

## Wired today (reference contexts)

| BFF | Consumes | SPA effect |
|---|---|---|
| EHR | `LabResultReceivedIntegrationEvent` (Lab.Contracts) | "New lab result" toast on the chart |
| PDMS | `IntradialyticAdverseEventIntegrationEvent` (PDMS.Contracts) | chairside alarm (error toast) |
| HIS | `PatientAdmittedIntegrationEvent` (HIS.Contracts) | today-board "patient admitted" toast |

SmartConnect / HIE / Admin / Portal can opt in the same way (a consumer + the two calls); the
gateway already routes `/<ctx>/...` to each BFF, and a `/<ctx>/events` route is added per wired
context.

## Frontend

Each `<ctx>-web` app ships `src/features/notifications/useBffNotifications.ts`, called from
`AppShell`. It opens a SignalR connection to `/<ctx>/events` (cookie-authenticated on the gateway
origin — no bearer needed), watches the selected patient from `PatientContext`, and on each
`notification` dispatches into the existing `<ToastHost/>`. It is best-effort: with no broker/backplane
in dev the connection simply no-ops.

## Gateway

A per-context route `/<ctx>/events/{**}` → `<ctx>-bff` (ordered above the `/<ctx>/{**}`→SPA
catch-all) carries the hub. WebSocket upgrade is YARP-default.
