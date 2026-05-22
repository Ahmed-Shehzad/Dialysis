using System.Collections.Immutable;
using System.Net;
using Dialysis.SmartConnect.Authentication;
using Dialysis.SmartConnect.ExtendedPlugins;
using Dialysis.SmartConnect.ExtendedPlugins.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dialysis.SmartConnect.Tests.Authentication;

/// <summary>
/// End-to-end tests for <see cref="HttpOutboundAdapter"/>'s per-route authentication wiring (slice
/// A1). Asserts that <c>opts.Authentication</c> in the parameters JSON drives the matching
/// <see cref="IHttpAuthenticationProvider"/> and that unrecognised kinds fail the route cleanly
/// instead of corrupting downstream sends.
/// </summary>
public sealed class HttpOutboundAdapterAuthenticationTests
{
    [Fact]
    public async Task Adapter_Sends_With_Bearer_Header_When_Authentication_Block_Present_Async()
    {
        HttpRequestMessage? captured = null;
        var (adapter, sp) = BuildAdapter(request =>
        {
            captured = request;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        await using var _ = sp;

        var message = BuildMessage("""
            {
              "Url": "https://partner.example/api/data",
              "Method": "POST",
              "Authentication": { "Kind": "bearer", "Token": "test-token-value" }
            }
            """);

        var result = await adapter.SendAsync(message, 0, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotNull(captured);
        Assert.Equal("Bearer", captured!.Headers.Authorization!.Scheme);
        Assert.Equal("test-token-value", captured.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task Adapter_Reports_Unknown_Authentication_Kind_As_Send_Failure_Async()
    {
        var (adapter, sp) = BuildAdapter(_ => new HttpResponseMessage(HttpStatusCode.OK));
        await using var _ = sp;

        var message = BuildMessage("""
            {
              "Url": "https://partner.example/api/data",
              "Authentication": { "Kind": "saml-bearer" }
            }
            """);

        var result = await adapter.SendAsync(message, 0, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains("saml-bearer", result.ErrorDetail!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Adapter_Returns_Auth_Failure_When_Provider_Throws_Async()
    {
        var (adapter, sp) = BuildAdapter(_ => new HttpResponseMessage(HttpStatusCode.OK));
        await using var _ = sp;

        // Missing 'Value' → provider throws InvalidOperationException, adapter wraps it as a
        // route failure so the flow runtime can route the message to dead-letter instead of
        // attempting the send with no API key.
        var message = BuildMessage("""
            {
              "Url": "https://partner.example/api/data",
              "Authentication": { "Kind": "api-key" }
            }
            """);

        var result = await adapter.SendAsync(message, 0, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains("api-key", result.ErrorDetail!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Adapter_Skips_Authentication_When_Block_Omitted_Async()
    {
        HttpRequestMessage? captured = null;
        var (adapter, sp) = BuildAdapter(request =>
        {
            captured = request;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        await using var _ = sp;

        var message = BuildMessage("""{ "Url": "https://partner.example/api/data" }""");

        var result = await adapter.SendAsync(message, 0, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Null(captured!.Headers.Authorization);
    }

    private static (HttpOutboundAdapter adapter, ServiceProvider sp) BuildAdapter(
        Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();
        services.AddSingleton<IHttpAuthenticationProvider, BearerTokenAuthenticationProvider>();
        services.AddSingleton<IHttpAuthenticationProvider, ApiKeyAuthenticationProvider>();
        services.AddSingleton<IHttpAuthenticationProvider, BasicAuthenticationProvider>();
        services.AddSingleton<IHttpAuthenticationProviderRegistry, HttpAuthenticationProviderRegistry>();
        services.AddHttpClient("smartconnect-outbound")
            .ConfigurePrimaryHttpMessageHandler(() => new InlineHandler(respond));

        var sp = services.BuildServiceProvider();
        var adapter = new HttpOutboundAdapter(
            sp.GetRequiredService<IHttpClientFactory>(),
            sp.GetRequiredService<IHttpAuthenticationProviderRegistry>());
        return (adapter, sp);
    }

    private static IntegrationMessage BuildMessage(string parametersJson) => new()
    {
        Id = Guid.NewGuid(),
        FlowId = Guid.NewGuid(),
        CorrelationId = "C",
        Payload = "payload"u8.ToArray(),
        PayloadFormat = PayloadFormat.Utf8Text,
        Metadata = ImmutableDictionary<string, string>.Empty
            .Add(HttpOutboundAdapter.ParametersMetadataKey, parametersJson),
        ReceivedAtUtc = DateTimeOffset.UtcNow,
    };

    private sealed class InlineHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }
}
