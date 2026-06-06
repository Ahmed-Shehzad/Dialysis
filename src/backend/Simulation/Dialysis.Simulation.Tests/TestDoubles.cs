using Dialysis.BuildingBlocks.Documents.Pdf;
using Dialysis.BuildingBlocks.Transponder;
using Dialysis.Simulation.Drivers;
using Dialysis.Simulation.Drivers.InMemory;
using Dialysis.Simulation.Engine.Domain;
using Dialysis.Simulation.Engine.Ports;

namespace Dialysis.Simulation.Tests;

/// <summary>In-memory session store + write store + query store + unit of work for engine tests.</summary>
internal sealed class InMemoryBackplane
    : ISimulationSessionRepository, ISimulationWriteStore, ISimulationQueryStore, Dialysis.DomainDrivenDesign.Persistence.IUnitOfWork
{
    private readonly Dictionary<Guid, SimulationSession> _sessions = [];

    public List<SimulationEventRecord> Events { get; } = [];

    public List<SimulationAuditEntry> Audits { get; } = [];

    public List<SessionRecordLink> Links { get; } = [];

    public void Add(SimulationSession session) => _sessions[session.Id] = session;

    public Task<SimulationSession?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_sessions.GetValueOrDefault(id));

    public void AppendEvent(SimulationEventRecord record) => Events.Add(record);

    public void AppendAudit(SimulationAuditEntry entry) => Audits.Add(entry);

    public void AppendLink(SessionRecordLink link) => Links.Add(link);

    public Task<SimulationSession?> GetSessionAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_sessions.GetValueOrDefault(id));

    public Task<IReadOnlyList<SimulationEventRecord>> ListEventsAsync(Guid sessionId, int take, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<SimulationEventRecord>>(
            [.. Events.Where(e => e.SimulationSessionId == sessionId).OrderByDescending(e => e.OccurredAtUtc).Take(take)]);

    public Task<IReadOnlyList<SimulationAuditEntry>> ListAuditAsync(Guid sessionId, int take, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<SimulationAuditEntry>>(
            [.. Audits.Where(a => a.SimulationSessionId == sessionId).OrderByDescending(a => a.OccurredAtUtc).Take(take)]);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
}

/// <summary>A no-op PDF renderer for engine tests (returns deterministic placeholder bytes).</summary>
internal sealed class StubPdfRenderer : IPdfDocumentRenderer
{
    public Task<byte[]> RenderAsync(DocumentModel document, CancellationToken cancellationToken) =>
        Task.FromResult(new byte[] { 0x25, 0x50, 0x44, 0x46 });

    public Task<byte[]> RenderWithFormsAsync(
        DocumentModel document,
        IReadOnlyList<Dialysis.BuildingBlocks.Documents.Pdf.AcroForms.AcroFormPlacement> formPlacements,
        CancellationToken cancellationToken) =>
        Task.FromResult(new byte[] { 0x25, 0x50, 0x44, 0x46 });
}

/// <summary>Records the integration events the engine enqueues on failure.</summary>
internal sealed class RecordingOutbox : ITransponderOutbox
{
    public List<TransponderOutboxEnvelope> Enqueued { get; } = [];

    public Task EnqueueAsync(TransponderOutboxEnvelope envelope, CancellationToken cancellationToken = default)
    {
        Enqueued.Add(envelope);
        return Task.CompletedTask;
    }
}

/// <summary>A fixed clock.</summary>
internal sealed class FixedTimeProvider : TimeProvider
{
    private DateTimeOffset _now;

    public FixedTimeProvider(DateTimeOffset now) => _now = now;

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan by) => _now = _now.Add(by);
}

/// <summary>An EHR driver that always throws — to exercise the failure path.</summary>
internal sealed class ThrowingEhrDriver : IEhrDriver
{
    public Task<RegisteredPatient> RegisterPatientAsync(RegisterPatientCommand command, DriverContext context, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("driver-boom");

    public Task<StartedEncounter> StartEncounterAsync(StartEncounterCommand command, DriverContext context, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("driver-boom");

    public Task<ClosedEncounter> CloseEncounterAsync(CloseEncounterCommand command, DriverContext context, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("driver-boom");

    public Task<RequestedReferral> RequestReferralAsync(RequestReferralCommand command, DriverContext context, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("driver-boom");
}

/// <summary>An EHR driver that fails the first registration call, then delegates — to exercise retry.</summary>
internal sealed class FlakyEhrDriver : IEhrDriver
{
    private readonly InMemoryEhrDriver _inner = new();
    private int _registerCalls;

    public int RegisterCalls => _registerCalls;

    public Task<RegisteredPatient> RegisterPatientAsync(RegisterPatientCommand command, DriverContext context, CancellationToken cancellationToken)
    {
        _registerCalls++;
        if (_registerCalls == 1)
            throw new InvalidOperationException("transient");
        return _inner.RegisterPatientAsync(command, context, cancellationToken);
    }

    public Task<StartedEncounter> StartEncounterAsync(StartEncounterCommand command, DriverContext context, CancellationToken cancellationToken) =>
        _inner.StartEncounterAsync(command, context, cancellationToken);

    public Task<ClosedEncounter> CloseEncounterAsync(CloseEncounterCommand command, DriverContext context, CancellationToken cancellationToken) =>
        _inner.CloseEncounterAsync(command, context, cancellationToken);

    public Task<RequestedReferral> RequestReferralAsync(RequestReferralCommand command, DriverContext context, CancellationToken cancellationToken) =>
        _inner.RequestReferralAsync(command, context, cancellationToken);
}
