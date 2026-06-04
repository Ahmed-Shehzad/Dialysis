using System.Globalization;
using System.Text;
using Dialysis.SmartConnect.Inbound;

namespace Dialysis.SmartConnect.Api.Demo;

/// <summary>
/// Development-only background service that periodically dispatches synthetic ADT^A01 and ORU^R01
/// messages into the demo flows seeded by <see cref="SmartConnectDemoSeeder"/>. Bypasses the HTTP
/// endpoint by calling <see cref="IInboundTransport"/> in-process so the integration ledger fills
/// up during a client demo with no external clients.
/// </summary>
public sealed class Hl7V2SimulatorService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<Hl7V2SimulatorService> _logger;
    /// <summary>
    /// Development-only background service that periodically dispatches synthetic ADT^A01 and ORU^R01
    /// messages into the demo flows seeded by <see cref="SmartConnectDemoSeeder"/>. Bypasses the HTTP
    /// endpoint by calling <see cref="IInboundTransport"/> in-process so the integration ledger fills
    /// up during a client demo with no external clients.
    /// </summary>
    public Hl7V2SimulatorService(IServiceProvider services, ILogger<Hl7V2SimulatorService> logger)
    {
        _services = services;
        _logger = logger;
    }
    private static readonly TimeSpan _interval = TimeSpan.FromSeconds(7);
    private static readonly Random _rng = new(20251115);
    private static readonly string[] _wards = ["3W-NEPH", "ICU-A", "ER", "DIALYSIS-1"];
    private static readonly string[] _labCodes = ["718-7", "2160-0", "2823-3", "17861-6"];
    private static readonly string[] _labNames = ["Hemoglobin", "Creatinine", "Potassium", "Calcium"];
    private static int _counter;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("HL7 v2 simulator started (every {Seconds}s).", _interval.TotalSeconds);
        try { await Task.Delay(TimeSpan.FromSeconds(12), stoppingToken).ConfigureAwait(false); }
        catch (TaskCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchOneAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "HL7 v2 simulator tick failed.");
            }
            try { await Task.Delay(_interval, stoppingToken).ConfigureAwait(false); }
            catch (TaskCanceledException) { return; }
        }
    }

    private async Task DispatchOneAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var transport = scope.ServiceProvider.GetRequiredService<IInboundTransport>();
        var factory = scope.ServiceProvider.GetRequiredService<IInboundMessageFactory>();

        var n = Interlocked.Increment(ref _counter);
        var adtTurn = n % 2 == 0;
        var flowId = adtTurn ? SmartConnectDemoSeeder.DemoAdtFlowId : SmartConnectDemoSeeder.DemoOruFlowId;
        var payload = adtTurn ? BuildAdtA01(n) : BuildOruR01(n);

        var message = factory.Create(
            flowId,
            new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(payload)),
            PayloadFormat.Utf8Text,
            correlationId: $"sim-{n:D6}",
            metadata: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["smartconnect.simulator"] = "true",
                ["smartconnect.sender"] = "DemoLabSystem",
            });
        await transport.DispatchAsync(message, cancellationToken).ConfigureAwait(false);
    }

    private static string BuildAdtA01(int seq)
    {
        var mrn = $"MRN-{1000 + seq % 5000:D4}";
        var ward = _wards[_rng.Next(_wards.Length)];
        var now = DateTime.UtcNow;
        var ts = now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        var sb = new StringBuilder();
        sb.Append($"MSH|^~\\&|SIM|HOSPITAL|HIS|HOSPITAL|{ts}||ADT^A01^ADT_A01|MSG{seq:D8}|P|2.5.1\r");
        sb.Append($"EVN|A01|{ts}|||SIM\r");
        sb.Append($"PID|1||{mrn}^^^HOSPITAL^MR||DEMO^PATIENT{seq:D3}||19700101|M\r");
        sb.Append($"PV1|1|I|{ward}^101^A||||DR^DEMO\r");
        return sb.ToString();
    }

    private static string BuildOruR01(int seq)
    {
        var mrn = $"MRN-{1000 + seq % 5000:D4}";
        var idx = _rng.Next(_labCodes.Length);
        var code = _labCodes[idx];
        var name = _labNames[idx];
        var value = (8.0m + (decimal)_rng.NextDouble() * 4m).ToString("F1", CultureInfo.InvariantCulture);
        var now = DateTime.UtcNow;
        var ts = now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        var sb = new StringBuilder();
        sb.Append($"MSH|^~\\&|LAB|HOSPITAL|EHR|HOSPITAL|{ts}||ORU^R01^ORU_R01|LAB{seq:D8}|P|2.5.1\r");
        sb.Append($"PID|1||{mrn}^^^HOSPITAL^MR||DEMO^PATIENT{seq:D3}||19700101|M\r");
        sb.Append($"OBR|1|ORD{seq:D6}||{code}^{name}^LN|||{ts}\r");
        sb.Append($"OBX|1|NM|{code}^{name}^LN||{value}|mg/dL|||||F\r");
        return sb.ToString();
    }
}
