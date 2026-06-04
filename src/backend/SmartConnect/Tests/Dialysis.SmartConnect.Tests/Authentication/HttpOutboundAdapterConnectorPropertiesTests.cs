using System.Collections.Immutable;
using System.Net;
using System.Text;
using Dialysis.SmartConnect.Authentication;
using Dialysis.SmartConnect.ExtendedPlugins;
using Dialysis.SmartConnect.ExtendedPlugins.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dialysis.SmartConnect.Tests.Authentication;

/// <summary>
/// Slice B of the SmartConnect ↔ Mirth alignment plan: the
/// <c>ConnectorProperties</c> block on <c>HttpOutboundParameters</c> drives per-route timeouts,
/// retry policy, and response-body capture (Mirth UG pp. 246–252, Destination Connector Properties).
/// </summary>
public sealed class HttpOutboundAdapterConnectorPropertiesTests
{
    [Fact]
    public async Task Adapter_Retries_Transient_5xx_Up_To_Max_Retries_Async()
    {
        var attempts = 0;
        var (adapter, sp) = BuildAdapter(_ =>
        {
            var current = Interlocked.Increment(ref attempts);
            return current < 3
                ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                : new HttpResponseMessage(HttpStatusCode.OK);
        });
        await using var _ = sp;

        var message = BuildMessage("""
            {
              "Url": "https://partner.example/api/data",
              "ConnectorProperties": { "MaxRetries": 3, "RetryDelayMs": 1 }
            }
            """);

        var result = await adapter.SendAsync(message, 0, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(3, attempts);
    }

    [Fact]
    public async Task Adapter_Does_Not_Retry_Non_Retryable_Status_Codes_Async()
    {
        var attempts = 0;
        var (adapter, sp) = BuildAdapter(_ =>
        {
            Interlocked.Increment(ref attempts);
            return new HttpResponseMessage(HttpStatusCode.BadRequest);
        });
        await using var _ = sp;

        var message = BuildMessage("""
            {
              "Url": "https://partner.example/api/data",
              "ConnectorProperties": { "MaxRetries": 5, "RetryDelayMs": 1 }
            }
            """);

        var result = await adapter.SendAsync(message, 0, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(1, attempts);
        Assert.Contains("400", result.ErrorDetail!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Adapter_Honours_Custom_Retry_Status_Code_List_Async()
    {
        var attempts = 0;
        var (adapter, sp) = BuildAdapter(_ =>
        {
            var current = Interlocked.Increment(ref attempts);
            return current < 2
                ? new HttpResponseMessage(HttpStatusCode.Conflict)
                : new HttpResponseMessage(HttpStatusCode.OK);
        });
        await using var _ = sp;

        // 409 isn't in the default transient list, but the route opts in explicitly.
        var message = BuildMessage("""
            {
              "Url": "https://partner.example/api/data",
              "ConnectorProperties": { "MaxRetries": 3, "RetryDelayMs": 1, "RetryOnStatusCodes": [409] }
            }
            """);

        var result = await adapter.SendAsync(message, 0, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task Adapter_Captures_Response_Body_When_Configured_Async()
    {
        var (adapter, sp) = BuildAdapter(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("ACK^A01", Encoding.UTF8, "text/plain"),
        });
        await using var _ = sp;

        var message = BuildMessage("""
            {
              "Url": "https://partner.example/api/data",
              "ConnectorProperties": { "CaptureResponseBody": true }
            }
            """);

        var result = await adapter.SendAsync(message, 0, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.ResponsePayload);
        Assert.Equal("ACK^A01", Encoding.UTF8.GetString(result.ResponsePayload!));
    }

    [Fact]
    public async Task Adapter_Returns_Empty_Response_Payload_When_Capture_Disabled_Async()
    {
        var (adapter, sp) = BuildAdapter(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("ACK", Encoding.UTF8, "text/plain"),
        });
        await using var _ = sp;

        var message = BuildMessage("""{ "Url": "https://partner.example/api/data" }""");

        var result = await adapter.SendAsync(message, 0, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Null(result.ResponsePayload);
    }

    [Fact]
    public async Task Adapter_Surfaces_Per_Attempt_Timeout_As_Request_Timeout_Async()
    {
        var attempts = 0;
        var (adapter, sp) = BuildAdapter(async (_, ct) =>
        {
            Interlocked.Increment(ref attempts);
            // Sleep until the per-attempt CTS fires.
            await Task.Delay(TimeSpan.FromSeconds(30), ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        await using var _ = sp;

        // 1 retry with 1s timeout each; both should fire and the final result should describe the timeout.
        var message = BuildMessage("""
            {
              "Url": "https://partner.example/api/data",
              "ConnectorProperties": { "TimeoutSeconds": 1, "MaxRetries": 1, "RetryDelayMs": 1 }
            }
            """);

        var result = await adapter.SendAsync(message, 0, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal(2, attempts);
        Assert.Contains("TimeoutSeconds=1", result.ErrorDetail!, StringComparison.Ordinal);
    }

    private static (HttpOutboundAdapter adapter, ServiceProvider sp) BuildAdapter(
        Func<HttpRequestMessage, HttpResponseMessage> respond) => BuildAdapter((req, _) => Task.FromResult(respond(req)));

    private static (HttpOutboundAdapter adapter, ServiceProvider sp) BuildAdapter(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respondAsync)
    {
        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();
        services.AddSingleton<IHttpAuthenticationProvider, BearerTokenAuthenticationProvider>();
        services.AddSingleton<IHttpAuthenticationProviderRegistry, HttpAuthenticationProviderRegistry>();
        services.AddHttpClient("smartconnect-outbound")
            .ConfigurePrimaryHttpMessageHandler(() => new ScriptedHandler(respondAsync));

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

    private sealed class ScriptedHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _respond;
        public ScriptedHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) => _respond = respond;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            _respond(request, cancellationToken);
    }
}
