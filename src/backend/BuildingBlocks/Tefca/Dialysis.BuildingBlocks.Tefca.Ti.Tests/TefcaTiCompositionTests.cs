using Dialysis.BuildingBlocks.Tefca.Ti.Endpoints;
using Dialysis.BuildingBlocks.Tefca.Ti.Secrets;
using Dialysis.BuildingBlocks.Tefca.Ti.Smcb;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dialysis.BuildingBlocks.Tefca.Ti.Tests;

/// <summary>
/// Composition-level smokes for the TI scaffold. The full handshake-against-gematik happens
/// in a CI-only conformance suite (gated behind GEMATIK_CONFORMANCE_VECTORS); these tests
/// just verify the wiring + the safe-default behaviour of the stub card reader and the
/// production-mode opt-in.
/// </summary>
public sealed class TefcaTiCompositionTests
{
    [Fact]
    public void Composition_Registers_Defaults()
    {
        var services = new ServiceCollection()
            .AddTelematikInfrastrukturClient()
            .BuildServiceProvider();

        Assert.IsType<StubSmcBCardReader>(services.GetRequiredService<ISmcBCardReader>());
        Assert.IsType<EnvironmentVariableTiSecretsProvider>(services.GetRequiredService<ITiSecretsProvider>());
        Assert.NotNull(services.GetRequiredService<ITelematikInfrastrukturClient>());
    }

    [Theory]
    [InlineData(GematikEnvironment.Referenz)]
    [InlineData(GematikEnvironment.Test)]
    [InlineData(GematikEnvironment.Produktion)]
    public void Endpoint_Catalog_Has_All_Environments(GematikEnvironment env)
    {
        var endpoint = GematikEndpointCatalog.For(env);
        Assert.Equal(env, endpoint.Environment);
        Assert.NotNull(endpoint.DiscoveryDocument);
        Assert.NotNull(endpoint.EpaUpload);
        Assert.NotNull(endpoint.EpaDownload);
        Assert.NotNull(endpoint.TokenIssuer);
    }

    [Fact]
    public void Stub_Card_Reader_Reports_Not_Present()
    {
        var reader = new StubSmcBCardReader();
        Assert.False(reader.IsPresent);
    }

    [Fact]
    public async Task Stub_Card_Reader_Fails_Loudly_When_Asked_For_A_Chain_Async()
    {
        var reader = new StubSmcBCardReader();
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => reader.ReadCertificateChainAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Environment_Variable_Secrets_Provider_Resolves_Missing_To_Null_Async()
    {
        var provider = new EnvironmentVariableTiSecretsProvider();
        var result = await provider.GetAsync("definitely-not-set-secret-key", CancellationToken.None);
        Assert.Null(result);
    }
}
