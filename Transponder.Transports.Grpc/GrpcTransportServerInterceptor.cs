using System.Diagnostics;
using System.Net.Http.Headers;

using Grpc.Core;
using Grpc.Core.Interceptors;

using Microsoft.Extensions.Logging;

namespace Transponder.Transports.Grpc;

/// <summary>
/// Enforces required fields, validates content type, injects correlation IDs, and logs lifecycle.
/// </summary>
public sealed class GrpcTransportServerInterceptor : Interceptor
{
    private static readonly string[] AllowedContentTypes = ["application/json"];
    private readonly ILogger<GrpcTransportServerInterceptor> _logger;

    public GrpcTransportServerInterceptor(ILogger<GrpcTransportServerInterceptor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        ArgumentNullException.ThrowIfNull(continuation);

        TransportEnvelopeInfo info = request switch
        {
            SendRequest send => ValidateAndEnrichSend(send),
            PublishRequest publish => ValidateAndEnrichPublish(publish),
            _ => new TransportEnvelopeInfo(null, null, null, null, null)
        };

        var stopwatch = Stopwatch.StartNew();
        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation(
                "Grpc transport start method={Method} destination={Destination} messageType={MessageType} messageId={MessageId} correlationId={CorrelationId} contentType={ContentType} peer={Peer}",
                context.Method,
                info.Destination,
                info.MessageType,
                info.MessageId,
                info.CorrelationId,
                info.ContentType,
                context.Peer);

        TResponse response = await continuation(request, context).ConfigureAwait(false);

        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation(
                "Grpc transport complete method={Method} destination={Destination} messageType={MessageType} messageId={MessageId} correlationId={CorrelationId} elapsedMs={ElapsedMs}",
                context.Method,
                info.Destination,
                info.MessageType,
                info.MessageId,
                info.CorrelationId,
                stopwatch.ElapsedMilliseconds);

        return response;
    }

    private static TransportEnvelopeInfo ValidateAndEnrichSend(SendRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Destination))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Destination is required."));

        TransportEnvelopeInfo info = ValidateAndEnrichMessage(request.Message, request.Destination);
        return info;
    }

    private static TransportEnvelopeInfo ValidateAndEnrichPublish(PublishRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return ValidateAndEnrichMessage(request.Message, destination: null);
    }

    private static TransportEnvelopeInfo ValidateAndEnrichMessage(GrpcTransportMessage? message, string? destination)
    {
        if (message is null)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Message is required."));

        if (message.Body is null || message.Body.IsEmpty)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Message body is required."));

        if (string.IsNullOrWhiteSpace(message.MessageType))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Message type is required."));

        if (string.IsNullOrWhiteSpace(message.ContentType))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Content type is required."));

        if (!IsAllowedContentType(message.ContentType))
            throw new RpcException(new Status(
                StatusCode.InvalidArgument,
                $"Unsupported content type '{message.ContentType}'."));

        if (string.IsNullOrWhiteSpace(message.CorrelationId))
            message.CorrelationId = Ulid.NewUlid().ToString();

        return new TransportEnvelopeInfo(
            message.MessageType,
            string.IsNullOrWhiteSpace(message.MessageId) ? null : message.MessageId,
            message.CorrelationId,
            message.ContentType,
            destination);
    }

    private static bool IsAllowedContentType(string contentType)
    {
        if (!MediaTypeHeaderValue.TryParse(contentType, out MediaTypeHeaderValue? parsed))
            return false;

        string mediaType = parsed.MediaType ?? string.Empty;
        return AllowedContentTypes.Any(type => string.Equals(type, mediaType, StringComparison.OrdinalIgnoreCase)) || mediaType.EndsWith("+json", StringComparison.OrdinalIgnoreCase);
    }

    private readonly record struct TransportEnvelopeInfo(
        string? MessageType,
        string? MessageId,
        string? CorrelationId,
        string? ContentType,
        string? Destination);
}
