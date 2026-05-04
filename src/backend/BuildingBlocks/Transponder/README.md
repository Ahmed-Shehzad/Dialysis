# Transponder

Distributed messaging framework for .NET (programming model, transports, durability, scheduling, sagas). This folder holds core libraries; **transport, persistence, and scheduler integrations are delivered as separate class libraries** that reference `Transponder.Abstractions` / `Transponder.Core` as appropriate.

## Transports

### Implemented

| Transport | Project | Role |
|-----------|---------|------|
| **RabbitMQ** | `Transport/Transponder.Transport.RabbitMq` | AMQP direct exchange, typed routing keys, optional DLX, publisher confirms |
| **NATS** | `Transport/Transponder.Transport.Nats` | Core NATS publish/subscribe; routing key and correlation in message headers |
| **Azure Service Bus** | `Transport/Transponder.Transport.AzureServiceBus` | Topic publish + subscription processor; routing key in application properties (same names as **`TransponderTransportHeaderNames`**) |
| **Amazon SQS** | `Transport/Transponder.Transport.AwsSqsSns` | Single **standard** queue; long-poll receive; routing key in message attributes |
| **gRPC** | `Transport/Transponder.Transport.Grpc` | Unary **Publish** + server-streaming **Subscribe** ingress relay; **`Grpc.Net.Client`** publishers/consumers and **`Grpc.AspNetCore`** relay host |
| **SignalR** | `Transport/Transponder.Transport.SignalR` | **`TransponderSignalRHub`** ingress: clients invoke **`Publish`**; all connections receive **`Receive`** with **`TransponderSignalREnvelopeDto`**; **`Microsoft.AspNetCore.SignalR.Client`** for **`AddTransponderSignalR`** |
| **Server-Sent Events (SSE)** | `Transport/Transponder.Transport.ServerSentEvents` | HTTP **POST** JSON publish + **GET** **`text/event-stream`** subscribe on **`TransponderSseIngressOptions.PathPrefix`** (default **`/transponder/sse`**); in-memory fan-out relay; **`HttpClient`** for **`AddTransponderServerSentEvents`** |

### Planned (stubs / future)

| Transport | Role |
|-----------|------|
| **AWS SNS** | Optional fan-out in front of SQS (configure in AWS; this library uses SQS send/receive only) |

## Supported persistence

| Store | Status | Notes |
|-------|--------|--------|
| **EF Core (shared model)** | Implemented | `Persistence/Transponder.Persistence.EntityFrameworkCore.Shared` — **`TransponderPersistenceOptions`**, **`TransponderOutboxMessageEntity`**, **`TransponderInboxMessageEntity`**, **`TransponderSagaInstanceEntity`** (durable sagas, **`SagaInstances`** table), **`EntityFrameworkCoreTransponderSagaStore{TContext}`**, **`TransponderPersistenceDbContextBase`**, **`TransponderPersistenceDesignTimeConfiguration`** (loads client config for `dotnet ef`). |
| **SQL Server (EF Core)** | Implemented | `Persistence/Transponder.Persistence.EntityFrameworkCore.SqlServer` — provider-specific **`TransponderPersistenceDbContext`** and `Migrations/`. |
| **PostgreSQL (EF Core)** | Implemented | `Persistence/Transponder.Persistence.EntityFrameworkCore.Postgresql` — same; Npgsql. |

## Supported scheduling

