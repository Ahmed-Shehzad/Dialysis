using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

/// <summary>
/// Covers the new debug endpoint that lets operators evaluate a transformer script against an
/// arbitrary HL7 payload without round-tripping a real message. Uses the same JavascriptTransform
/// binding path as the production runtime — so a script that works here works in a deployed flow.
/// </summary>
public sealed class DebugEvaluateScriptEndpointTests : IClassFixture<SmartConnectApiFactory>
{
    private readonly SmartConnectApiFactory _factory;

    public DebugEvaluateScriptEndpointTests(SmartConnectApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Evaluates_Script_Reading_Pid3_Via_Msg_Global_Async()
    {
        using var client = _factory.CreateClient();
        var body = new
        {
            script = "msg.GetValue('PID.3.1')",
            payloadText =
                "MSH|^~\\&|HIS|HOSPITAL|EMR|CLINIC|20260526120000||ADT^A01|MSG-1|P|2.5\r" +
                "PID|||MRN-12345^^^HOSPITAL^MR",
        };

        var response = await client.PostAsJsonAsync("/api/v1/admin/debug/evaluate-script", body);
        var debugBody = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected 200 OK, got {response.StatusCode}. Body: {debugBody}");

        var result = JsonSerializer.Deserialize<EvaluateResponse>(debugBody,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(result);
        Assert.Equal("MRN-12345", result!.Result);
    }

    [Fact]
    public async Task Rejects_Missing_Script_Async()
    {
        using var client = _factory.CreateClient();
        var body = new { script = "", payloadText = "any" };

        var response = await client.PostAsJsonAsync("/api/v1/admin/debug/evaluate-script", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Returns_Unprocessable_Entity_When_Script_Throws_Async()
    {
        using var client = _factory.CreateClient();
        var body = new
        {
            script = "throw new Error('boom from script');",
            payloadText = "MSH|^~\\&|TEST",
        };

        var response = await client.PostAsJsonAsync("/api/v1/admin/debug/evaluate-script", body);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    private sealed class EvaluateResponse
    {
        [JsonPropertyName("result")]
        public string? Result { get; set; }
    }
}
