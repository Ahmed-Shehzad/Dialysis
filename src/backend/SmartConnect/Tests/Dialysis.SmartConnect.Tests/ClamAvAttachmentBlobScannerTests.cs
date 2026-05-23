using System.Text;
using Dialysis.SmartConnect.Attachments;
using Dialysis.SmartConnect.AvScanning.ClamAv;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Dialysis.SmartConnect.Tests;

/// <summary>
/// Integration tests for the ClamAV scanner adapter against a real clamd container. Uses the
/// generic Testcontainers <see cref="ContainerBuilder"/> because Testcontainers doesn't ship a
/// dedicated ClamAV module — the official image exposes port 3310 for the INSTREAM protocol.
///
/// The EICAR signature is the industry-standard test virus; every AV product recognises it without
/// being a real piece of malware.
/// </summary>
public sealed class ClamAvAttachmentBlobScannerTests : IAsyncLifetime
{
    private const string ClamAvImage = "clamav/clamav:1.4.0";
    private const int ClamdPort = 3310;
    private const string EicarSignature =
        "X5O!P%@AP[4\\PZX54(P^)7CC)7}$EICAR-STANDARD-ANTIVIRUS-TEST-FILE!$H+H*";
    private IContainer? _clamav;

    public async Task InitializeAsync()
    {
        _clamav = new ContainerBuilder(ClamAvImage)
            .WithPortBinding(ClamdPort, assignRandomHostPort: true)
            // clamd takes ~30s to load signatures on first start; clamdscan --ping returns 0 once
            // the socket is accepting commands, which is the most reliable readiness signal.
            .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("clamdscan", "--ping", "5"))
            .Build();
        await _clamav.StartAsync().ConfigureAwait(false);
    }

    public async Task DisposeAsync()
    {
        if (_clamav is not null)
            await _clamav.DisposeAsync().ConfigureAwait(false);
    }

    [Fact]
    public async Task Clean_Payload_Returns_Clean_Verdict_Async()
    {
        var scanner = BuildScanner();
        var bytes = Encoding.UTF8.GetBytes("a perfectly innocent dialysis treatment summary");

        var result = await scanner.ScanAsync(bytes, CancellationToken.None);

        Assert.Equal(AttachmentScanVerdict.Clean, result.Verdict);
    }

    [Fact]
    public async Task Eicar_Signature_Returns_Infected_Verdict_Async()
    {
        var scanner = BuildScanner();
        var bytes = Encoding.ASCII.GetBytes(EicarSignature);

        var result = await scanner.ScanAsync(bytes, CancellationToken.None);

        Assert.Equal(AttachmentScanVerdict.Infected, result.Verdict);
        Assert.NotNull(result.ThreatName);
        Assert.Contains("EICAR", result.ThreatName, StringComparison.OrdinalIgnoreCase);
    }

    private ClamAvAttachmentBlobScanner BuildScanner()
    {
        var clamav = _clamav ?? throw new InvalidOperationException("InitializeAsync was not called.");
        var hostPort = clamav.GetMappedPublicPort(ClamdPort);
        return new ClamAvAttachmentBlobScanner(
            Options.Create(new ClamAvScannerOptions
            {
                Host = "localhost",
                Port = hostPort,
                Timeout = TimeSpan.FromMinutes(2),
            }),
            NullLogger<ClamAvAttachmentBlobScanner>.Instance);
    }
}
