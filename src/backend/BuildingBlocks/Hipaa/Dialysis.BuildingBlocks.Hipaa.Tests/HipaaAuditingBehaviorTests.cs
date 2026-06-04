using Dialysis.BuildingBlocks.Fhir.Audit;
using Dialysis.BuildingBlocks.Hipaa.Audit;
using Dialysis.BuildingBlocks.Intercessor;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.BuildingBlocks.Hipaa.Tests;

public sealed class HipaaAuditingBehaviorTests
{
    [Fact]
    public async Task Emits_Audit_Event_When_Request_Is_Marked_Phi_Access_Async()
    {
        var emitter = new RecordingEmitter();
        var context = new TestContext("his", "user-42");
        var behavior = new HipaaAuditingBehavior<PhiReadQuery, string>(emitter, context, NullLogger<HipaaAuditingBehavior<PhiReadQuery, string>>.Instance);

        var response = await behavior.HandleAsync(new PhiReadQuery(), () => Task.FromResult("ok"), CancellationToken.None);

        Assert.Equal("ok", response);
        var ev = Assert.Single(emitter.Captured);
        Assert.Equal(AuditEvent.AuditEventAction.R, ev.Action);
        Assert.Equal(AuditEvent.AuditEventOutcome.N0, ev.Outcome);
        Assert.Equal("his", ev.Source.Site);
        Assert.Equal("Practitioner/user-42", Assert.Single(ev.Agent).Who?.Reference);
        Assert.Equal("Patient/PhiReadQuery", Assert.Single(ev.Entity).What?.Reference);
    }

    [Fact]
    public async Task Skips_Audit_For_Non_Phi_Access_Request_Async()
    {
        var emitter = new RecordingEmitter();
        var context = new TestContext("his", null);
        var behavior = new HipaaAuditingBehavior<NoPhiQuery, string>(emitter, context, NullLogger<HipaaAuditingBehavior<NoPhiQuery, string>>.Instance);

        await behavior.HandleAsync(new NoPhiQuery(), () => Task.FromResult("ok"), CancellationToken.None);

        Assert.Empty(emitter.Captured);
    }

    [Fact]
    public async Task Emits_Minor_Failure_Audit_When_Handler_Throws_Async()
    {
        var emitter = new RecordingEmitter();
        var context = new TestContext("his", "user-9");
        var behavior = new HipaaAuditingBehavior<PhiReadQuery, string>(emitter, context, NullLogger<HipaaAuditingBehavior<PhiReadQuery, string>>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await behavior.HandleAsync(new PhiReadQuery(), () => throw new InvalidOperationException("boom"), CancellationToken.None));

        var ev = Assert.Single(emitter.Captured);
        Assert.Equal(AuditEvent.AuditEventOutcome.N4, ev.Outcome);
        Assert.Equal("boom", ev.OutcomeDesc);
    }

    [PhiAccess(PhiAccessAction.Read, "Patient")]
    private sealed record PhiReadQuery : IRequest<string>;

    private sealed record NoPhiQuery : IRequest<string>;

    private sealed record TestContext : IHipaaAuditContext
    {
        public TestContext(string ModuleSlug, string? CurrentUserId)
        {
            this.ModuleSlug = ModuleSlug;
            this.CurrentUserId = CurrentUserId;
        }
        public string ModuleSlug { get; init; }
        public string? CurrentUserId { get; init; }
        public void Deconstruct(out string moduleSlug, out string? currentUserId)
        {
            moduleSlug = this.ModuleSlug;
            currentUserId = this.CurrentUserId;
        }
    }

    private sealed class RecordingEmitter : IAuditEventEmitter
    {
        public List<AuditEvent> Captured { get; } = [];

        public ValueTask EmitAsync(AuditEvent auditEvent, CancellationToken cancellationToken)
        {
            Captured.Add(auditEvent);
            return ValueTask.CompletedTask;
        }
    }
}
