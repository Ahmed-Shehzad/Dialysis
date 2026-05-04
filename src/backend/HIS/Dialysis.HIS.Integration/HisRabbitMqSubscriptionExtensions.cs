using Dialysis.BuildingBlocks.Transponder.Transport.RabbitMq;
using Dialysis.HIS.Contracts.IntegrationEvents;

namespace Dialysis.HIS.Integration;

/// <summary>
/// Declares RabbitMQ message types consumed by <see cref="HisTransponderIntegrationExtensions.AddHisIntegrationConsumers"/> so the broker binds queues consistently.
/// </summary>
public static class HisRabbitMqSubscriptionExtensions
{
    public static RabbitMqSubscriptionBuilder AddHisIntegrationMessageSubscriptions(this RabbitMqSubscriptionBuilder builder)
    {
        builder.Listen<PatientAdmittedToHospitalIntegrationEvent>();
        builder.Listen<PatientDischargedIntegrationEvent>();
        builder.Listen<ReferralCreatedIntegrationEvent>();
        builder.Listen<AppointmentBookedIntegrationEvent>();
        builder.Listen<MedicationOrderPlacedIntegrationEvent>();
        builder.Listen<MedicationOrderDiscontinuedIntegrationEvent>();
        return builder;
    }
}
