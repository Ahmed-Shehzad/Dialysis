using System.Net.Http.Json;
using System.Text.Json;
using Dialysis.HIS.Medication.Features.PlaceMedicationOrder;
using Xunit;

namespace Dialysis.HIS.Tests;

/// <summary>
/// Proves HIS → SQL outbox → outbox relay → RabbitMQ when CI sets <c>HIS_CI_OUTBOX_E2E=1</c> and connection/broker env vars
/// (documented in repo <c>his_transponder_e2e_runbook.md</c> and <c>.github/workflows/his-ci.yml</c>).
/// </summary>
public sealed class HisOutboxRelayGoldenPathTests : IClassFixture<HisApiDefaultFactory>
{
    private readonly HttpClient _client;

    public HisOutboxRelayGoldenPathTests(HisApiDefaultFactory factory) => _client = factory.CreateClient();

    [SkippableFact]
    public async Task Medication_order_placed_integration_event_is_relayed_and_marked_processed()
    {
        Skip.IfNot(string.Equals(Environment.GetEnvironmentVariable("HIS_CI_OUTBOX_E2E"), "1", StringComparison.Ordinal));

        var patientId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddd0c");
        using var place = await _client.PostAsJsonAsync(
            new Uri("/api/v1.0/medication/orders", UriKind.Relative),
            new PlaceMedicationOrderCommand(patientId, "E2E-MED-001"));
        place.EnsureSuccessStatusCode();

        const string eventTypeFragment = "MedicationOrderPlacedIntegrationEvent";
        for (var attempt = 0; attempt < 120; attempt++)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500));
            using var list = await _client.GetAsync(
                new Uri("/api/v1.0/data-management/integration/outbox-metadata?take=50", UriKind.Relative));
            list.EnsureSuccessStatusCode();
            using var doc = JsonDocument.Parse(await list.Content.ReadAsStringAsync());
            foreach (var row in doc.RootElement.GetProperty("data").EnumerateArray())
            {
                var typeName = row.GetProperty("assemblyQualifiedEventType").GetString();
                if (typeName is null || !typeName.Contains(eventTypeFragment, StringComparison.Ordinal))
                    continue;

                if (row.TryGetProperty("processedAtUtc", out var processed)
                    && processed.ValueKind == JsonValueKind.String
                    && processed.GetDateTime() > DateTime.MinValue)
                    return;
            }
        }

        Assert.Fail("Expected an outbox row for MedicationOrderPlacedIntegrationEvent with ProcessedAtUtc set within the timeout.");
    }
}
