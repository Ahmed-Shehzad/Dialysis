using System.Text.Json;

using Dialysis.Alarm.Application.Features.RecordAlarmFromThresholdBreach;

using Intercessor.Abstractions;

using Microsoft.Extensions.Logging;

using Transponder.Persistence;
using Transponder.Persistence.Abstractions;
using Transponder.Transports.Abstractions;

namespace Dialysis.Alarm.Infrastructure.IntegrationEvents;

internal sealed class ThresholdBreachDetectedReceiveHandler
{
    private const string ConsumerId = "alarm-threshold-breach";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IStorageSessionFactory _sessionFactory;
    private readonly ISender _sender;
    private readonly ILogger<ThresholdBreachDetectedReceiveHandler> _logger;

    public ThresholdBreachDetectedReceiveHandler(
        IStorageSessionFactory sessionFactory,
        ISender sender,
        ILogger<ThresholdBreachDetectedReceiveHandler> logger)
    {
        _sessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(IReceiveContext context, CancellationToken cancellationToken = default)
    {
        ITransportMessage message = context.Message;
        Ulid? messageId = message.MessageId;

        if (!messageId.HasValue)
        {
            _logger.LogWarning(
                "ThresholdBreachDetectedReceiveHandler: Message has no MessageId; cannot apply Inbox. Rejecting.");
            throw new InvalidOperationException("MessageId is required for Inbox idempotency.");
        }

        Ulid id = messageId.Value;

        await using IStorageSession session = await _sessionFactory.CreateSessionAsync(cancellationToken).ConfigureAwait(false);
        IInboxState? existing = await session.Inbox.GetAsync(id, ConsumerId, cancellationToken).ConfigureAwait(false);

        if (existing is not null)
        {
            _logger.LogDebug(
                "ThresholdBreachDetectedReceiveHandler: Message {MessageId} already processed by {ConsumerId}. Skipping.",
                id, ConsumerId);
            return;
        }

        ThresholdBreachDetectedMessage? evt = JsonSerializer.Deserialize<ThresholdBreachDetectedMessage>(
            message.Body.Span, JsonOptions);

        if (evt is null) throw new InvalidOperationException("Failed to deserialize ThresholdBreachDetectedMessage.");

        if (!Ulid.TryParse(evt.TreatmentSessionId, out Ulid treatmentSessionId))
            throw new ArgumentException($"Invalid TreatmentSessionId: {evt.TreatmentSessionId}");
        if (!Ulid.TryParse(evt.ObservationId, out Ulid observationId))
            throw new ArgumentException($"Invalid ObservationId: {evt.ObservationId}");

        var command = new RecordAlarmFromThresholdBreachCommand(
            evt.SessionId,
            evt.DeviceId,
            evt.BreachType,
            evt.Code,
            evt.ObservedValue,
            evt.ThresholdValue,
            evt.Direction,
            treatmentSessionId,
            observationId,
            evt.TenantId);

        _ = await _sender.SendAsync(command, cancellationToken).ConfigureAwait(false);

        var inboxState = new InboxState(id, ConsumerId);
        if (!await session.Inbox.TryAddAsync(inboxState, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning(
                "ThresholdBreachDetectedReceiveHandler: Inbox.TryAdd failed (race). MessageId={MessageId}", id);
            throw new InvalidOperationException("Inbox TryAdd failed; possible duplicate.");
        }

        await session.CommitAsync(cancellationToken).ConfigureAwait(false);
    }
}
