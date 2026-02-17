using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.Health;

public sealed class GetHealthQueryHandler : IQueryHandler<GetHealthQuery, HealthResult>
{
    private readonly ILogger<GetHealthQueryHandler> _logger;

    public GetHealthQueryHandler(ILogger<GetHealthQueryHandler> logger)
    {
        _logger = logger;
    }

    public Task<HealthResult> HandleAsync(GetHealthQuery request, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Health check");
        return Task.FromResult(new HealthResult("healthy", DateTimeOffset.UtcNow));
    }
}
