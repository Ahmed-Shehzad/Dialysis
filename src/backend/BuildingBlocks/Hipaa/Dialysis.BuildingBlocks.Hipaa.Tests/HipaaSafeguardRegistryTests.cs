using System.Xml.Linq;
using Dialysis.BuildingBlocks.Fhir.Audit;
using Dialysis.BuildingBlocks.Hipaa.AspNetCore;
using Dialysis.BuildingBlocks.Hipaa.Safeguards;
using Hl7.Fhir.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Dialysis.BuildingBlocks.Hipaa.Tests;

/// <summary>
/// Regression gate for the compliance dashboard. A fully wired host (encryption + audit + HSTS +
/// persistent key ring) must end up with every safeguard reporting <see cref="HipaaSafeguardStatus.Active"/>.
/// If a refactor removes one of those wires, this test fails — that's the CI-level enforcement
/// the marketing claim depends on.
/// </summary>
public sealed class HipaaSafeguardRegistryTests
{
    [Fact]
    public void Reports_Missing_When_Nothing_Is_Wired()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHipaaSafeguards();
        services.AddSingleton<IConfigureOptions<KeyManagementOptions>>(_ => new ConfigureOptions<KeyManagementOptions>(_ => { }));
        using var sp = services.BuildServiceProvider();

        var snapshot = sp.GetRequiredService<HipaaSafeguardRegistry>().Evaluate();

        Assert.Contains(snapshot.Safeguards, s => s.Id == "encryption-at-rest" && s.Status == HipaaSafeguardStatus.Missing);
        Assert.Contains(snapshot.Safeguards, s => s.Id == "audit-log-emitter" && s.Status == HipaaSafeguardStatus.Missing);
        Assert.Contains(snapshot.Safeguards, s => s.Id == "data-protection-key-ring" && s.Status == HipaaSafeguardStatus.Degraded);
    }

    [Fact]
    public void Reports_All_Active_When_Production_Wires_Are_In_Place()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHipaaCompliance("test");
        services.AddHipaaAspNetCoreSafeguards();

        services.AddSingleton<IDataProtectionProvider, EphemeralDataProtectionProvider>();
        services.AddSingleton<IAuditEventEmitter, NoOpEmitter>();
        services.AddHsts(o =>
        {
            o.IncludeSubDomains = true;
            o.MaxAge = TimeSpan.FromDays(365);
        });
        services.Configure<KeyManagementOptions>(o => o.XmlRepository = new InMemoryXmlRepo());

        using var sp = services.BuildServiceProvider();
        var snapshot = sp.GetRequiredService<HipaaSafeguardRegistry>().Evaluate();

        Assert.All(snapshot.Safeguards, s =>
            Assert.True(s.Status == HipaaSafeguardStatus.Active,
                $"Expected {s.Id} to be Active; was {s.Status}. Evidence: {s.Evidence}"));
    }

    private sealed class NoOpEmitter : IAuditEventEmitter
    {
        public ValueTask EmitAsync(AuditEvent auditEvent, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    }

    private sealed class InMemoryXmlRepo : IXmlRepository
    {
        public IReadOnlyCollection<XElement> GetAllElements() => [];
        public void StoreElement(XElement element, string friendlyName) { }
    }
}
