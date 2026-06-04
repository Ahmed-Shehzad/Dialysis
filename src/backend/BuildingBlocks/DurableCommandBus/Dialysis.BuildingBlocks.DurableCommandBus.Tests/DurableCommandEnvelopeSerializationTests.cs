using System.Text.Json;
using Shouldly;
using Xunit;

namespace Dialysis.BuildingBlocks.DurableCommandBus.Tests;

/// <summary>
/// Round-trips the wire envelope through System.Text.Json. The bus and the consumer must agree
/// on the camelCase property naming for the envelope to deserialize after a hop through
/// RabbitMQ.
/// </summary>
public sealed class DurableCommandEnvelopeSerializationTests
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    [Fact]
    public void Envelope_Round_Trips_Preserving_Every_Field()
    {
        var enqueuedAt = new DateTime(2026, 6, 3, 12, 0, 0, DateTimeKind.Utc);
        var original = new DurableCommandEnvelope(
            CommandId: Guid.NewGuid(),
            CommandTypeKey: "Some.Module.Domain.FooCommand",
            SchemaVersion: 1,
            PayloadJson: "{\"a\":1}",
            CorrelationId: "abc123",
            EnqueuedAtUtc: enqueuedAt,
            RequestedBySubject: "subject1");

        var json = JsonSerializer.Serialize(original, _options);
        var restored = JsonSerializer.Deserialize<DurableCommandEnvelope>(json, _options);

        restored.ShouldNotBeNull();
        restored.CommandId.ShouldBe(original.CommandId);
        restored.CommandTypeKey.ShouldBe(original.CommandTypeKey);
        restored.SchemaVersion.ShouldBe(original.SchemaVersion);
        restored.PayloadJson.ShouldBe(original.PayloadJson);
        restored.CorrelationId.ShouldBe(original.CorrelationId);
        restored.EnqueuedAtUtc.ShouldBe(original.EnqueuedAtUtc);
        restored.RequestedBySubject.ShouldBe(original.RequestedBySubject);
    }

    [Fact]
    public void Envelope_With_Null_Subject_Round_Trips()
    {
        var original = new DurableCommandEnvelope(
            CommandId: Guid.NewGuid(),
            CommandTypeKey: "T",
            SchemaVersion: 1,
            PayloadJson: "{}",
            CorrelationId: "c",
            EnqueuedAtUtc: DateTime.UtcNow,
            RequestedBySubject: null);

        var json = JsonSerializer.Serialize(original, _options);
        var restored = JsonSerializer.Deserialize<DurableCommandEnvelope>(json, _options);

        restored.ShouldNotBeNull();
        restored.RequestedBySubject.ShouldBeNull();
    }
}
