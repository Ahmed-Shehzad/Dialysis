# SmartConnect architecture flowcharts

This document explains how **SmartConnect** under `src/backend/SmartConnect` fits together: host wiring, message ingress, `FlowRuntimeEngine`, persistence, and management APIs.

Primary code references:

- [`Program.cs`](../../src/backend/SmartConnect/Api/Dialysis.SmartConnect.Api/Program.cs) — DI, routes, optional JWT
- [`FlowRuntimeEngine.cs`](../../src/backend/SmartConnect/Dialysis.SmartConnect.Core/FlowRuntimeEngine.cs) — pipeline execution
- [`InboundTransport.cs`](../../src/backend/SmartConnect/Inbound/Dialysis.SmartConnect.Inbound.Abstractions/InboundTransport.cs) — preflight + dispatch to runtime
- [`SmartConnectInboundEndpointExtensions.cs`](../../src/backend/SmartConnect/Inbound/Dialysis.SmartConnect.Inbound.AspNetCore/SmartConnectInboundEndpointExtensions.cs) — HTTP ingress
- [`SourceConnectorHostedService.cs`](../../src/backend/SmartConnect/Inbound/Dialysis.SmartConnect.Inbound.Hosting/SourceConnectorHostedService.cs) — background source connectors

---

## 1. Solution shape (projects and responsibilities)

```mermaid
flowchart TB
  subgraph api [Dialysis.SmartConnect.Api]
    Program[Program_cs_routes_and_DI]
    Www[wwwroot_smartconnect_static_UI]
    Program --> Www
  end

  subgraph inbound [Inbound_packages]
    AspInbound[Inbound_AspNetCore_HTTP_POST]
    Mllp[Inbound_Mllp_TCP_listener]
    Hosting[Inbound_Hosting_SourceConnectorHostedService]
    Abstractions[Inbound_Abstractions_IInboundTransport_IFactory]
    AspInbound --> Abstractions
    Mllp --> Abstractions
    Hosting --> Abstractions
  end

  subgraph core [Dialysis.SmartConnect_Core]
    Runtime[FlowRuntimeEngine_IFlowRuntime]
    Plugins[IFlowPluginRegistry_filters_transforms_outbound]
    Scripts[ChannelScriptExecutor_Jint]
    Pruner[DataPrunerHostedService]
    Runtime --> Plugins
    Runtime --> Scripts
  end

  subgraph persistence [Persistence_EFCore]
    Repo[IIntegrationFlowRepository]
    Ledger[IMessageLedger_IMessageLedgerQuery]
    Maps[IVariableMapStore]
    Db[SmartConnectDbContext]
    Repo --> Db
    Ledger --> Db
    Maps --> Db
  end

  subgraph management [Management_AspNetCore]
    Admin[flows_lifecycle_import_export]
    LedgerApi[ledger_message_browser]
    MapsApi[configuration_maps_CRUD]
    Events[audit_events]
    PrunerApi[pruner_config_readout]
  end

  Program --> core
  Program --> persistence
  Program --> inbound
  Program --> management
  Abstractions --> Runtime
  Runtime --> Repo
  Runtime --> Ledger
  management --> Repo
  management --> Ledger
  management --> Maps
  management --> Events
  Pruner --> Ledger
```

**Notes**

- **`IInboundTransport`** (`InboundTransport.cs`): optional flow preflight, then **`IFlowRuntime.DispatchAsync`**.
- **Ingress**: HTTP (`SmartConnectInboundEndpointExtensions`), MLLP TCP (`MllpInboundHostedService`), and **source connectors** (`SourceConnectorHostedService`) all use **`IInboundTransport`** (scoped), same path as **Channel Writer** (in-process chaining).

---

## 2. End-to-end path: from ingress to ledger and outbounds

