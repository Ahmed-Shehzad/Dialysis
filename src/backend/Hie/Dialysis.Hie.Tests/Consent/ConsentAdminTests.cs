using Dialysis.CQRS;
using Dialysis.Hie.Consent.Domain;
using Dialysis.Hie.Consent.Features.GrantConsent;
using Dialysis.Hie.Consent.Features.ListConsentsForPatient;
using Dialysis.Hie.Consent.Features.RevokeConsent;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Dialysis.Hie.Tests.Consent;

public sealed class ConsentAdminTests
{
    [Fact]
    public async Task Grant_List_Revoke_Consent_Round_Trip_Async()
    {
        await using var factory = new HieWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var cqrs = scope.ServiceProvider.GetRequiredService<ICqrsGateway>();

        var patientId = Guid.NewGuid();
        var grantId = await cqrs.SendCommandAsync<GrantConsentCommand, Guid>(
            new GrantConsentCommand(
                patientId,
                "default",
                "patient.demographics",
                ConsentDirection.Outbound,
                DateTime.UtcNow.AddMinutes(-1),
                EffectiveToUtc: null));

        var listAfterGrant = await cqrs.SendQueryAsync<ListConsentsForPatientQuery, IReadOnlyList<ConsentDto>>(
            new ListConsentsForPatientQuery(patientId));
        listAfterGrant.Count.ShouldBe(1);
        listAfterGrant[0].Id.ShouldBe(grantId);
        listAfterGrant[0].RevokedAtUtc.ShouldBeNull();

        await cqrs.SendCommandAsync<RevokeConsentCommand, Unit>(new RevokeConsentCommand(grantId));

        var listAfterRevoke = await cqrs.SendQueryAsync<ListConsentsForPatientQuery, IReadOnlyList<ConsentDto>>(
            new ListConsentsForPatientQuery(patientId));
        listAfterRevoke.Single().RevokedAtUtc.ShouldNotBeNull();
    }
}