| Scheduler | Project | Status |
|-----------|---------|--------|
| **Hangfire** | `Scheduling/Transponder.Scheduling.Hangfire` | **`AddTransponderHangfireScheduling`** — delayed + recurring jobs call **`ITransponderBus.PublishPreparedAsync`**; cron uses Hangfire / NCrontab |
| **Quartz.NET** | `Scheduling/Transponder.Scheduling.Quartz` | **`AddTransponderQuartzScheduling`** — same fire semantics; cron uses Quartz syntax (register **`AddQuartz`** + **`AddQuartzHostedService`** yourself) |
| **TickerQ** | `Scheduling/Transponder.Scheduling.TickerQ` | **`AddTransponderTickerQScheduling`** (wraps **`AddTickerQ<TimeTickerEntity, CronTickerEntity>`** + discovery) or split **`ConfigureTransponderTickerQDiscovery`** + **`AddTransponderTickerQSchedulerOnly`**; cron is **six-field** (second …); call **`app.UseTickerQ()`**; add EF Core or Redis persistence per [TickerQ docs](https://tickerq.net/) |

### Scheduling API

**Abstractions:** **`ITransponderMessageScheduler`** — **`ScheduleOnceAsync<TMessage>(message, runAt, publishOptions?)`** returns an id for **`TryCancelOnceAsync`**; **`ScheduleRecurringAsync<TMessage>(message, cronExpression, publishOptions?, scheduleId?, timeZone?)`** returns a stable recurring id (supply **`scheduleId`** to update the same logical schedule) for **`TryCancelRecurringAsync`**. The message snapshot is stored with **`IMessageSerializer`** (same JSON as **`PublishAsync<T>`**); when the job runs, **`PublishPreparedAsync`** is used with **`Type.FullName`** as the routing key. Register **exactly one** scheduler integration per host (**`RemoveDescriptorsFor`** is applied inside each **`AddTransponder*Scheduling`** extension).

**Pragmatic usage**

1. Call **`AddTransponder`** and your transport (so **`ITransponderBus`** publishes out-of-process if needed).
2. Pick **Hangfire**, **Quartz**, or **TickerQ** (only one **`ITransponderMessageScheduler`** per host).
3. Inject **`ITransponderMessageScheduler`** where you enqueue work (application services, MVC controllers, etc.).

**Hangfire quick path:** `services.AddHangfire(...);` `services.AddHangfireServer();` then **`services.AddTransponderHangfireScheduling();`**. Example recurring: **`Cron.Daily`** or a raw NCrontab string.

**Quartz quick path:** `services.AddQuartz(q => q.UseMicrosoftDependencyInjectionJobFactory());` `services.AddQuartzHostedService(o => o.WaitForJobsToComplete = true);` then **`services.AddTransponderQuartzScheduling();`**. One-time jobs are created in group **`transponder-once`**; recurring in **`transponder-recurring`** (cancel APIs expect the returned id and use these groups internally).

**TickerQ quick path:** `services.AddTransponderTickerQScheduling(b => { /* persistence: EF Core, Redis, etc. — see TickerQ docs */ });` then **`app.UseTickerQ();`**. Recurring **`scheduleId`** must be a **Guid** string when you need a stable id for **`TryCancelRecurringAsync`**; otherwise an id is generated. The **`timeZone`** parameter on **`ScheduleRecurringAsync`** is ignored for TickerQ (cron is interpreted in the scheduler’s configured zone).

**TickerQ IntelliSense / symbols on NuGet:** For **10.3.0**, the **`TickerQ`**, **`TickerQ.Utilities`**, and **`TickerQ.SDK`** packages on [nuget.org](https://www.nuget.org/) ship assemblies **without** companion **`*.xml`** doc files inside the `.nupkg`, and **`tickerq.10.3.0.snupkg`** is not on the feed (404). Arcenox does not publish a separate documentation or “analysis” package; related ids are **`TickerQ.EntityFrameworkCore`**, **`TickerQ.Dashboard`**, **`TickerQ.Caching.StackExchangeRedis`**, **`TickerQ.Instrumentation.OpenTelemetry`**, **`TickerQ.SourceGenerator`**, **`TickerQ.RemoteExecutor`**, plus third-party integrations (for example **`Volo.Abp.*`**, **`eQuantic.TickerQ.*`**). For API text, use [tickerq.net](https://tickerq.net/) and the [TickerQ](https://github.com/Arcenox-co/TickerQ) repository. To produce **`TickerQ.xml`** locally: clone that repo, set **`<GenerateDocumentationFile>true</GenerateDocumentationFile>`** in a **`Directory.Build.props`** (or per project), run **`dotnet build`**, then open the built **`bin`** output or switch your app to a **`ProjectReference`** so the IDE loads XML next to the DLL.

## Sagas and state machines

**Goal:** model **long-running business processes** that survive restarts, accept **multiple inbound message types**, and advance **explicit phases** (a state machine) while publishing follow-up work through **`ITransponderBus`**.

**Building blocks (Abstractions + Core):**

| Type | Role |
|------|------|
| **`ITransponderSagaStore`** | Durable read/write for **`TransponderSagaRecord`** ( **`SagaKind`**, **`InstanceKey`**, **`StateName`**, **`StateJson`**, **`Version`**, **`IsCompleted`** ). Implement with EF Core / SQL / Redis for production. |
| **`InMemoryTransponderSagaStore`** | Dev/test store (single-process; global lock inside the store). |
| **`ITransponderSagaMessageHandler<TState, TMessage>`** | One handler per message contract: **`GetInstanceKey`**, then **`HandleAsync`** receives current **`TState`**, phase name, **`ITransponderSagaMessageMutator<TState>`**, and **`ConsumeContext<TMessage>`** (use **`consumeContext.Bus`** for commands/events and compensations). |
| **`TransponderSagaConsumer<…>`** | Registered as **`IConsumer<TMessage>`** — loads state, runs the handler, commits **`Update`** or **`Complete`**, enforces **optimistic concurrency** on **`Version`**, and serializes all deliveries for the same **`(SagaKind, InstanceKey)`** across message types. |
| **`SagaStateMachine<TState>`** | Optional **`enum`** transition table (**`When<TMessage>(from, to)`**) to keep phase changes explicit inside handlers. |

**Registration:** inside **`AddTransponder`**, call **`builder.AddSagaMessageHandler<TSagaState, TMessage, TSagaHandler>()`** for each message type that participates in the saga. The first registration adds **`InMemoryTransponderSagaStore`** only if **`ITransponderSagaStore`** is not already registered.

**Production (multi-node):** after **`AddDbContext<TContext>`** (or your host’s equivalent), call **`services.AddTransponderEfSagaStore<TContext>()`** from `Transponder.Persistence.EntityFrameworkCore.Shared` **before** **`AddTransponder`** so sagas use the **`SagaInstances`** table with optimistic concurrency on **`Version`** and a unique index on **`(SagaKind, InstanceKey)`**. Apply the **`AddTransponderSagaInstances`** migration in your chosen provider project (**SqlServer** or **Postgresql**). Keep the in-memory store for isolated tests only.

**Idempotency:** combine sagas with the **inbox** (**`ITransponderInboxGate`**) so redelivered commands do not double-apply side effects.

## Routing slips (itinerary activities)

**Goal:** run a **fixed sequence of named activities** with **durable progress** stored in the same **`SagaInstances`** table as sagas, using a dedicated **`SagaKind`** (**`TransponderRoutingSlipPersistenceKind.SagaKind`**) so rows stay distinct from application sagas. Activities may optionally implement **`IRoutingSlipCompensatableActivity`** so prior successful steps are **compensated in reverse order** when a later **`ExecuteAsync`** faults.

**Building blocks (Abstractions + Core):**

| Type | Role |
|------|------|
| **`TransponderRoutingSlipState`** | Serializable slip: **`Itinerary`**, **`CurrentIndex`**, **`CompletedActivities`**, **`Variables`**, optional **`CorrelationId`**. |
| **`TransponderRoutingSlipContinue`** | Internal message: **`SlipId`** + **`StepIndex`** (must match persisted **`CurrentIndex`** so stale redeliveries are ignored). |
| **`IRoutingSlipActivity`** | **`Name`** + **`ExecuteAsync`** for one step. |
| **`IRoutingSlipCompensatableActivity`** | Optional **`CompensateAsync`** after a later fault. |
| **`IRoutingSlipActivityContext`** | **`SlipId`** (same value as event **`TrackingNumber`**), **`ConsumeContext`**, **`Bus`**, **`CurrentActivity`**, **`Variables`**, **`SetVariable`**. |
| **`ITransponderRoutingSlipStarter`** | **`StartAsync(itinerary, publishOptions?)`** — inserts saga row (**`TryInsertAsync`**), publishes first **`TransponderRoutingSlipContinue`**. |
| **`TransponderRoutingSlipContinueConsumer`** | Loads state under the slip lock, runs one activity, **`TryUpdateAsync`** / **`DeleteAsync`**, enqueues **routing slip event** publishes, **`finally`** releases the slip lock, then runs the queued publishes and the next **`Continue`** (terminal/fault paths use **`goto`** after enqueueing so a **`return` inside `try` cannot skip the deferred work). |

**Routing slip events (monitoring / projections):** each event includes **`TrackingNumber`** (the slip id returned from **`StartAsync`**) and **`TimestampUtc`** (**`DateTimeOffset.UtcNow`** at publish time). **`AddRoutingSlipActivity`** registers **discarding no-op** consumers for every routing slip event type so **`ITransponderBus`** always has a route; add your own **`AddConsumer<TEvent, THandler>()`** (or transport **`Listen<TEvent>()`**) to observe or persist events—**`GetServices<IConsumer<TEvent>>`** invokes **all** registered consumers in registration order.

| Event | When |
|-------|------|
| **`TransponderRoutingSlipActivityCompleted`** | After a successful **`TryUpdateAsync`** (intermediate step) or after the final activity before the row is deleted. |
| **`TransponderRoutingSlipActivityFaulted`** | Missing registration, or **`ExecuteAsync`** threw. |
| **`TransponderRoutingSlipActivityCompensated`** | **`CompensateAsync`** succeeded for one prior step. |
| **`TransponderRoutingSlipActivityCompensationFailed`** | **`CompensateAsync`** threw for one prior step. |
| **`TransponderRoutingSlipCompensationFailed`** | At least one compensation step failed (published before **`TransponderRoutingSlipFaulted`** when applicable). |
| **`TransponderRoutingSlipCompleted`** | Entire itinerary finished without fault. |
| **`TransponderRoutingSlipFaulted`** | Terminal fault (after compensation attempts when a step faulted). |

Event publishes use **`TransponderPublishOptions.DeduplicationId`** values prefixed with **`{TrackingNumber}:evt:`** so they compose with the **inbox** the same way as **`Continue`** messages. Applications can persist these events keyed by **`TrackingNumber`** for history or drive an Automatonymous-style state machine off the bus.

**Registration:** inside **`AddTransponder`**, call **`builder.AddRoutingSlipActivity<TActivity>(activityName?)`** for each activity (name defaults to **`typeof(TActivity).Name`**). The first call on a given **`IServiceCollection`** registers **`ITransponderRoutingSlipStarter`**, **`ITransponderSagaStore`** (in-memory only if not already registered), **`TransponderRoutingSlipContinue`** consumer, and **`TransponderRoutingSlipOptions`**.

**Production (multi-node):** register **`EntityFrameworkCoreTransponderSagaStore<TContext>`** (same as sagas) **before** **`AddTransponder`** so slip state is durable across processes. No extra migrations: slips reuse **`SagaInstances`**.

**Broker transports:** subscribe to **`TransponderRoutingSlipContinue`** the same way as other contracts (for example **`Listen<TransponderRoutingSlipContinue>()`** on RabbitMQ/NATS/etc.).

**Deduplication and inbox:** each follow-up publish sets **`TransponderPublishOptions.DeduplicationId`** to **`{slipId}:step-{stepIndex}`**; the starter uses **`{slipId}:step-0`**. Routing slip **events** use **`{TrackingNumber}:evt:…`** (see table above). Combine with **`ITransponderInboxGate`** so redelivered **`Continue`** or duplicate event deliveries do not double-apply side effects.

## Repository layout

- `Transponder.Abstractions` / `Transponder.Core` — core programming model, in-process bus, **sagas** (`Sagas/` types and **`TransponderSagaBuilderExtensions`**), **routing slips** (`RoutingSlips/` and **`TransponderRoutingSlipBuilderExtensions`**).
- `Transponder/Tests/Dialysis.BuildingBlocks.Transponder.Tests` — unit tests for Transponder.Core (including routing slips).
- `Transport/` — plugin class libraries; **RabbitMQ**, **NATS**, **Azure Service Bus**, **Amazon SQS**, **gRPC**, **SignalR**, and **Server-Sent Events** are implemented.
- `Persistence/` — **`Transponder.Persistence.EntityFrameworkCore.Shared`** (options + model), then **SqlServer** or **Postgresql** (migrations + `DbContext`; pick one provider per host).
- `Scheduling/` — **`Transponder.Scheduling.Hangfire`**, **`Transponder.Scheduling.Quartz`**, and **`Transponder.Scheduling.TickerQ`** each implement **`ITransponderMessageScheduler`** (pick one per host).

NuGet versions are centralized in repo `Directory.Packages.props`; each plugin references only the packages it needs.

## EF Core migrations

**Schema and history:** The client chooses a database schema via **`TransponderPersistenceOptions.Schema`**. **`OutboxMessages`**, **`InboxMessages`**, and **`__EFMigrationsHistory`** are created in that same schema. Use **one** provider per host (SQL Server **or** PostgreSQL).

**Configuration (`TransponderPersistenceOptions`):**

| Member | Role |
|--------|------|
| **`Schema`** | Required. Your schema name (e.g. `transponder`, `his_messaging`, `dbo`). |
| **`ConnectionString`** | Optional. When set, used as the ADO.NET connection string. |
| **`ConnectionStringName`** | When **`ConnectionString`** is empty, **`IConfiguration.GetConnectionString(ConnectionStringName)`** is used (default name **`TransponderPersistence`**). |

**Registration (pragmatic):**

- From **`IConfiguration`** (typical ASP.NET Core / host): binds section **`Transponder:Persistence`** by default.

  ```csharp
  services.AddTransponderSqlServerPersistence(configuration);
  // or: services.AddTransponderPostgreSqlPersistence(configuration);

  // appsettings.json example:
  // "Transponder": { "Persistence": { "Schema": "his_messaging", "ConnectionStringName": "HisDatabase" } },
  // "ConnectionStrings": { "HisDatabase": "..." }
  ```

- In code: **`AddTransponderSqlServerPersistence(services, (TransponderPersistenceOptions o) => { ... })`** or **`AddTransponderSqlServerPersistence(services, connectionString, schema)`**.

- **`TransponderPersistenceConfiguration.ResolveConnectionString`** documents resolution order when you use the configuration overload.

**Apply migrations:** after `var app = builder.Build();`, **`await app.Services.ApplyTransponderSqlServerPersistenceMigrationsAsync();`** (or the PostgreSQL equivalent), or run **`dotnet ef database update`** in CI.

**EF limitation:** Shipped migration SQL embeds the **schema name that was active when the migration was generated**. When you maintain migrations from source, set **`Transponder:Persistence:Schema`** in the same configuration you use for **`dotnet ef`**. Runtime **`Schema`** must **match** those migrations, **or** regenerate migrations for a different schema.

**Design-time (`dotnet ef`):** **`TransponderPersistenceDbContextDesignTimeFactory`** uses **`TransponderPersistenceDesignTimeConfiguration.Build()`** to load client configuration: optional **`appsettings.json`** and **`appsettings.{Environment}.json`** from the **process working directory**, then **environment variables** (including hierarchical keys such as **`Transponder__Persistence__Schema`**). It binds **`Transponder:Persistence`** and resolves the connection string with **`TransponderPersistenceConfiguration.ResolveConnectionString`** — **no hardcoded connection strings** in the factory.

Use **`dotnet ef --startup-project &lt;your client host&gt;`** so the working directory and **`appsettings`** match the application that already defines **`ConnectionStrings`** and **`Transponder:Persistence`**.

```bash
dotnet ef migrations add <Name> \
  --project src/backend/BuildingBlocks/Transponder/Persistence/Transponder.Persistence.EntityFrameworkCore.SqlServer/Transponder.Persistence.EntityFrameworkCore.SqlServer.csproj \
  --startup-project path/to/Your.Host.csproj \
  --context TransponderPersistenceDbContext \
  --output-dir Migrations
```

(PostgreSQL: swap the project path for **`Transponder.Persistence.EntityFrameworkCore.Postgresql`**.)

**Model:** **`TransponderOutboxMessageEntity`** and **`TransponderInboxMessageEntity`** in the Shared project — outbox has nullable **`W3CTraceParent`** and **`CorrelationId`** (max 128 each); inbox has a unique **`DeduplicationKey`** (max 256).

**Note:** Bounded-context–specific outboxes (for example HIS **`his_outbox`**) stay separate until you align on this table and **`TransponderPersistenceDbContext`**.

## Transactional outbox and inbox

**Outbox (publisher):** inject **`ITransponderOutbox`** in the same unit of work as your EF **`DbContext`**. Call **`EnqueueAsync`** with a **`TransponderOutboxEnvelope`** (assembly-qualified type name, JSON payload, optional id / trace / correlation), then **`SaveChanges`** on that context so the row commits with domain state. **`TransponderOutboxWriter<TContext>`** does not call **`SaveChanges`** itself.

**Relay:** register **`AddTransponderOutboxRelay<TContext>()`** (optional **`Action<TransponderOutboxRelayOptions>`** for poll interval and batch size). The hosted service loads unprocessed rows (**`ProcessedAtUtc`** null), deserializes the payload, then calls **`ITransponderBus.PublishPreparedAsync`** with the CLR routing key (**`Type.FullName`**), then marks **`ProcessedAtUtc`**. Run **one** relay per database (or coordinate externally). The host must load the contract type: **`Type.GetType(AssemblyQualifiedEventType)`** is used only to resolve the row’s type name to a **`Type`** for JSON deserialization and routing (no generic reflection on publish).

**Inbox (consumer):** register **`AddTransponderEfOutboxAndInbox<TContext>()`** (or register **`ITransponderInboxGate`** manually). When a transport supplies **`TransportMessage.DeduplicationId`** (RabbitMQ **`MessageId`** / correlation fallback; NATS header **`TransponderTransportHeaderNames.DeduplicationId`**), **`TransponderConsumeDispatcher`** acquires the inbox before handlers, completes on success, or abandons on failure so redelivery can retry. **`ConsumeContext<T>`** exposes **`DeduplicationId`**. Handlers should be **idempotent** when the same key can be redelivered after a crash before **`CompleteAsync`** (inbox allows a second run for an incomplete row).

### SQL Server

Migrations: **`Persistence/Transponder.Persistence.EntityFrameworkCore.SqlServer/Migrations/`**.

### PostgreSQL

Migrations: **`Persistence/Transponder.Persistence.EntityFrameworkCore.Postgresql/Migrations/`** (`Npgsql.EntityFrameworkCore.PostgreSQL` version in `Directory.Packages.props`).

## Transport seam (`Transponder.Abstractions`)

- **`ITransponderBus`** — `PublishAsync<T>(...)`, `PublishPreparedAsync(routingKey, object, TransponderPublishOptions, ...)`, and **`PublishLargeAsync<T>(T, TransponderLargeMessageOptions?, ...)`** for payloads that exceed broker or host limits. Large sends serialize **once**, compute **SHA-256** over the full JSON bytes, split into **`TransponderMessageChunk`** frames (same digest on every frame), reuse one **`CorrelationId`** across chunks (from **`TransponderLargeMessageOptions.CorrelationId`** or a generated id), and set a **distinct `TransponderPublishOptions.DeduplicationId` per chunk** so RabbitMQ **`MessageId`** / JetStream **`MsgId`** deduplication cannot drop later segments. If the serialized size is at or below **`MaxSegmentBytes`**, a single **`PublishAsync<T>`** is used (no chunk envelope). **`AddTransponder`** registers the chunk route and a singleton **`TransponderChunkReassemblyConsumer`**: it buffers by **`ChunkSessionId`**, rejects conflicting metadata, verifies the digest on the merged bytes, then dispatches the logical routing key so your **`IConsumer<T>`** runs unchanged. Configure limits and **`IncompleteSessionTimeout`** via **`services.Configure<TransponderLargeMessageOptions>(...)`** (defaults: 256 KiB segments, 100 MiB total, 10k chunks, 10 minute incomplete TTL). **Do not** register another **`IConsumer<TransponderMessageChunk>`** unless you intend to run multiple handlers. Built-in **`AddTransponder*`** transport extensions (RabbitMQ, NATS, Azure Service Bus, SQS, gRPC, SignalR, Server-Sent Events) call **`Listen<TransponderMessageChunk>()`** automatically so the consumer receives chunk frames.
- **`TransponderMessageChunk`** / **`TransponderLargeMessageOptions`** — wire contract and publisher limits (strongly typed; no reflection on the logical contract).
- **`ConsumeContext<T>`** — includes **`CorrelationId`** and optional **`DeduplicationId`** when the transport supplied them.
- **`ITransponderTransport`** — connect, `PublishAsync(TransportMessage)`, and `RunConsumerAsync` for broker-backed apps. **`TransportMessage`** can carry optional **`Headers`** (including `TransponderTransportHeaderNames.RoutingKey`, **`CorrelationId`**, and **`DeduplicationId`**).
- **`IMessageSerializer`** — JSON (default in `Transponder.Core`: `SystemTextJsonMessageSerializer`), including **`Serialize(Type, object)`** for prepared publishes.
- **`ITransponderConsumeRouteInvoker`** — compiled dispatch table for inbound routing keys (built from **`AddConsumer<T,...>`** and each transport’s **`Listen<T>()`** via **`TransponderConsumeRouteRegistration.Register<T>`**). No runtime reflection for consumer invocation.
- **`TransponderConsumeDispatcher`** (Core) — uses the route invoker, then runs `IConsumer<T>` instances inside a scope.
- **`TransponderServiceCollectionExtensions.RemoveDescriptorsFor`** (Core) — removes prior `ITransponderBus` registrations so a transport can replace the in-process bus.
- **`ITransponderMessageScheduler`** — optional deferred / recurring **`ITransponderBus`** publishes (see **Supported scheduling**). Implementations snapshot the message with **`IMessageSerializer`** and call **`PublishPreparedAsync`** when the job fires.
- **`ITransponderSagaStore`** / **`ITransponderSagaMessageHandler<TState, TMessage>`** — durable saga instances and per-message orchestration (see **Sagas and state machines**). Production hosts use **`AddTransponderEfSagaStore<TContext>()`** + **`SagaInstances`**; tests may rely on **`InMemoryTransponderSagaStore`**.

Register **one** broker transport per host (for example **`AddTransponderRabbitMq`**, **`AddTransponderNats`**, **`AddTransponderAzureServiceBus`**, **`AddTransponderAwsSqs`**, **`AddTransponderGrpc`**, **`AddTransponderSignalR`**, or **`AddTransponderServerSentEvents`** — do not mix two buses in one host). Optionally register **one** **`ITransponderMessageScheduler`** implementation (**`AddTransponderHangfireScheduling`**, **`AddTransponderQuartzScheduling`**, or **`AddTransponderTickerQScheduling`**).

### RabbitMQ

1. Call **`AddTransponder`** first (consumers, serializer, dispatcher, default in-memory bus).
2. Call **`AddTransponderRabbitMq`** from `Dialysis.BuildingBlocks.Transponder.Transport.RabbitMq` with **`Action<TransponderRabbitMqOptions>`** and optional **`Action<RabbitMqSubscriptionBuilder>`** using **`Listen<TMessage>()`** for each type this host should consume.

**Routing:** keys are **`typeof(TMessage).FullName`**, aligned with `PublishAsync<TMessage>`. Exchange is **direct**; name from **`TransponderRabbitMqOptions.ExchangeName`** (default `transponder`). Queue from **`QueueName`** (default `transponder.default`).

**Publisher confirms:** **`PublisherConfirmsEnabled`** (default `true`) opens the publish channel with RabbitMQ.Client publisher confirmations so **`BasicPublishAsync`** can surface broker nacks as exceptions. Set to `false` if you do not need confirm semantics.

**Poison messages / DLX:**

- **`PoisonMessagePolicy`** — **`Requeue`** (default): `BasicNack` with `requeue=true` so the broker redelivers. **`DeadLetter`**: `requeue=false`; if the queue is declared with a dead-letter exchange, failed messages are routed there instead of being dropped.
- When **`DeadLetterFanoutExchangeName`** and **`DeadLetterQueueName`** are set, the transport declares a durable fanout DLX, a DLQ bound to it, and the main queue uses **`x-dead-letter-exchange`** pointing at that fanout so poisoned messages land in the DLQ when policy is **`DeadLetter`**.

**Correlation:** pass **`new TransponderPublishOptions(CorrelationId: "...")`** on publish; the transport sets AMQP **`CorrelationId`** / **`MessageId`** and headers as appropriate.

### NATS

1. Call **`AddTransponder`** first.
2. Call **`AddTransponderNats`** from `Dialysis.BuildingBlocks.Transponder.Transport.Nats` with **`Action<TransponderNatsOptions>`** and optional **`Action<NatsSubscriptionBuilder>`** with **`Listen<TMessage>()`** for each consumed type.

**Routing:** all publishes go to **`IngressSubject`** (default `transponder.send`). The CLR routing key (`typeof(TMessage).FullName`) is sent in headers under **`TransponderTransportHeaderNames.RoutingKey`**; correlation uses **`TransponderTransportHeaderNames.CorrelationId`**.

**Options:** **`Url`**, **`ClientName`**, **`IngressSubject`**, optional **`QueueGroup`** (core queue subscriber or JetStream **`deliver_group`**), **`PoisonSubject`**, **`PoisonMessagePolicy`** (**`Log`** = ack and discard on handler failure in JetStream; **`Republish`** = publish to poison then ack), **`DeliveryMode`** (**`Core`** default, **`JetStream`**).

**JetStream:** set **`DeliveryMode = NatsDeliveryMode.JetStream`**, **`JetStreamStream`**, and **`JetStreamDurable`**. The stream must capture **`IngressSubject`** (and **`PoisonSubject`** if you use **`Republish`**), or set **`JetStreamAutoProvision = true`** so the transport issues **`CreateOrUpdateStreamAsync`** on connect (fine for dev; production often manages streams separately). Publishing uses JetStream **`PublishAsync`** with ack (**`EnsureSuccess`**). Consuming uses **`CreateOrUpdateConsumerAsync`** and **`ConsumeAsync`** with **`AckAsync`** after a successful handler; on failure, **`Republish`** publishes to **`PoisonSubject`** then acks the original, while **`Log`** acks to stop redelivery. Optional **`NatsJSPubOpts.MsgId`** is set from **`TransportMessage.DeduplicationId`**.

### Azure Service Bus

1. Provision a **topic** and a **subscription** (for example topic **`transponder`**, subscription **`default`**). Grant the connection string **send** on the topic and **listen** on the subscription.
2. Call **`AddTransponder`** first.
3. Call **`AddTransponderAzureServiceBus`** from `Dialysis.BuildingBlocks.Transponder.Transport.AzureServiceBus` with **`Action<TransponderAzureServiceBusOptions>`** and optional **`Action<AzureServiceBusSubscriptionBuilder>`** with **`Listen<TMessage>()`** for each consumed type.

**Routing:** all publishes go to **`TopicName`**. **`TransponderTransportHeaderNames`** values are copied to **`ServiceBusMessage.ApplicationProperties`**; **`Subject`** is also set to the CLR routing key for operator visibility. **`MessageId`** carries deduplication when present. The processor **completes** after a successful handler and **abandons** on failure (message becomes available again after lock duration).

**Options:** **`ConnectionString`**, **`TopicName`**, **`SubscriptionName`**, **`PrefetchCount`**, **`MaxConcurrentCalls`**.

### Amazon SQS

1. Create a **standard** SQS queue. Configure IAM credentials (environment, profile, or role) and optionally **`TransponderAwsSqsOptions.RegionName`**.
2. Call **`AddTransponder`** first.
3. Call **`AddTransponderAwsSqs`** from `Dialysis.BuildingBlocks.Transponder.Transport.AwsSqsSns` with **`Action<TransponderAwsSqsOptions>`** and optional **`Action<AwsSqsSubscriptionBuilder>`** with **`Listen<TMessage>()`** for each consumed type.

**Routing:** message attributes use the same names as **`TransponderTransportHeaderNames`**. The body is UTF-8 JSON (same as other transports). Successful handlers **`DeleteMessage`**; failures leave the message to retry after visibility timeout. Payloads larger than the SQS body limit must use **`PublishLargeAsync`** (chunked **`TransponderMessageChunk`** frames).

**Options:** **`QueueUrl`**, optional **`RegionName`**, **`WaitTimeSeconds`** (long polling, max 20), **`MaxNumberOfMessages`** (per receive, max 10).

### gRPC

Two roles: an **ASP.NET Core ingress relay** (fan-out hub) and **client hosts** that publish and subscribe over HTTP/2.

**Relay (ingress) host**

1. Reference `Dialysis.BuildingBlocks.Transponder.Transport.Grpc` and configure **Kestrel** for **HTTP/2** (TLS in production).
2. **`services.AddTransponderGrpcIngressServer(o => { ... })`** then **`app.MapTransponderGrpcIngress()`** (or **`endpoints.MapTransponderGrpcIngress()`**). The overload configures **`TransponderGrpcIngressOptions`**: **`MaxReceiveMessageSizeBytes`**, **`MaxSendMessageSizeBytes`** (defaults 32 MiB, applied to **`GrpcServiceOptions`**), and **`EnableDetailedErrors`** (default false; keep false outside local dev).
3. **Authentication / authorization:** register an **`ITransponderGrpcIngressAuthorizer`** implementation. It runs at the start of **`Publish`** and **`Subscribe`**; throw **`RpcException`** with **`Unauthenticated`** or **`PermissionDenied`** to reject a call.
4. **Durability (publisher path):** the hub still fans out **in memory** only. For durability **before** fan-out, register **`ITransponderGrpcIngressPublishJournal`**: **`AppendAsync`** runs after auth and **before** **`TransponderGrpcIngressHub`** broadcast so you can append to a database, append-only log, or hand off to a broker-backed outbox. **Replay** to late subscribers is **not** built in—you own recovery (for example re-read your journal, or use a broker transport instead of the pure gRPC relay).
5. **TLS:** terminate HTTPS in Kestrel (or a reverse proxy) with a valid server certificate. Clients use **`https://`** **`Address`** by default. For **local dev only**, **`TransponderGrpcClientOptions.ForDevelopmentOnly_DisableCertificateValidation`** skips TLS server validation (never enable in production).
6. **Very large payloads:** raise **`TransponderGrpcIngressOptions`** / **`TransponderGrpcClientOptions`** message size limits if needed, or prefer **`PublishLargeAsync`** so each wire frame stays smaller.

**Client application hosts**

1. Call **`AddTransponder`** first.
2. Call **`AddTransponderGrpc`** from `Dialysis.BuildingBlocks.Transponder.Transport.Grpc` with **`Action<TransponderGrpcClientOptions>`** (set **`Address`** to the relay base URL, e.g. `https://relay:8443`; align **`MaxReceiveMessageSizeBytes`** / **`MaxSendMessageSizeBytes`** with the relay) and optional **`Action<GrpcSubscriptionBuilder>`** with **`Listen<TMessage>()`** for each consumed type.

**Wire contract:** `Protos/transponder_ingress.proto` defines **`TransponderIngress.Publish`** (unary) and **`TransponderIngress.Subscribe`** (server streaming). Each **`TransportEnvelope`** carries **`routing_key`**, **`payload`**, **`correlation_id`**, **`deduplication_id`**, **`content_type`**, and optional **`headers`**. The relay **`TransponderGrpcIngressHub`** delivers a **clone** of each published envelope to every active subscribe stream.

### SignalR

Two roles: an **ASP.NET Core ingress hub** and **client hosts** (workers, services, or browsers) using **`Microsoft.AspNetCore.SignalR.Client`**.

**Ingress host**

1. Reference `Dialysis.BuildingBlocks.Transponder.Transport.SignalR`.
2. **`services.AddSignalR()`** (or your existing SignalR setup), then **`services.AddTransponderSignalRIngressServer(o => { ... })`** to bind **`TransponderSignalRIngressOptions`** and apply **`MaximumReceiveMessageSizeBytes`** (default 32 MiB) to **`HubOptions`**.
3. **`app.MapTransponderSignalRHub()`** maps **`TransponderSignalRHub`** at **`TransponderSignalRHub.MapPath`** (default **`/hubs/transponder`**). Configure **CORS** and **WebSockets** as needed for browser clients.
4. **Auth:** register **`ITransponderSignalRAuthorizer`** to gate **`Publish`** and new connections (**`AuthorizeSubscribeAsync`**). You can also apply **`[Authorize]`** on a **subclass** of **`TransponderSignalRHub`** if you prefer ASP.NET Core identity on the hub type you map.
5. **Durability before fan-out:** register **`ITransponderSignalRPublishJournal`** (**`AppendAsync`** runs after publish authorization and before **`Clients.All.SendAsync`**). Fan-out remains **in-process**; late joiners do not receive history unless you build replay.
6. **Large payloads:** align hub **`MaximumReceiveMessageSizeBytes`** with your JSON size, or use **`PublishLargeAsync`**.

**Client hosts**

1. Call **`AddTransponder`** first.
2. Call **`AddTransponderSignalR`** with **`Action<TransponderSignalRClientOptions>`** and optional **`Action<SignalRSubscriptionBuilder>`** with **`Listen<TMessage>()`** for each consumed type (chunk route is registered automatically).

**Client URL:** Set **`TransponderSignalRClientOptions.HubUrl`** to the full hub URL (for example `https://localhost:5001/hubs/transponder`). Use **`AccessTokenProvider`** when the hub is authenticated.

The transport uses **`WithAutomaticReconnect()`**; inbound messages are buffered in a channel until **`RunConsumerAsync`** drains them.

**Wire contract:** **`TransponderSignalREnvelopeDto`** (`RoutingKey`, `Payload`, `CorrelationId`, `DeduplicationId`, `ContentType`, `Headers`). Server hub method **`Publish`**, client callback **`Receive`**.

### Server-Sent Events (SSE)

Two roles: an **ASP.NET Core ingress relay** (fan-out over **`text/event-stream`**) and **client hosts** using **`HttpClient`**.

**Ingress usage:** `services.AddTransponderSseIngressServer(...)` then `app.MapTransponderSseIngress()`. **Client usage:** `AddTransponderServerSentEvents(o => { o.BaseAddress = "https://host/"; }, b => b.Listen<MyEvent>());` after `AddTransponder`.

**Ingress host**

1. Reference `Dialysis.BuildingBlocks.Transponder.Transport.ServerSentEvents`.
2. **`services.AddTransponderSseIngressServer(o => { ... })`** to configure **`TransponderSseIngressOptions`** (notably **`PathPrefix`**, default **`/transponder/sse`**). Maps **`{PathPrefix}/publish`** (POST JSON body **`TransponderSseEnvelopeDto`**) and **`{PathPrefix}/subscribe`** (GET SSE).
3. **`app.MapTransponderSseIngress()`** after **`WebApplication`/`app`** is built. The publish endpoint uses **`.DisableAntiforgery()`** so machine clients can POST without antiforgery tokens.
4. **Auth:** register **`ITransponderSseAuthorizer`** to gate **`AuthorizePublishAsync`** / **`AuthorizeSubscribeAsync`** (each receives **`HttpContext`**).
5. **Durability before fan-out:** register **`ITransponderSsePublishJournal`** (**`AppendAsync`** runs after publish authorization and before **`TransponderSseIngressRelay`** broadcast). Fan-out remains **in-process**; late subscribers do not receive history unless you build replay.
6. **Large payloads:** each SSE event is one JSON line in **`data:`**; very large bodies may be awkward for intermediaries—prefer **`PublishLargeAsync`** so each event stays smaller.

**Client hosts**

1. Call **`AddTransponder`** first.
2. Call **`AddTransponderServerSentEvents`** with **`Action<TransponderSseClientOptions>`** and optional **`Action<SseSubscriptionBuilder>`** with **`Listen<TMessage>()`** for each consumed type (chunk route is registered automatically).

**Client base URL:** Set **`TransponderSseClientOptions.BaseAddress`** to the relay origin with a trailing slash (for example `https://localhost:5001/`). Relative paths default to **`transponder/sse/publish`** and **`transponder/sse/subscribe`** (**`PublishPath`** / **`SubscribePath`**). Use **`AccessTokenProvider`** when the ingress requires a Bearer token.

**Wire contract:** **`TransponderSseEnvelopeDto`** (same shape as SignalR: **`RoutingKey`**, **`Payload`**, **`CorrelationId`**, **`DeduplicationId`**, **`ContentType`**, **`Headers`**). Events are standard SSE **`data:`** frames with JSON per event.

### Adding another transport

Follow the same pattern as RabbitMQ, NATS, Azure Service Bus, SQS, gRPC, SignalR, or SSE: implement **`ITransponderTransport`**, an **`ITransponderBus`** (including **`PublishPreparedAsync`** and **`PublishLargeAsync<T>`** delegating to **`TransponderLargeMessagePublisher`**), a **hosted consumer** that calls **`TransponderConsumeDispatcher`**, a subscription registry/builder that calls **`TransponderConsumeRouteRegistration.Register<T>(services)`** for each **`Listen<T>`** (and **`Listen<TransponderMessageChunk>()`** if you want parity with the built-in transports), and an extension that calls **`RemoveDescriptorsFor`** then registers transport-specific services.