```mermaid
flowchart LR
  subgraph sources [Message_sources]
    HttpPost["POST_/smartconnect/v1/flows/{flowId}/messages"]
    TcpMllp[MLLP_TCP_frames]
    SrcConn[FileReader_DatabaseReader_TcpListener_etc]
    ChanWr[ChannelWriterOutboundAdapter]
  end

  subgraph factory [Message_build]
    MsgFact[IInboundMessageFactory]
  end

  subgraph transport [Scoped_pipeline]
    Inb[IInboundTransport_InboundTransport]
    Run[IFlowRuntime_FlowRuntimeEngine]
  end

  subgraph sidefx [Persistence_and_plugins]
    LedgerW[IMessageLedger_append_status]
    Out[Outbound_adapters_HTTP_File_SMTP_TCP_DB_ChannelWriter]
  end

  HttpPost --> MsgFact
  TcpMllp --> MsgFact
  SrcConn --> MsgFact
  ChanWr --> MsgFact
  MsgFact --> Inb
  Inb --> Run
  Run --> LedgerW
  Run --> Out
```

**Ordering**

1. Build **`IntegrationMessage`** (payload, format, correlation id, metadata).
2. **`InboundTransport.DispatchAsync`**: if `IIntegrationFlowRepository` is registered, check flow exists and **`RuntimeState == Started`** (HTTP can map 404/409).
3. **`FlowRuntimeEngine.DispatchAsync`**: ledger + pipeline (section 3).
4. Outbound adapters may call **`IInboundTransport`** again (e.g. channel writer), opening a **new scoped** dispatch for the target flow.

---

## 3. Detailed pipeline: `FlowRuntimeEngine.DispatchAsync`

Aligned with `FlowRuntimeEngine.cs`.

```mermaid
flowchart TD
  start([DispatchAsync_IntegrationMessage])
  recv["Append_ledger_Received_plus_payload_snapshot"]
  loadFlow["Load_flow_from_IIntegrationFlowRepository"]
  stateGate{"Flow_RuntimeState_Started?"}
  pre["Optional_PreProcessor_script_Jint"]
  preDrop{"Dropped?"}
  filters["For_each_RouteFilterSlot_resolve_filter_EvaluateAsync"]
  filterDrop{"Disposition_Drop?"}
  routes["For_each_OutboundRoute_index_i"]
  resolveOut["TryResolveOutboundAdapter_kind"]
  badOut{"Adapter_missing?"}
  xform["TryTransformForRouteAsync_per_route_TransformStages"]
  xfail{"Transform_error?"}
  attachParams["Attach_outbound_parameters_to_metadata_if_JSON"]
  retry["SendAsync_with_exponential_backoff_MaxAttempts"]
  sendOk{"Send_succeeded?"}
  respX["Optional_ResponseTransformStages_on_response_bytes"]
  seq{"OutboundRoutesSequential_and_failure?"}
  moreRoutes{"More_routes?"}
  done["Append_ledger_Completed"]
  post["Optional_PostProcessor_script"]
  endNode([Return_FlowDispatchResult])

  start --> recv --> loadFlow --> stateGate
  stateGate -->|no| failState([Failure_paused_or_stopped])
  stateGate -->|yes| pre
  pre --> preDrop
  preDrop -->|yes| ledgerDrop1["Ledger_RouteFilterDropped_PreProcessor"] --> endDrop([Success_empty_routes])
  preDrop -->|no| filters
  filters --> filterDrop
  filterDrop -->|yes| ledgerDrop2["Ledger_RouteFilterDropped"] --> endDrop2([Success_empty_routes])
  filterDrop -->|no| routes
  routes --> resolveOut
  resolveOut --> badOut
  badOut -->|yes| ledgerOutFail["Ledger_OutboundFailed"] --> seq
  badOut -->|no| xform
  xform --> xfail
  xfail -->|yes| ledgerXFail["Ledger_OutboundFailed"] --> seq
  xfail -->|no| attachParams --> retry --> sendOk
  sendOk -->|no| ledgerSendFail["Ledger_OutboundFailed"] --> seq
  sendOk -->|yes| ledgerSent["Ledger_OutboundSent"] --> respX
  respX --> seq
  seq -->|break_parallel| moreRoutes
  seq -->|continue| moreRoutes
  moreRoutes -->|yes| routes
  moreRoutes -->|no| done --> post --> endNode
```

