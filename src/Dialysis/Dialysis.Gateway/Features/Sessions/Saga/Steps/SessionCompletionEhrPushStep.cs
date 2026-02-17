using Dialysis.Gateway.Features.Outbound.PushToEhr;
using Dialysis.Gateway.Infrastructure;

using Intercessor.Abstractions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dialysis.Gateway.Features.Sessions.Saga.Steps;

/// <summary>
/// Single responsibility: execute EHR push for session completion.
/// </summary>
public sealed class SessionCompletionEhrPushStep : ISessionCompletionStep
{
    private readonly ISender _sender;
    private readonly EhrOutboundOptions _ehrOptions;
    private readonly ILogger<SessionCompletionEhrPushStep> _logger;

    public SessionCompletionEhrPushStep(
        ISender sender,
        IOptions<EhrOutboundOptions> ehrOptions,
        ILogger<SessionCompletionEhrPushStep> logger)
    {
        _sender = sender;
        _ehrOptions = ehrOptions.Value;
        _logger = logger;
    }

    public async Task ExecuteAsync(SessionCompletionState state, CancellationToken cancellationToken = default)
    {
        var baseUrl = _ehrOptions.PdmsFhirBaseUrl?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            _logger.LogDebug(
                "SessionCompletionEhrPushStep: PdmsFhirBaseUrl not configured. Skipping EHR push for SessionId={SessionId}.",
                state.SessionId);
            state.EhrPushSucceeded = true;
            return;
        }

        var baseUrlWithSlash = baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/";
        var command = new PushToEhrCommand(baseUrlWithSlash, state.PatientId);
        var result = await _sender.SendAsync(command, cancellationToken);

        state.EhrPushSucceeded = result.Success;
        if (result.Success)
        {
            _logger.LogInformation(
                "SessionCompletionEhrPushStep: EHR push succeeded for SessionId={SessionId}, PatientId={PatientId}",
                state.SessionId,
                state.PatientId);
        }
        else
        {
            _logger.LogWarning(
                "SessionCompletionEhrPushStep: EHR push failed for SessionId={SessionId}, PatientId={PatientId}, Error={Error}",
                state.SessionId,
                state.PatientId,
                result.ErrorMessage);
            throw new InvalidOperationException($"EHR push failed: {result.ErrorMessage}");
        }
    }
}
