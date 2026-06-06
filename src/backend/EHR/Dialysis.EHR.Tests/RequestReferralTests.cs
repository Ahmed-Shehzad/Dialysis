using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.ClinicalNotes.Domain;
using Dialysis.EHR.ClinicalNotes.Features.RequestReferral;
using Dialysis.EHR.ClinicalNotes.Ports;
using Dialysis.EHR.Contracts.Integration;
using Shouldly;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.EHR.Tests;

public sealed class RequestReferralTests
{
    [Fact]
    public async Task Persists_A_Referral_And_Raises_The_Integration_Event_Async()
    {
        var repo = new CapturingReferralRepository();
        var nowUtc = new DateTime(2026, 6, 6, 12, 0, 0, DateTimeKind.Utc);
        var handler = new RequestReferralCommandHandler(repo, new CountingUnitOfWork(), new FixedClock(nowUtc));

        var patient = Guid.NewGuid();
        var provider = Guid.NewGuid();
        var id = await handler.HandleAsync(
            new RequestReferralCommand(patient, "partner-nephrology", provider, "Vascular access evaluation"),
            CancellationToken.None);

        var referral = repo.Added.ShouldNotBeNull();
        referral.Id.ShouldBe(id);
        referral.PatientId.ShouldBe(patient);
        referral.DestinationPartnerId.ShouldBe("partner-nephrology");
        referral.RequestedAtUtc.ShouldBe(nowUtc);

        var evt = referral.IntegrationEvents.OfType<ReferralRequestedIntegrationEvent>().ShouldHaveSingleItem();
        evt.PatientId.ShouldBe(patient);
        evt.DestinationPartnerId.ShouldBe("partner-nephrology");
        evt.ReferringProviderId.ShouldBe(provider);
        evt.ReferralReason.ShouldBe("Vascular access evaluation");
    }

    private sealed class CapturingReferralRepository : IReferralRepository
    {
        public Referral? Added { get; private set; }
        public void Add(Referral referral) => Added = referral;
        public Task<IReadOnlyList<Referral>> ListByPatientAsync(Guid patientId, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Referral>>(Added is null ? [] : [Added]);
    }

    private sealed class CountingUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }

    private sealed class FixedClock : TimeProvider
    {
        private readonly DateTime _utcNow;
        public FixedClock(DateTime utcNow) => _utcNow = utcNow;
        public override DateTimeOffset GetUtcNow() => new(_utcNow, TimeSpan.Zero);
    }
}
