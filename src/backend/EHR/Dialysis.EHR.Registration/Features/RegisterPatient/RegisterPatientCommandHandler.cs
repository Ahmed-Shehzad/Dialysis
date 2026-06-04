using System.Text.Json;
using Dialysis.BuildingBlocks.Transponder;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Registration.Domain;
using Dialysis.EHR.Registration.Ports;

namespace Dialysis.EHR.Registration.Features.RegisterPatient;

public sealed class RegisterPatientCommandHandler : ICommandHandler<RegisterPatientCommand, Guid>
{
    private readonly IPatientRepository _patients;
    private readonly ITransponderOutbox _outbox;
    private readonly IUnitOfWork _unitOfWork;
    public RegisterPatientCommandHandler(IPatientRepository patients,
        ITransponderOutbox outbox,
        IUnitOfWork unitOfWork)
    {
        _patients = patients;
        _outbox = outbox;
        _unitOfWork = unitOfWork;
    }
    public async Task<Guid> HandleAsync(RegisterPatientCommand request, CancellationToken cancellationToken)
    {
        if (await _patients.FindByMedicalRecordNumberAsync(request.MedicalRecordNumber, cancellationToken).ConfigureAwait(false) is not null)
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

        _patients.Add(patient);

        foreach (var @event in patient.IntegrationEvents)
        {
            var eventType = @event.GetType();
            var json = JsonSerializer.Serialize(@event, eventType);
            await _outbox.EnqueueAsync(new TransponderOutboxEnvelope(
                AssemblyQualifiedEventType: eventType.AssemblyQualifiedName ?? eventType.FullName!,
                PayloadJson: json,
                Id: @event.EventId),
                cancellationToken).ConfigureAwait(false);
        }
        patient.ClearIntegrationEvents();

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
