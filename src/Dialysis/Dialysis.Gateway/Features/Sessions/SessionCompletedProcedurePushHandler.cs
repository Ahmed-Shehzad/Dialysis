using Dialysis.Contracts.Events;
using Dialysis.Gateway.Features.Outbound.PushToEhr;
using Dialysis.Gateway.Infrastructure;

using Intercessor.Abstractions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dialysis.Gateway.Features.Sessions;

/// <summary>
/// Pushes patient data (including completed Procedure) to the configured EHR when a session completes.
/// Requires Integration:PdmsFhirBaseUrl and Integration:EhrFhirBaseUrl to be set.
/// </summary>
public sealed class SessionCompletedProcedurePushHandler : INotificationHandler<SessionCompleted>
{
    private readonly ISender _sender;
    private readonly EhrOutboundOptions _options;
    private readonly ILogger<SessionCompletedProcedurePushHandler> _logger;

    public SessionCompletedProcedurePushHandler(
        ISender sender,
        IOptions<EhrOutboundOptions> options,
        ILogger<SessionCompletedProcedurePushHandler> logger)
    {
        _sender = sender;
        _options = options.Value;
        _logger = logger;
    }

    public async Task HandleAsync(SessionCompleted notification, CancellationToken cancellationToken = default)
    {
        var baseUrl = _options.PdmsFhirBaseUrl?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            _logger.LogDebug(
                "SessionCompletedProcedurePushHandler: PdmsFhirBaseUrl not configured. Skipping EHR push for SessionId={SessionId}.",
                notification.SessionId);
            return;
        }

        var baseUrlWithSlash = baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/";
        var command = new PushToEhrCommand(baseUrlWithSlash, notification.PatientId.Value);
        var result = await _sender.SendAsync(command, cancellationToken);

        if (result.Success)
        {
            _logger.LogInformation(
                "SessionCompletedProcedurePushHandler: EHR push succeeded for SessionId={SessionId}, PatientId={PatientId}, ResourceCount={Count}",
                notification.SessionId,
                notification.PatientId.Value,
                result.ResourceCount);
        }
        else
        {
            _logger.LogWarning(
                "SessionCompletedProcedurePushHandler: EHR push failed for SessionId={SessionId}, PatientId={PatientId}, StatusCode={StatusCode}, Error={Error}",
                notification.SessionId,
                notification.PatientId.Value,
                result.StatusCode,
                result.ErrorMessage);
        }
    }
}