**Behavior summary**

| Stage | Role |
|--------|------|
| **Ledger `Received`** | Auditable intake with payload snapshot. |
| **Flow state** | Only **Started** flows run; **Paused** / not started short-circuit. |
| **PreProcessor** | Optional Jint; can drop or replace payload. |
| **Route filters** | Ordered slots; **Drop** stops pipeline and logs filter drop. |
| **Outbound routes** | Each route: **transform chain** → **SendAsync** (retries) → ledger **Sent** / **Failed**. |
| **Sequential vs parallel** | If **`OutboundRoutesSequential`**, first hard failure stops further routes; otherwise other routes still run. |
| **Response transforms** | If adapter returns **response bytes** and route defines **`ResponseTransformStages`**, run transform chain and keep response payload for **`FlowDispatchResult`**. |
| **PostProcessor** | Optional Jint after all routes; sees overall success flag. |

---

## 4. Management and operator surface (orthogonal to dispatch)

```mermaid
flowchart LR
  Browser[Operator_static_HTML]
  JsonApi["JSON_/smartconnect/v1/admin/_and_/events/_and_/maps"]
  Jwt["Optional_JWT_AddSmartConnectManagementJwt"]
  Browser --> JsonApi
  Jwt -.-> JsonApi
  JsonApi --> Repo[(flows)]
  JsonApi --> Ledger[(message_ledger)]
  JsonApi --> Maps[(variable_maps)]
  JsonApi --> Audit[(audit_events)]
```

`Program.cs` maps inbound routes, management routes, ledger, configuration maps, events, pruner; optional **`UseAuthentication`** when JWT authority is configured (`ManagementSecurityExtensions`).

---

## 5. Layered view (who talks to whom)

Logical layers for **runtime workflows** vs **operations**.

```mermaid
flowchart TB
  subgraph external [External_systems]
    HttpClient[HTTP_clients_webhooks]
    TcpPeers[TCP_MLLP_peers]
    Files[(Filesystem_DB_poll_sources)]
  end

  subgraph edge [Edge_adapters]
    Kestrel[Kestrel_ASP_NET_Core]
    MllpListener[MLLP_listener_hosted_service]
    SrcRun[Source_connector_tasks]
  end

  subgraph app [SmartConnect_application]
    InRoutes[Inbound_routes_and_transport]
    CoreRun[FlowRuntimeEngine_and_plugins]
    MgmtRoutes[Management_JSON_endpoints]
    StaticUI[Static_operator_UI]
  end

  subgraph data [Data_and_pruning]
    Ef[(EF_Core_SmartConnectDbContext)]
    PrunerSvc[DataPrunerHostedService]
  end

  HttpClient --> Kestrel
  TcpPeers --> MllpListener
  Files --> SrcRun
  Kestrel --> InRoutes
  MllpListener --> InRoutes
  SrcRun --> InRoutes
  InRoutes --> CoreRun
  CoreRun --> Ef
  MgmtRoutes --> Ef
  StaticUI --> MgmtRoutes
  PrunerSvc --> Ef
```

---

## 6. Sequence: HTTP inbound message through one flow

Typical path: **`POST /smartconnect/v1/flows/{flowId}/messages`** → scoped **`InboundTransport`** → **`FlowRuntimeEngine`**.

