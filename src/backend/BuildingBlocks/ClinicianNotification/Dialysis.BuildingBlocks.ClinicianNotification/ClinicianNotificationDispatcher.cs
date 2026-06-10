using Microsoft.Extensions.Logging;

namespace Dialysis.BuildingBlocks.ClinicianNotification;

/// <summary>
/// Default dispatcher: for each request, picks every sender that matches the channel code
/// and tries them in registration order, returning the first delivery. Logs every failure
/// with structured fields so the operator audit page can show the trail without us doing
/// extra correlation work.
/// </summary>
public sealed class ClinicianNotificationDispatcher : IClinicianNotificationDispatcher
{
    private readonly Dictionary<string, IClinicianNotificationSender[]> _byChannel;

    private readonly ILogger<ClinicianNotificationDispatcher> _logger;
    /// <summary>
    /// Default dispatcher: for each request, picks every sender that matches the channel code
    /// and tries them in registration order, returning the first delivery. Logs every failure
    /// with structured fields so the operator audit page can show the trail without us doing
    /// extra correlation work.
    /// </summary>
    public ClinicianNotificationDispatcher(IEnumerable<IClinicianNotificationSender> senders,
        ILogger<ClinicianNotificationDispatcher> logger)
    {
        _logger = logger;
        _byChannel = senders.GroupBy(s => s.ChannelCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToArray(), StringComparer.OrdinalIgnoreCase);
    }

    public async Task<IReadOnlyList<ChannelOutcome>> DispatchAsync(
        IReadOnlyList<ClinicianNotificationRequest> requests,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(requests);
        var outcomes = new List<ChannelOutcome>(requests.Count);
        foreach (var request in requests)
        {
            if (!_byChannel.TryGetValue(request.Channel, out var pool) || pool.Length == 0)
            {
                outcomes.Add(new ChannelOutcome(request.Channel, request.Address,
                    new ClinicianNotificationResult(false, null, $"No sender registered for channel '{request.Channel}'.")));
                continue;
            }

            ClinicianNotificationResult? lastResult = null;
            foreach (var sender in pool)
            {
                try
                {
                    lastResult = await sender.SendAsync(request, cancellationToken).ConfigureAwait(false);
                    if (lastResult.Delivered)
                        break;
                    _logger.LogWarning(
                        "Clinician notification provider {Sender} reported failure on channel {Channel}: {Reason}",
                        sender.GetType().Name, request.Channel, lastResult.FailureReason);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    lastResult = new ClinicianNotificationResult(false, null, ex.GetType().Name);
                    _logger.LogError(ex,
                        "Clinician notification provider {Sender} threw on channel {Channel}.",
                        sender.GetType().Name, request.Channel);
                }
            }

            outcomes.Add(new ChannelOutcome(request.Channel, request.Address,
                lastResult ?? new ClinicianNotificationResult(false, null, "No attempt made.")));
        }
        return outcomes;
    }
}
