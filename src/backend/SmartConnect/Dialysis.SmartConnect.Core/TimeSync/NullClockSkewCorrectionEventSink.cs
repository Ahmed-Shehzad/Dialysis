namespace Dialysis.SmartConnect.TimeSync;

/// <summary>
/// Default <see cref="IClockSkewCorrectionEventSink"/>: discards the audit record. Lets
/// hosts that aren't ready to wire a publisher run the corrector without crashing; once
/// the host registers a Transponder-backed sink the <c>TryAddSingleton</c> in the DI
/// extension steps aside.
/// </summary>
public sealed class NullClockSkewCorrectionEventSink : IClockSkewCorrectionEventSink
{
    public Task PublishAsync(ClockSkewCorrectionResult result, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
