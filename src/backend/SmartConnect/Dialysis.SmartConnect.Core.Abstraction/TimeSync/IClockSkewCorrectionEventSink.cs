namespace Dialysis.SmartConnect.TimeSync;

/// <summary>
/// Slice J3: receives an audit record every time the §2 probe rewrites a message's MSH-7
/// timestamp. The default registration is a no-op so callers don't need to special-case
/// "no audit sink"; production hosts wire a Transponder-backed sink that publishes
/// <c>Hl7V2ClockSkewCorrectedIntegrationEvent</c> for downstream subscribers (audit log,
/// operator dashboard, compliance archive).
/// </summary>
/// <remarks>
/// Kept narrow on purpose — accepts a <see cref="ClockSkewCorrectionResult"/> directly so
/// implementations can decide how to project it onto whatever event shape their transport
/// expects. The contract project's <c>Hl7V2ClockSkewCorrectedIntegrationEvent</c> is the
/// canonical shape, but a host could just as well log the result to a SIEM.
/// </remarks>
public interface IClockSkewCorrectionEventSink
{
    Task PublishAsync(ClockSkewCorrectionResult result, CancellationToken cancellationToken);
}
