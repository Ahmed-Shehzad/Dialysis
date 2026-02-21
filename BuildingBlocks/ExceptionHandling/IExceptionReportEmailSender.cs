namespace BuildingBlocks.ExceptionHandling;

/// <summary>
/// Sends exception reports via email to the development team.
/// When not configured, the sender returns without sending.
/// </summary>
public interface IExceptionReportEmailSender
{
    /// <summary>
    /// Sends the full exception report to the configured development email.
    /// Implementations should not throw; log failures instead.
    /// </summary>
    Task SendAsync(ExceptionReport report, CancellationToken cancellationToken = default);
}
