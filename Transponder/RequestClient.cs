using System.Collections.Concurrent;

using Microsoft.Extensions.Logging;

using Transponder.Abstractions;
using Transponder.Transports.Abstractions;

namespace Transponder;

internal sealed class RequestClient<TRequest> : IRequestClient<TRequest>
    where TRequest : class, IMessage
{
    private readonly TransponderBus _bus;
    private readonly IMessageSerializer _serializer;
    private readonly ITransportHostProvider _hostProvider;
    private readonly ILogger<RequestClient<TRequest>> _logger;
    private readonly Uri _destinationAddress;
    private readonly TimeSpan _timeout;
    private readonly ConcurrentDictionary<Ulid, PendingRequest> _pendingRequests = new();
    private readonly Lock _sync = new();
    private IReceiveEndpoint? _responseEndpoint;
    private Uri? _responseAddress;
    private Task? _initializationTask;

    public RequestClient(
        TransponderBus bus,
        IMessageSerializer serializer,
        ITransportHostProvider hostProvider,
        Uri destinationAddress,
        TimeSpan timeout,
        ILogger<RequestClient<TRequest>> logger)
    {
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _hostProvider = hostProvider ?? throw new ArgumentNullException(nameof(hostProvider));
        _destinationAddress = destinationAddress ?? throw new ArgumentNullException(nameof(destinationAddress));
        _timeout = timeout <= TimeSpan.Zero ? TimeSpan.FromSeconds(30) : timeout;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TResponse> GetResponseAsync<TResponse>(
        TRequest request,
        CancellationToken cancellationToken = default)
        where TResponse : class, IMessage
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogDebug(
            "RequestClient starting request. RequestType={RequestType}, Destination={Destination}",
            typeof(TRequest).Name,
            _destinationAddress);

        await EnsureResponseEndpointAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogDebug(
            "RequestClient response endpoint ready. RequestType={RequestType}, ResponseAddress={ResponseAddress}",
            typeof(TRequest).Name,
            _responseAddress);

        var requestId = Ulid.NewUlid();
        var pending = new PendingRequest(typeof(TResponse));
        _pendingRequests[requestId] = pending;

        try
        {
            var headers = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                [TransponderMessageHeaders.RequestId] = requestId.ToString()
            };

            if (_responseAddress is not null) headers[TransponderMessageHeaders.ResponseAddress] = _responseAddress.ToString();

            await _bus.SendInternalAsync(
                    _destinationAddress,
                    request,
                    requestId,
                    requestId,
                    null,
                    headers,
                    cancellationToken)
                .ConfigureAwait(false);

            _logger.LogDebug(
                "RequestClient sent request. RequestType={RequestType}, RequestId={RequestId}",
                typeof(TRequest).Name,
                requestId);

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(_timeout);

            var delayTask = Task.Delay(_timeout, cancellationToken);
            Task completed = await Task.WhenAny(pending.Task, delayTask).ConfigureAwait(false);

            if (completed == delayTask)
                ThrowTimeoutOrCancellation(requestId, cancellationToken);

            object response = await pending.Task.ConfigureAwait(false);
            return (TResponse)response;
        }
        finally
        {
            _ = _pendingRequests.TryRemove(requestId, out _);
        }
    }

    private void ThrowTimeoutOrCancellation(Ulid requestId, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug(
                "RequestClient request cancelled. RequestType={RequestType}, RequestId={RequestId}",
                typeof(TRequest).Name,
                requestId);
            cancellationToken.ThrowIfCancellationRequested();
        }

        _logger.LogWarning(
            "RequestClient request timed out. RequestType={RequestType}, RequestId={RequestId}, Timeout={Timeout}s",
            typeof(TRequest).Name,
            requestId,
            _timeout.TotalSeconds);

        throw new TimeoutException(
            $"Request timed out after {_timeout.TotalSeconds:F0} seconds for {typeof(TRequest).Name}.");
    }

    private async Task EnsureResponseEndpointAsync(CancellationToken cancellationToken)
    {
        if (_responseEndpoint is not null) return;

        Task? initTask;
        lock (_sync)
        {
            if (_responseEndpoint is not null) return;

            if (_initializationTask is not null) initTask = _initializationTask;
            else
            {
                _responseAddress = _bus.CreateResponseAddress();
                ITransportHost host = _hostProvider.GetHost(_responseAddress);

                var configuration = new ReceiveEndpointConfiguration(
                    _responseAddress,
                    HandleResponseAsync);

                _responseEndpoint = host.ConnectReceiveEndpoint(configuration);

                initTask = _responseEndpoint.StartAsync(cancellationToken);
                _initializationTask = initTask;
            }
        }

        await initTask.ConfigureAwait(false);

        lock (_sync) _initializationTask = null;
    }

    private Task HandleResponseAsync(IReceiveContext context)
    {
        ITransportMessage message = context.Message;

        TransponderMessageContext messageContext = TransponderMessageContextFactory.FromTransportMessage(
            message,
            context.SourceAddress,
            context.DestinationAddress);
        using IDisposable? scope = _bus.BeginConsumeScope(messageContext);

        if (!TryGetRequestId(message, out Ulid requestId) || !_pendingRequests.TryGetValue(requestId, out PendingRequest? pending))
        {
            _logger.LogDebug(
                "RequestClient response ignored. RequestType={RequestType}, HasRequestId={HasRequestId}, MessageType={MessageType}",
                typeof(TRequest).Name,
                TryGetRequestId(message, out _),
                message.MessageType ?? "unknown");
            return Task.CompletedTask;
        }

        try
        {
            _logger.LogDebug(
                "RequestClient response received. RequestType={RequestType}, RequestId={RequestId}",
                typeof(TRequest).Name,
                requestId);

            object response = _serializer.Deserialize(message.Body.Span, pending.ResponseType);
            pending.TrySetResult(response);
        }
        catch (Exception ex)
        {
            string responseTypeName = pending.ResponseType.FullName ?? pending.ResponseType.Name;
            string messageTypeName = message.MessageType ?? "unknown";

            _logger.LogError(
                ex,
                "RequestClient response deserialize failed. RequestType={RequestType}, RequestId={RequestId}, MessageType={MessageType}, ResponseType={ResponseType}",
                typeof(TRequest).Name,
                requestId,
                messageTypeName,
                responseTypeName);

            var wrapped = new InvalidOperationException(
                $"Failed to deserialize response for request {requestId} (message type: {messageTypeName}, response type: {responseTypeName}).",
                ex);
            pending.TrySetException(wrapped);
        }

        return Task.CompletedTask;
    }

    private static bool TryGetRequestId(ITransportMessage message, out Ulid requestId)
    {
        if (message.CorrelationId.HasValue)
        {
            requestId = message.CorrelationId.Value;
            return true;
        }

        if (message.Headers.TryGetValue(TransponderMessageHeaders.RequestId, out object? headerValue) &&
            Ulid.TryParse(headerValue?.ToString(), out Ulid parsed))
        {
            requestId = parsed;
            return true;
        }

        requestId = Ulid.Empty;
        return false;
    }

    private sealed class PendingRequest
    {
        private readonly TaskCompletionSource<object> _completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public PendingRequest(Type responseType)
        {
            ResponseType = responseType ?? throw new ArgumentNullException(nameof(responseType));
        }

        public Type ResponseType { get; }

        public Task<object> Task => _completion.Task;

        public void TrySetResult(object response) => _completion.TrySetResult(response);

        public void TrySetException(Exception exception) => _completion.TrySetException(exception);
    }
}
