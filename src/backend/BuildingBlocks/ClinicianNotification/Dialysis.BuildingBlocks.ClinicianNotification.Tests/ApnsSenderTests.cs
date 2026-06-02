using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Dialysis.BuildingBlocks.ClinicianNotification.Apns;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Dialysis.BuildingBlocks.ClinicianNotification.Tests;

/// <summary>
/// Verifies the ApnsPushSender's ES256 JWT signing because that's the only piece of
/// non-trivial logic in the sender (the HTTP round-trip is straight pass-through to
/// Apple). We generate a fresh ECDsa key per test, sign through the sender's path, then
/// verify the signature against the same key.
/// </summary>
public sealed class ApnsSenderTests
{
    [Fact]
    public async Task Sender_Returns_Failure_When_Not_Configured_Async()
    {
        var sender = new ApnsPushSender(
            new SingleClientFactory(new StubHandler()),
            Options.Create(new ApnsPushOptions()),
            NullLogger<ApnsPushSender>.Instance);

        var result = await sender.SendAsync(
            new ClinicianNotificationRequest(
                Channel: "push.apns",
                Address: "device-token",
                Subject: "Chair alarm",
                Body: "Acknowledge in the app.",
                DeepLink: null,
                Priority: NotificationPriority.Critical,
                Metadata: new Dictionary<string, string>()),
            CancellationToken.None);

        result.Delivered.ShouldBeFalse();
        result.FailureReason.ShouldNotBeNull().ShouldContain("not configured");
    }

    [Fact]
    public async Task Sender_Posts_Bearer_Jwt_That_Verifies_Against_The_Same_Key_Async()
    {
        using var ec = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var pem = ec.ExportPkcs8PrivateKeyPem();
        var captured = new StubHandler { StatusCode = HttpStatusCode.OK };
        var sender = new ApnsPushSender(
            new SingleClientFactory(captured),
            Options.Create(new ApnsPushOptions
            {
                TeamId = "TEAM123456",
                KeyId = "KEYID12345",
                BundleId = "com.dialysis.app",
                PrivateKeyPem = pem,
            }),
            NullLogger<ApnsPushSender>.Instance);

        var result = await sender.SendAsync(
            new ClinicianNotificationRequest(
                Channel: "push.apns",
                Address: "dev-tok",
                Subject: "Chair alarm",
                Body: "Acknowledge",
                DeepLink: null,
                Priority: NotificationPriority.Critical,
                Metadata: new Dictionary<string, string>()),
            CancellationToken.None);

        result.Delivered.ShouldBeTrue();
        captured.LastRequestAuthHeader.ShouldNotBeNull();
        captured.LastRequestAuthHeader!.Scheme.ShouldBe("bearer");

        var jwt = captured.LastRequestAuthHeader.Parameter!;
        VerifyJwt(jwt, ec).ShouldBeTrue();
    }

    private static bool VerifyJwt(string jwt, ECDsa key)
    {
        var parts = jwt.Split('.');
        if (parts.Length != 3) return false;

        var signingInput = $"{parts[0]}.{parts[1]}";
        var signature = Base64UrlDecode(parts[2]);
        var verified = key.VerifyData(Encoding.UTF8.GetBytes(signingInput), signature, HashAlgorithmName.SHA256);
        if (!verified) return false;

        // Header carries `kid` matching the configured key id; payload carries `iss`.
        using var header = JsonDocument.Parse(Base64UrlDecode(parts[0]));
        header.RootElement.GetProperty("alg").GetString().ShouldBe("ES256");
        header.RootElement.GetProperty("kid").GetString().ShouldBe("KEYID12345");
        using var payload = JsonDocument.Parse(Base64UrlDecode(parts[1]));
        payload.RootElement.GetProperty("iss").GetString().ShouldBe("TEAM123456");
        return true;
    }

    private static byte[] Base64UrlDecode(string s)
    {
        var p = s.Replace('-', '+').Replace('_', '/');
        switch (p.Length % 4)
        {
            case 2: p += "=="; break;
            case 3: p += "="; break;
        }
        return Convert.FromBase64String(p);
    }

    private sealed class SingleClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
        public AuthenticationHeaderValue? LastRequestAuthHeader { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestAuthHeader = request.Headers.Authorization;
            var response = new HttpResponseMessage(StatusCode);
            response.Headers.Add("apns-id", Guid.NewGuid().ToString("N"));
            return Task.FromResult(response);
        }
    }
}
