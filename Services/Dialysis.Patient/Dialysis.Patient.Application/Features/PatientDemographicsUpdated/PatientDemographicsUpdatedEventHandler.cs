using BuildingBlocks.Abstractions;

using Dialysis.Patient.Application.Domain.Events;

using Microsoft.Extensions.Logging;

namespace Dialysis.Patient.Application.Features.PatientDemographicsUpdated;

internal sealed class PatientDemographicsUpdatedEventHandler : IDomainEventHandler<PatientDemographicsUpdatedEvent>
{
    private readonly ILogger<PatientDemographicsUpdatedEventHandler> _logger;

    public PatientDemographicsUpdatedEventHandler(ILogger<PatientDemographicsUpdatedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(PatientDemographicsUpdatedEvent notification, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "DomainEvent: PatientDemographicsUpdated PatientId={PatientId} Name={FirstName} {LastName}",
            notification.PatientId,
            notification.Name.FirstName,
            notification.Name.LastName);

        return Task.CompletedTask;
    }
}
