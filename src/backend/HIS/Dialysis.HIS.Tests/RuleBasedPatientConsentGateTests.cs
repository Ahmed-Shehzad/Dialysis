using Dialysis.HIS.PatientAccess;
using Dialysis.HIS.PatientAccess.Ports;
using Microsoft.Extensions.Options;

namespace Dialysis.HIS.Tests;

public sealed class RuleBasedPatientConsentGateTests
{
    private sealed class FixedReadModel(PatientPortalConsentState? state) : IPatientPortalConsentReadModel
    {
        public Task<PatientPortalConsentState?> GetAsync(Guid patientId, CancellationToken cancellationToken = default) =>
            Task.FromResult(state);
    }

    private static RuleBasedPatientConsentGate Gate(IPatientPortalConsentReadModel readModel, bool requireExplicitConsentRow = false) =>
        new(readModel, Options.Create(new PatientPortalOptions { RequireExplicitConsentRowForPortal = requireExplicitConsentRow }));

    [Fact]
    public async Task Summary_hidden_throws()
    {
        var gate = Gate(new FixedReadModel(new PatientPortalConsentState(SummaryVisible: false, AppointmentRequestsAllowed: true)));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => gate.EnsureCanViewSummaryAsync(Guid.NewGuid(), default));
    }

    [Fact]
    public async Task Missing_row_allows_summary_when_explicit_not_required()
    {
        var gate = Gate(new FixedReadModel(null));
        await gate.EnsureCanViewSummaryAsync(Guid.NewGuid(), default);
    }

    [Fact]
    public async Task Missing_row_throws_when_explicit_required()
    {
        var gate = Gate(new FixedReadModel(null), requireExplicitConsentRow: true);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => gate.EnsureCanViewSummaryAsync(Guid.NewGuid(), default));
    }

    [Fact]
    public async Task Appointments_blocked_throws_on_request_path()
    {
        var gate = Gate(new FixedReadModel(new PatientPortalConsentState(true, false)));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => gate.EnsureCanRequestAppointmentAsync(Guid.NewGuid(), default));
    }
}
