using System.Net.Http.Json;

using BuildingBlocks.Tenancy;

using Dialysis.Alarm.Application.Abstractions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dialysis.Alarm.Infrastructure.DeviceRegistration;

internal sealed class DeviceRegistrationClient : IDeviceRegistrationClient
{
    private readonly HttpClient _httpClient;
    private readonly ITenantContext _tenant;
    private readonly ILogger<DeviceRegistrationClient> _logger;
    private readonly DeviceApiOptions _options;

    public DeviceRegistrationClient(
        HttpClient httpClient,
        ITenantContext tenant,
        ILogger<DeviceRegistrationClient> logger,
        IOptions<DeviceApiOptions> options)
    {
        _httpClient = httpClient;
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
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/devices");
            _ = request.Headers.TryAddWithoutValidation("X-Tenant-Id", _tenant.TenantId ?? "default");
            request.Content = JsonContent.Create(new { deviceEui64 = deviceEui64.Trim() });

            HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
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
