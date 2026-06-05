using System.Text.Json;

namespace Dialysis.SmartConnect.Lab;

/// <summary>
/// Parses a <c>LabOrderPlacedIntegrationEvent</c> JSON payload into a transport-neutral
/// <see cref="LabOrderFrame"/>. Shared by the HL7 v2 ORM and FHIR <c>ServiceRequest</c> outbound
/// stages so both transports read the event identically. Returns <see langword="null"/> for any
/// payload that isn't a usable order (unparseable, no tests, or missing the placer/patient identity
/// needed to route the order and match its result), letting the caller pass the message through.
/// </summary>
public static class LabOrderEventParser
{
    // The Lab module's LabOrderPriority enum serialises Stat as 1 (Routine = 0). The outbox uses
    // default System.Text.Json (numbers); a re-serialisation may use the string name. Accept both.
    private const int StatPriorityOrdinal = 1;

    /// <summary>Attempts to project the event JSON onto a <see cref="LabOrderFrame"/>; null when not an order.</summary>
    public static LabOrderFrame? TryParse(ReadOnlySpan<byte> payload, JsonSerializerOptions options)
    {
        EventDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<EventDto>(payload, options);
        }
        catch (JsonException)
        {
            return null;
        }

        if (dto is null
            || string.IsNullOrWhiteSpace(dto.PlacerOrderNumber)
            || dto.PatientId == Guid.Empty
            || dto.Tests is null
            || dto.Tests.Count == 0)
        {
            return null;
        }

        var tests = new List<LabTestRequest>(dto.Tests.Count);
        foreach (var test in dto.Tests)
        {
            if (string.IsNullOrWhiteSpace(test.LoincCode))
            {
                continue;
            }

            tests.Add(new LabTestRequest(test.LoincCode, test.Display ?? test.LoincCode));
        }

        if (tests.Count == 0)
        {
            return null;
        }

        return new LabOrderFrame(
            PatientIdentifier: dto.PatientId.ToString(),
            PlacerOrderNumber: dto.PlacerOrderNumber,
            IsStat: IsStat(dto.Priority),
            Specimen: dto.Specimen,
            Tests: tests,
            OrderedAtUtc: dto.PlacedAtUtc == default ? DateTime.UtcNow : dto.PlacedAtUtc);
    }

    private static bool IsStat(JsonElement priority) =>
        priority.ValueKind switch
        {
            JsonValueKind.Number => priority.TryGetInt32(out var n) && n == StatPriorityOrdinal,
            JsonValueKind.String => string.Equals(priority.GetString(), "Stat", StringComparison.OrdinalIgnoreCase),
            _ => false,
        };

    private sealed record EventDto
    {
        public Guid PatientId { get; init; }
        public string? PlacerOrderNumber { get; init; }
        public JsonElement Priority { get; init; }
        public string? Specimen { get; init; }
        public IReadOnlyList<TestDto>? Tests { get; init; }
        public DateTime PlacedAtUtc { get; init; }
    }

    private sealed record TestDto
    {
        public string? LoincCode { get; init; }
        public string? Display { get; init; }
    }
}
