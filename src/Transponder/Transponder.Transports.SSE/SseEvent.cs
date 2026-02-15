namespace Transponder.Transports.SSE;

internal sealed class SseEvent
{
    internal SseEvent(string? id, string? eventName, string? data, string? comment)
    {
        Id = id;
        EventName = eventName;
        Data = data;
        Comment = comment;
    }

    public string? Id { get; }

    public string? EventName { get; }

    public string? Data { get; }

    public string? Comment { get; }

    public static SseEvent FromEnvelope(string? eventName, SseTransportEnvelope envelope)
        => new(envelope.Id, eventName, envelope.ToJson(), comment: null);

    public static SseEvent CreateComment(string text)
        => new(id: null, eventName: null, data: null, comment: text);
}
