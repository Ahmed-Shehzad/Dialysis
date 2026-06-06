using Dialysis.BuildingBlocks.ClinicianNotification;
using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.CQRS.Queries;
using Dialysis.EHR.ClinicalNotes.Features.QualityMeasures;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.EHR.Tests.ClinicalNotes;

public sealed class NotifyAtRiskCohortTests
{
    private static PopulationControlResult ControlResult() => new(
        "HTN-BP", "Hypertension: BP controlled", InCohort: 3, Controlled: 1, Uncontrolled: 2, NoData: 0, ControlRatePercent: 33.3,
        Breakdown:
        [
            new PatientControlBreakdown(Guid.NewGuid(), "MRN-1", "A", nameof(PatientControlOutcome.Controlled), 120m),
            new PatientControlBreakdown(Guid.NewGuid(), "MRN-2", "B", nameof(PatientControlOutcome.Uncontrolled), 150m),
            new PatientControlBreakdown(Guid.NewGuid(), "MRN-3", "C", nameof(PatientControlOutcome.Uncontrolled), 160m),
        ]);

    [Fact]
    public async Task Disabled_Returns_Targets_Without_Dispatching_Async()
    {
        var dispatcher = new FakeDispatcher();
        var handler = new NotifyAtRiskCohortCommandHandler(
            new FakeGateway(ControlResult()), new FakeResolver(resolves: true),
            dispatcher, Options.Create(new OutreachOptions { Enabled = false, FallbackAddress = "ops@clinic" }));

        var result = await handler.HandleAsync(new NotifyAtRiskCohortCommand("HTN-BP"), CancellationToken.None);

        result.Targeted.ShouldBe(2); // the two uncontrolled patients
        result.Dispatched.ShouldBeFalse();
        dispatcher.Calls.ShouldBe(0);
    }

    [Fact]
    public async Task Enabled_Dispatches_Phi_Light_Requests_Async()
    {
        var dispatcher = new FakeDispatcher();
        var handler = new NotifyAtRiskCohortCommandHandler(
            new FakeGateway(ControlResult()), new FakeResolver(resolves: true),
            dispatcher, Options.Create(new OutreachOptions { Enabled = true, FallbackAddress = "ops@clinic" }));

        var result = await handler.HandleAsync(new NotifyAtRiskCohortCommand("HTN-BP"), CancellationToken.None);

        result.Dispatched.ShouldBeTrue();
        dispatcher.Calls.ShouldBe(1);
        dispatcher.LastBatch.Count.ShouldBe(2);
        // PHI minimisation: no MRN/name in the dispatched body.
        dispatcher.LastBatch.ShouldAllBe(r => !r.Body.Contains("MRN") && r.Metadata.ContainsKey("patientId"));
    }

    private sealed class FakeResolver : IOutreachContactResolver
    {
        private readonly bool _resolves;
        public FakeResolver(bool resolves) => _resolves = resolves;
        public OutreachContact? Resolve(Guid patientId) => _resolves ? new OutreachContact("webhook", "ops@clinic") : null;
    }

    private sealed class FakeDispatcher : IClinicianNotificationDispatcher
    {
        public int Calls { get; private set; }
        public IReadOnlyList<ClinicianNotificationRequest> LastBatch { get; private set; } = [];
        public Task<IReadOnlyList<ChannelOutcome>> DispatchAsync(IReadOnlyList<ClinicianNotificationRequest> requests, CancellationToken cancellationToken)
        {
            Calls++;
            LastBatch = requests;
            return Task.FromResult<IReadOnlyList<ChannelOutcome>>(
                [.. requests.Select(r => new ChannelOutcome(r.Channel, r.Address, new ClinicianNotificationResult(true, "msg", null)))]);
        }
    }

    private sealed class FakeGateway : ICqrsGateway
    {
        private readonly PopulationControlResult _result;
        public FakeGateway(PopulationControlResult result) => _result = result;
        public Task<TResponse> SendQueryAsync<TQuery, TResponse>(TQuery query, CancellationToken cancellationToken = default)
            where TQuery : IQuery<TResponse> => Task.FromResult((TResponse)(object)_result);
        public Task<TResponse> SendCommandAsync<TCommand, TResponse>(TCommand command, CancellationToken cancellationToken = default)
            where TCommand : ICommand<TResponse> => throw new NotSupportedException();
    }
}
