using Dialysis.CQRS;
using Dialysis.HIE.Tefca.Features;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Dialysis.HIE.Tests.Tefca;

/// <summary>
/// Guards that the <c>Dialysis.HIE.Tefca</c> slice is registered in the host's
/// <c>HandlerAssemblies</c>. When the assembly was missing, the onboarding command/query
/// handlers never resolved, so every <c>/tefca/partners</c> endpoint (GET included) returned
/// HTTP 500 — see the e2e <c>tefca-partners</c> spec.
/// </summary>
public sealed class QhinPartnerAdminTests
{
    [Fact]
    public async Task Onboard_Then_List_Qhin_Partner_Round_Trip_Async()
    {
        await using var factory = new HieWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var cqrs = scope.ServiceProvider.GetRequiredService<ICqrsGateway>();

        var id = await cqrs.SendCommandAsync<OnboardQhinPartnerCommand, Guid>(
            new OnboardQhinPartnerCommand(
                "Acme QHIN",
                "https://qhin.example/fhir",
                "https://qhin.example/ias",
                "operator"));

        var rows = await cqrs.SendQueryAsync<ListQhinPartnersQuery, IReadOnlyList<QhinPartnerRow>>(
            new ListQhinPartnersQuery());

        rows.ShouldContain(r => r.Id == id && r.Name == "Acme QHIN");
    }
}
