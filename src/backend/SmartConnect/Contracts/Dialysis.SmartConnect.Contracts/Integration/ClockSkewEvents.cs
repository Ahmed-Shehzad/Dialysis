using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.SmartConnect.Contracts.Integration;

/// <summary>
/// Slice J3: published every time the §2 Time Synchronization probe rewrites a message's
/// <c>MSH-7</c> timestamp to the SmartConnect server clock because the upstream's clock
/// drifted outside the configured tolerance. Subscribers (audit log, operator dashboard)
/// persist these so we never silently retime a clinical message — there is always a
/// queryable record of when the correction happened, what the original timestamp said,
/// and how big the observed skew was.
/// </summary>
public sealed record Hl7V2ClockSkewCorrectedIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// Slice J3: published every time the §2 Time Synchronization probe rewrites a message's
    /// <c>MSH-7</c> timestamp to the SmartConnect server clock because the upstream's clock
    /// drifted outside the configured tolerance. Subscribers (audit log, operator dashboard)
    /// persist these so we never silently retime a clinical message — there is always a
    /// queryable record of when the correction happened, what the original timestamp said,
    /// and how big the observed skew was.
    /// </summary>
    /// <param name="EventId">Stable identifier for outbox de-duplication.</param>
    /// <param name="OccurredOn">Server clock at the moment of correction (UTC).</param>
    /// <param name="SchemaVersion">Bumped if the field set changes; consumers should ignore
    /// events with a schema version newer than the one they were compiled against.</param>
    /// <param name="SourceId">Upstream identifier — for HL7v2 the convention is
    /// <c>{sendingApp}@{sendingFacility}</c> (matches the slice C2 <c>SenderId</c> column
    /// and the slice J monitor's source key).</param>
    /// <param name="OriginalMessageTimestampUtc">MSH-7 as it arrived on the wire.</param>
    /// <param name="CorrectedMessageTimestampUtc">MSH-7 after the rewrite (always equal to
    /// <see cref="OccurredOn"/> since correction normalises to the server clock).</param>
    /// <param name="ObservedSkewSeconds">Signed drift the corrector observed; positive means
    /// the upstream's clock was behind ours.</param>
    public Hl7V2ClockSkewCorrectedIntegrationEvent(Guid EventId,
        DateTime OccurredOn,
        int SchemaVersion,
        string SourceId,
        DateTime OriginalMessageTimestampUtc,
        DateTime CorrectedMessageTimestampUtc,
        double ObservedSkewSeconds)
    {
        this.EventId = EventId;
        this.OccurredOn = OccurredOn;
        this.SchemaVersion = SchemaVersion;
        this.SourceId = SourceId;
        this.OriginalMessageTimestampUtc = OriginalMessageTimestampUtc;
        this.CorrectedMessageTimestampUtc = CorrectedMessageTimestampUtc;
        this.ObservedSkewSeconds = ObservedSkewSeconds;
    }

    /// <summary>Stable identifier for outbox de-duplication.</summary>
    public Guid EventId { get; init; }

    /// <summary>Server clock at the moment of correction (UTC).</summary>
    public DateTime OccurredOn { get; init; }

    /// <summary>Bumped if the field set changes; consumers should ignore
    /// events with a schema version newer than the one they were compiled against.</summary>
    public int SchemaVersion { get; init; }

    /// <summary>Upstream identifier — for HL7v2 the convention is
    /// <c>{sendingApp}@{sendingFacility}</c> (matches the slice C2 <c>SenderId</c> column
    /// and the slice J monitor's source key).</summary>
    public string SourceId { get; init; }

    /// <summary>MSH-7 as it arrived on the wire.</summary>
    public DateTime OriginalMessageTimestampUtc { get; init; }

    /// <summary>MSH-7 after the rewrite (always equal to
    /// <see cref="OccurredOn"/> since correction normalises to the server clock).</summary>
    public DateTime CorrectedMessageTimestampUtc { get; init; }

    /// <summary>Signed drift the corrector observed; positive means
    /// the upstream's clock was behind ours.</summary>
    public double ObservedSkewSeconds { get; init; }

    public void Deconstruct(out Guid EventId, out DateTime OccurredOn, out int SchemaVersion, out string SourceId, out DateTime OriginalMessageTimestampUtc, out DateTime CorrectedMessageTimestampUtc, out double ObservedSkewSeconds)
    {
        EventId = this.EventId;
        OccurredOn = this.OccurredOn;
        SchemaVersion = this.SchemaVersion;
        SourceId = this.SourceId;
        OriginalMessageTimestampUtc = this.OriginalMessageTimestampUtc;
        CorrectedMessageTimestampUtc = this.CorrectedMessageTimestampUtc;
        ObservedSkewSeconds = this.ObservedSkewSeconds;
    }
}