```mermaid
sequenceDiagram
  participant Client as HTTP_client
  participant Kestrel as Kestrel_endpoint
  participant MsgFact as IInboundMessageFactory
  participant Inb as IInboundTransport
  participant Repo as IIntegrationFlowRepository
  participant Run as FlowRuntimeEngine
  participant Ledger as IMessageLedger
  participant Plugins as IFlowPluginRegistry
  participant Out as IOutboundAdapter

  Client->>Kestrel: POST_body_plus_headers
  Kestrel->>MsgFact: Build_IntegrationMessage
  MsgFact-->>Kestrel: message
  Kestrel->>Inb: DispatchAsync_message
  Inb->>Repo: GetByIdAsync_preflight_optional
  Repo-->>Inb: flow_Started
  Inb->>Run: DispatchAsync_message
  Run->>Ledger: Append_Received
  Run->>Repo: GetByIdAsync_pipeline
  Repo-->>Run: IntegrationFlow
  Run->>Plugins: Route_filters_then_transforms
  Plugins-->>Run: filtered_or_transformed_message
  Run->>Out: SendAsync_per_route_with_retries
  Out-->>Run: OutboundSendResult
  Run->>Ledger: Append_Sent_or_Failed_per_route
  Run->>Ledger: Append_Completed
  Run-->>Inb: FlowDispatchResult
  Inb-->>Kestrel: InboundReceiveResult
  Kestrel-->>Client: HTTP_200_or_error
```

---

## 7. Sequence: Channel Writer (flow A → flow B)

`ChannelWriterOutboundAdapter` opens a **new DI scope**, resolves **`IInboundTransport`**, and dispatches into **another flow** (depth guard in metadata).

```mermaid
sequenceDiagram
  participant RunA as FlowRuntime_flow_A
  participant CW as ChannelWriterOutboundAdapter
  participant Scope as IServiceScopeFactory
  participant Inb as IInboundTransport_scoped
  participant RunB as FlowRuntime_flow_B
  participant Ledger as IMessageLedger

  RunA->>CW: SendAsync_message_route_N
  CW->>Scope: CreateAsyncScope
  CW->>Inb: DispatchAsync_child_message
  Inb->>RunB: DispatchAsync_same_engine_new_flowId
  RunB->>Ledger: Append_Received_for_B
  Note over RunB: Full_pipeline_on_B_filters_outbounds
  RunB-->>Inb: FlowDispatchResult
  Inb-->>CW: InboundReceiveResult
  CW-->>RunA: OutboundSendResult_awaited
```

This is **in-process chaining**: not a separate network hop to self unless an outbound adapter explicitly does so.

---

## 8. Swimlanes: parallel workflows on the host

```mermaid
flowchart TB
  subgraph lane_ops [Operations_and_UI]
    OpUI[Browser_wwwroot_smartconnect]
    AdminApi[REST_admin_configure_flows_maps_pruner]
    OpUI --> AdminApi
  end

  subgraph lane_ingress [Ingress_workers]
    HttpW[HTTP_request_threads]
    MllpW[MLLP_background_listener]
    SrcW[SourceConnectorHostedService_tasks]
  end

  subgraph lane_core [Per_message_scoped_work]
    Dispatch[InboundTransport_to_FlowRuntimeEngine]
  end

  subgraph lane_bg [Background_hosted_services]
    Prune[DataPrunerHostedService]
    MllpStart[MllpInboundHostedService]
  end

  subgraph lane_data [Persistence]
    LedgerTables[(message_ledger_EF_Core)]
  end

  AdminApi -.->|read_write_definitions| lane_core
  HttpW --> Dispatch
  MllpW --> Dispatch
  SrcW --> Dispatch
  MllpStart --> MllpW
  Dispatch --> LedgerTables
  Prune -.->|delete_expired_rows| LedgerTables
```

**Interpretation**

- **lane_ops**: operators change configuration; does not execute channel filters/transforms for live payloads (except via **reprocess** APIs that inject work into **lane_core**).
- **lane_ingress**: multiple concurrent entry points; each message eventually hits **Dispatch** (scoped).
- **lane_bg**: pruner and TCP listener run independently of a single HTTP request.

---

## See also

- [Scope vs Mirth Connect](scope-vs-mirth.md)
- [User guide traceability matrix](guide-traceability.md)
