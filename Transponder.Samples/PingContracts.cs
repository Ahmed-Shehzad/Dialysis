using Transponder.Abstractions;
using Transponder.Persistence.Abstractions;

namespace Transponder.Samples;

public sealed record PingRequest(string Message, string Sender, DateTimeOffset SentAt) : IMessage;

public sealed record PingResponse(string Message, string Responder, DateTimeOffset RespondedAt) : IMessage;

public sealed class PingState : ISagaState
{
    public Ulid CorrelationId { get; set; }
    public Ulid? ConversationId { get; set; }
    public int Version { get; set; }
    public DateTimeOffset? LastPingAt { get; set; }
    public string? LastSender { get; set; }
    public string? LastMessage { get; set; }
}

public sealed class PingSaga : ISagaMessageHandler<PingState, PingRequest>
{
    public async Task HandleAsync(ISagaConsumeContext<PingState, PingRequest> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Saga.LastPingAt = DateTimeOffset.UtcNow;
        context.Saga.LastSender = context.Message.Sender;
        context.Saga.LastMessage = context.Message.Message;

        string responder = context.DestinationAddress?.ToString() ?? Environment.MachineName;
        var response = new PingResponse(
            $"Pong from {responder} to {context.Message.Sender}: {context.Message.Message}",
            responder,
            DateTimeOffset.UtcNow);

        await context.RespondAsync(response, context.CancellationToken).ConfigureAwait(false);
        context.MarkCompleted();
    }
}
