using Dialysis.HIE.Consent.Domain;
using Dialysis.HIE.Core.Abstraction.Consent;
using Dialysis.HIE.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Dialysis.HIE.Tests.Consent;

/// <summary>
/// The consent gate honours the TEFCA permitted purpose: a purpose-less ("wildcard") consent
/// applies to any request, while a purpose-scoped consent only honours a matching purpose.
/// </summary>
public sealed class ConsentGatePurposeTests
{
    private const string Partner = "default";

    [Fact]
    public async Task Wildcard_Consent_Honours_Any_Purpose_Async()
    {
        await using var factory = new HieWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var patientId = Guid.NewGuid();

        var db = sp.GetRequiredService<HieDbContext>();
        // purpose: null → wildcard.
        db.Consents.Add(new ConsentRecord(
            patientId, Partner, ConsentScopes.Demographics, ConsentDirection.Outbound,
            DateTime.UtcNow.AddMinutes(-1), effectiveToUtc: null, purpose: null));
        await db.SaveChangesAsync();

        var gate = sp.GetRequiredService<IConsentGate>();
        (await gate.CheckOutboundAsync(patientId, Partner, ConsentScopes.Demographics, "Treatment")).ShouldBeTrue();
        (await gate.CheckOutboundAsync(patientId, Partner, ConsentScopes.Demographics, "PublicHealth")).ShouldBeTrue();
    }

    [Fact]
    public async Task Purpose_Scoped_Consent_Only_Honours_Matching_Purpose_Async()
    {
        await using var factory = new HieWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var patientId = Guid.NewGuid();

        var db = sp.GetRequiredService<HieDbContext>();
        db.Consents.Add(new ConsentRecord(
            patientId, Partner, ConsentScopes.Labs, ConsentDirection.Outbound,
            DateTime.UtcNow.AddMinutes(-1), effectiveToUtc: null, purpose: "Treatment"));
        await db.SaveChangesAsync();

        var gate = sp.GetRequiredService<IConsentGate>();
        (await gate.CheckOutboundAsync(patientId, Partner, ConsentScopes.Labs, "Treatment")).ShouldBeTrue();
        // Mismatched purpose: the disclosure is suppressed.
        (await gate.CheckOutboundAsync(patientId, Partner, ConsentScopes.Labs, "Payment")).ShouldBeFalse();
    }
}
