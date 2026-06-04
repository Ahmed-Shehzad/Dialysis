using Dialysis.BuildingBlocks.Transponder;
using Dialysis.HIS.Contracts.IntegrationEvents.PatientFlow;
using Dialysis.PDMS.TreatmentSessions.Projections;
using Microsoft.Extensions.Logging;

namespace Dialysis.PDMS.TreatmentSessions.Features.IngestChairAssignment;

/// <summary>
/// HIS → PDMS boundary consumer. The HIS Front Desk publishes a chair placement when a
/// waiting patient is moved into a treatment chair; PDMS captures it in the in-memory
/// <see cref="ChairOccupancyProjection"/> so the chairside view can resolve patient
/// context from a chair id before vitals arrive.
/// </summary>
/// <remarks>
/// Persistent storage of chair occupancy is a deliberate follow-up: a PDMS-owned EF
/// projection (with its own migration) is the natural shape, but the read model is
/// in-memory for the demo loop the same way <c>EfManagerDashboardReadModel</c> sits
/// alongside in-memory views in HIS. Restarts lose state — acceptable while the only
/// consumer is the chairside dashboard, which can re-hydrate from the next placement.
/// </remarks>
public sealed class PatientPlacedInChairConsumer : IConsumer<PatientPlacedInChairIntegrationEvent>
{
    private readonly ChairOccupancyProjection _projection;
    private readonly ILogger<PatientPlacedInChairConsumer> _logger;
    /// <summary>
    /// HIS → PDMS boundary consumer. The HIS Front Desk publishes a chair placement when a
    /// waiting patient is moved into a treatment chair; PDMS captures it in the in-memory
    /// <see cref="ChairOccupancyProjection"/> so the chairside view can resolve patient
    /// context from a chair id before vitals arrive.
    /// </summary>
    /// <remarks>
    /// Persistent storage of chair occupancy is a deliberate follow-up: a PDMS-owned EF
    /// projection (with its own migration) is the natural shape, but the read model is
    /// in-memory for the demo loop the same way <c>EfManagerDashboardReadModel</c> sits
    /// alongside in-memory views in HIS. Restarts lose state — acceptable while the only
    /// consumer is the chairside dashboard, which can re-hydrate from the next placement.
    /// </remarks>
    public PatientPlacedInChairConsumer(ChairOccupancyProjection projection,
        ILogger<PatientPlacedInChairConsumer> logger)
    {
        _projection = projection;
        _logger = logger;
    }
    public Task HandleAsync(ConsumeContext<PatientPlacedInChairIntegrationEvent> context)
    {
        var message = context.Message;
        _projection.Place(message.PatientId, message.Chair, message.PlacedAtUtc);
        _logger.LogInformation(
            "Chair occupancy: patient {PatientId} placed on chair {Chair} at {PlacedAtUtc}.",
            message.PatientId, message.Chair, message.PlacedAtUtc);
        return Task.CompletedTask;
    }
}
