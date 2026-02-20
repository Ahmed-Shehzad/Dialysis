using Dialysis.Treatment.Application.Abstractions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Refit;

namespace Dialysis.Treatment.Infrastructure.AlarmApi;

internal sealed class AlarmApiClient : IAlarmApiClient
{
    private readonly IAlarmApi? _api;
    private readonly ILogger<AlarmApiClient> _logger;
    private readonly AlarmApiOptions _options;

    public AlarmApiClient(IServiceProvider serviceProvider, IOptions<AlarmApiOptions> options, ILogger<AlarmApiClient> logger)
    {
        _options = options.Value;
        _logger = logger;
        _api = string.IsNullOrWhiteSpace(_options.BaseUrl) ? null : (IAlarmApi?)serviceProvider.GetService(typeof(IAlarmApi));
    }

    public async Task<bool> RecordFromThresholdBreachAsync(
        string sessionId,
        string? deviceId,
        string breachType,
        string code,
        double observedValue,
        double thresholdValue,
        string direction,
        string treatmentSessionId,
        string observationId,
        string? tenantId,
        CancellationToken cancellationToken = default)
    {
        if (_api is null)
        {
            _logger.LogDebug("AlarmApi not configured; skipping cross-context alarm creation");
            return false;
        }

        var request = new RecordAlarmFromThresholdBreachApiRequest(
            sessionId, deviceId, breachType, code, observedValue, thresholdValue, direction,
            treatmentSessionId, observationId, tenantId);

        ApiResponse<RecordAlarmFromThresholdBreachApiResponse> response = await _api.RecordFromThresholdBreachAsync(request, tenantId ?? "default", cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Created alarm {AlarmId} from threshold breach {BreachType}", response.Content?.AlarmId, breachType);
            return true;
        }

        _logger.LogWarning("AlarmApi RecordFromThresholdBreach failed: {StatusCode}", response.StatusCode);
        return false;
    }
}
