using BuildingBlocks.Abstractions;
using Dialysis.Alerting.Features.ProcessAlerts;
using Dialysis.Contracts.Events;
using Dialysis.Tenancy;
using Intercessor.Abstractions;

namespace Dialysis.Alerting;

/// <summary>
/// Handles HypotensionRiskRaised from the message bus and dispatches CreateAlertCommand.
/// </summary>
public sealed class HypotensionRiskIntegrationEventHandler : IIntegrationEventHandler<HypotensionRiskRaised>
{
    private readonly ISender _sender;
    private readonly TenantContext _tenantContext;

    public HypotensionRiskIntegrationEventHandler(ISender sender, TenantContext tenantContext)
    {
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
    }

    public async Task HandleAsync(HypotensionRiskRaised message, CancellationToken cancellationToken = default)
    {
        _tenantContext.TenantId = message.TenantId ?? "default";

        await _sender.SendAsync(new CreateAlertCommand
        {
            PatientId = message.PatientId,
            EncounterId = message.EncounterId,
            Code = "8480-6",
            Severity = "high",
            Message = $"Hypotension risk score {message.RiskScore:P1} calculated at {message.CalculatedAt:O}"
        }, cancellationToken);
    }
}
