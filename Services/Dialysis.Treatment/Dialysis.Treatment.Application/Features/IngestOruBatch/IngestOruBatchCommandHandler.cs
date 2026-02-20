using Dialysis.Treatment.Application.Abstractions;

using Intercessor.Abstractions;

namespace Dialysis.Treatment.Application.Features.IngestOruBatch;

public sealed class IngestOruBatchCommandHandler : ICommandHandler<IngestOruBatchCommand, IngestOruBatchResponse>
{
    private readonly IHl7BatchParser _batchParser;
    private readonly ISender _sender;

    public IngestOruBatchCommandHandler(IHl7BatchParser batchParser, ISender sender)
    {
        _batchParser = batchParser;
        _sender = sender;
    }

    public async Task<IngestOruBatchResponse> HandleAsync(IngestOruBatchCommand request, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<string> messages = _batchParser.ExtractMessages(request.RawHl7Batch);
        var sessionIds = new List<string>();

        foreach (string message in messages)
        {
            if (!IsOruR01(message)) continue;

            IngestOruMessage.IngestOruMessageResponse response = await _sender
                .SendAsync(new IngestOruMessage.IngestOruMessageCommand(message), cancellationToken);
            sessionIds.Add(response.SessionId);
        }

        return new IngestOruBatchResponse(sessionIds.Count, sessionIds);
    }

    private static bool IsOruR01(string message)
    {
        if (string.IsNullOrEmpty(message) || !message.StartsWith("MSH", StringComparison.Ordinal))
            return false;

        string[] segments = message.Split('\r', '\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (string seg in segments)
        {
            if (!seg.StartsWith("MSH", StringComparison.Ordinal)) continue;
            string[] fields = seg.Split('|');
            if (fields.Length <= 8) return false;
            string msh9 = fields[8].Trim();
            return msh9.StartsWith("ORU^R01", StringComparison.OrdinalIgnoreCase) ||
                   (msh9.Contains("ORU", StringComparison.OrdinalIgnoreCase) && msh9.Contains("R01", StringComparison.OrdinalIgnoreCase));
        }

        return false;
    }
}
