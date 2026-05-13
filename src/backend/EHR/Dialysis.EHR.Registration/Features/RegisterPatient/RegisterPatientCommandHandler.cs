using System.Text.Json;
using Dialysis.BuildingBlocks.Transponder;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Registration.Domain;
using Dialysis.EHR.Registration.Ports;

namespace Dialysis.EHR.Registration.Features.RegisterPatient;

public sealed class RegisterPatientCommandHandler(
    IPatientRepository patients,
    ITransponderOutbox outbox,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RegisterPatientCommand, Guid>
{
    public async Task<Guid> Handle(RegisterPatientCommand request, CancellationToken cancellationToken)
    {
        if (await patients.FindByMedicalRecordNumberAsync(request.MedicalRecordNumber, cancellationToken).ConfigureAwait(false) is not null)
            throw new InvalidOperationException($"MRN '{request.MedicalRecordNumber}' is already in use.");

        var id = Guid.CreateVersion7();
        var name = new HumanName(request.FamilyName, request.GivenName, request.MiddleName);
        var patient = Patient.Register(
            id,
            request.MedicalRecordNumber,
            name,
            request.DateOfBirth,
            request.SexAtBirthCode,
            request.PreferredLanguageCode);

        patients.Add(patient);

        foreach (var @event in patient.IntegrationEvents)
        {
            var eventType = @event.GetType();
            var json = JsonSerializer.Serialize(@event, eventType);
            await outbox.EnqueueAsync(new TransponderOutboxEnvelope(
                AssemblyQualifiedEventType: eventType.AssemblyQualifiedName ?? eventType.FullName!,
                PayloadJson: json,
                Id: @event.EventId),
                cancellationToken).ConfigureAwait(false);
        }
        patient.ClearIntegrationEvents();

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
