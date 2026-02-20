using BuildingBlocks.Tenancy;

using Dialysis.Treatment.Application.Abstractions;

using Refit;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dialysis.Treatment.Infrastructure.DeviceRegistration;

internal sealed class DeviceRegistrationClient : IDeviceRegistrationClient
{
    private readonly IDeviceApi _deviceApi;
    private readonly ITenantContext _tenant;
    private readonly ILogger<DeviceRegistrationClient> _logger;
    private readonly DeviceApiOptions _options;

    public DeviceRegistrationClient(
        IDeviceApi deviceApi,
        ITenantContext tenant,
        ILogger<DeviceRegistrationClient> logger,
        IOptions<DeviceApiOptions> options)
    {
        _deviceApi = deviceApi;
        _tenant = tenant;
        _logger = logger;
        _options = options.Value;
    }

    public async Task EnsureRegisteredAsync(string deviceEui64, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deviceEui64) || !_options.Enabled)
            return;

        try
        {
            ApiResponse<RegisterDeviceResponse> response = await _deviceApi.RegisterAsync(
                new RegisterDeviceRequest(deviceEui64.Trim()),
                _tenant.TenantId ?? "default",
                cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                _logger.LogWarning(
                    "Device registration failed for {DeviceEui64}: {StatusCode} {Reason}",
                    deviceEui64, response.StatusCode, response.ReasonPhrase);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Device registration failed for {DeviceEui64}", deviceEui64);
        }
    }
}
