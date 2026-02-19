using BuildingBlocks.Abstractions;
using BuildingBlocks.Tenancy;

using Dialysis.Treatment.Application.Domain.Events;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Dialysis.Treatment.Application.Features.TreatmentSessionStarted;

internal sealed class FhirSubscriptionNotifyHandler : IDomainEventHandler<TreatmentSessionStartedEvent>
{
    private readonly IFhirSubscriptionNotifyClient _notifyClient;
    private readonly ITenantContext _tenant;
    private readonly IConfiguration _config;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public FhirSubscriptionNotifyHandler(
        IFhirSubscriptionNotifyClient notifyClient,
        ITenantContext tenant,
        IConfiguration config,
        IHttpContextAccessor httpContextAccessor)
    {
        _notifyClient = notifyClient;
        _tenant = tenant;
        _config = config;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task HandleAsync(TreatmentSessionStartedEvent notification, CancellationToken cancellationToken = default)
    {
        string? baseUrl = _config["FhirSubscription:NotifyUrl"];
        if (string.IsNullOrEmpty(baseUrl))
            return;

        string resourceUrl = baseUrl.TrimEnd('/') + "/api/treatment-sessions/" + notification.SessionId.Value + "/fhir";
        string? auth = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].FirstOrDefault();

        await _notifyClient.NotifyAsync(
            "Procedure",
            resourceUrl,
            _tenant.TenantId,
            auth,
            cancellationToken);
    }
}
