# Transponder.Reactive.Signals

First-party, server-grade reactive signals for Transponder — the outcome of the
SignalsDotnet evaluation in `docs/reports/signals-transponder-audit-2026-06-11.md` (rejected:
UI/MVVM-oriented, undocumented server-side thread-safety, R3 transitive dependency, bus
factor). Zero external dependencies; only centrally-pinned `Microsoft.Extensions.*`.

**Mental model:** Transponder is the source of truth for events; signals are a *local,
reactive view of the latest state*. Channels move data, the bus distributes it, signals
understand it.

## Primitives

| Type | Role | Concurrency contract |
|---|---|---|
| `Signal<T>` | Writable latest-state cell | Value+version travel in one immutable snapshot — reads are never torn. `Value = x` is last-write-wins; `Update(f)` is a CAS retry loop and never loses a read-modify-write. `f` must be pure (may rerun under contention). |
| `Computed.From(...)` / `FromMany` | Derived signal over **explicit** dependencies (no auto-tracking — that's a deliberate server-side choice) | Push-invalidated, pull-recomputed with seqlock validation: the value you read was always computed from one coherent set of dependency states. Memoized while dependency versions are unchanged. Compute must be pure; runs on the reader's thread. Dependencies must be nodes from this library. |
| `SignalBatch.Run(...)` | Glitch-free multi-write | Thread-bound and synchronous (does not flow across `await`). Writes apply immediately; notifications dedupe per signal and flush once at the outermost scope. |
| `Effect` | Reaction (gauge, log, push) | Wake channel is bounded(1)/drop-write: bursts coalesce, the reaction sees the latest state (latest-wins). Runs once at startup. Failures are logged, never rethrown — effects are sinks, never actors. Strong subscriptions; dispose to detach. |
| `SignalStore` | Singleton named-signal registry | The seam between scoped consumers and process-wide state. |
| `SignalProjection<TMessage,TState>` | `IConsumer<TMessage>` base folding deliveries into a named signal | Register via `TransponderBuilder.AddSignalProjection<TMessage,TProjection>()`. |

## Idempotency guidance for projections

Delivery is at-least-once. Reducers must tolerate replay and reordering:

- **Latest-wins assignment** (`(s, m) => m.Snapshot`) — naturally idempotent. Preferred.
- **Monotonic version guard** (`m.Seq <= s.Seq ? s : fold(s, m)`) — handles replay + reorder.
- **Bare counters are NOT safe** without a deduplication key; don't.

## Diagnostics pilot (audit §5)

`AddTransponderReactiveSignals()` registers the `SignalStore`, the diagnostics graph
(`TransponderDiagnosticsSignals`: per-transport connection states + last relay tick →
computed `BusHealth` and `OutboxLagging`), the signals-backed `ITransponderStateObserver`
(transports and the outbox relay take it as an optional constructor dependency; the default
is a no-op), and a hosted service exposing:

- meter **`Dialysis.Transponder.Signals`** — `dialysis.transponder.bus.health` (0 unknown /
  1 healthy / 2 degraded / 3 down) and `dialysis.transponder.outbox.lagging` (0/1; threshold
  `TransponderSignalsDiagnosticsOptions.OutboxLagThreshold`, default 30 s). Add the meter
  name to `ModuleTelemetryOptions.AdditionalMeters` to export it.
- a bus-health transition log effect.

The frozen `Dialysis.Transponder.Outbox` metric contract is untouched — this surface only
adds. **v2 note:** once this pilot has soaked, the `oldest_pending_age` *feed* can move
behind the observer (a metrics observer in Persistence.Shared composed via
`IEnumerable<ITransponderStateObserver>`); not done now to keep the contract byte-identical.

## What signals will NOT do (scope guard, audit §6)

- **Not a bus** — a signal never crosses a process; cross-node reactivity is always
  *integration event in → local signal graph*.
- **Not a queue** — no backpressure or per-transition delivery; effects are latest-wins.
  Consume messages when every transition matters.
- **Not durability** — signal state dies with the process; outbox/inbox stay the durability
  story. No event sourcing: projections are latest-state views, never replayable logs.
- **Not mutual exclusion** — signals derive state, they don't guard it. Chunk reassembly,
  saga locks, and the message hot path keep their locks.

## Concurrency spike status (audit §5.3 gate)

`SignalConcurrencySpikeTests` in `Dialysis.BuildingBlocks.Transponder.Reactive.Signals.Tests`
is the promotion gate — green as of 2026-06-11: parallel writers on distinct signals feeding
one computed (exact final sum, no torn intermediate reads), concurrent CAS updates on one
signal (no lost updates), torn-snapshot invariant under hammering, computed coherence under
write races. Plus: effect coalescing/no-`SynchronizationContext`/failure isolation, batch
glitch-freedom, 100k subscribe/dispose leak cycles, scoped-consumer→singleton-state folding.
