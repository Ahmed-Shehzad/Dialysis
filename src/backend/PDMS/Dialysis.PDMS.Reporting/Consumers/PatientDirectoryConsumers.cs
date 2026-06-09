using Dialysis.BuildingBlocks.Transponder;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Contracts.Integration;
using Dialysis.PDMS.Core.Persistence;
using Dialysis.PDMS.Reporting.Directory;

namespace Dialysis.PDMS.Reporting.Consumers;

/// <summary>
/// Shared upsert for the EHR-fed <see cref="PatientDirectoryEntry"/> cache. PDMS doesn't own patient
/// identity; these boundary consumers mirror just enough (name + MRN + DOB) for the background report
/// builder to print a real name on the session PDFs.
/// </summary>
internal static class PatientDirectoryWriter
{
    public static async Task UpsertAsync(
        IPdmsRepository<PatientDirectoryEntry, Guid> directory,
        IUnitOfWork unitOfWork,
        Guid patientId,
        string medicalRecordNumber,
        string givenName,
        string familyName,
        DateOnly? dateOfBirth,
        DateTime updatedAtUtc,
        CancellationToken cancellationToken)
    {
        var existing = await directory.GetByIdAsync(patientId, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            await directory.AddAsync(
                PatientDirectoryEntry.From(patientId, medicalRecordNumber, givenName, familyName, dateOfBirth, updatedAtUtc),
                cancellationToken).ConfigureAwait(false);
        }
        else
        {
            existing.Update(medicalRecordNumber, givenName, familyName, dateOfBirth, updatedAtUtc);
            directory.Update(existing);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>EHR → PDMS: a newly registered patient is cached with full demographics (incl. DOB).</summary>
public sealed class PatientRegisteredDirectoryConsumer : IConsumer<PatientRegisteredIntegrationEvent>
{
    private readonly IPdmsRepository<PatientDirectoryEntry, Guid> _directory;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>Creates the consumer.</summary>
    public PatientRegisteredDirectoryConsumer(
        IPdmsRepository<PatientDirectoryEntry, Guid> directory, IUnitOfWork unitOfWork)
    {
        _directory = directory;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public Task HandleAsync(ConsumeContext<PatientRegisteredIntegrationEvent> context)
    {
        var m = context.Message;
        return PatientDirectoryWriter.UpsertAsync(
            _directory, _unitOfWork, m.PatientId, m.MedicalRecordNumber, m.GivenName, m.FamilyName,
            m.DateOfBirth, m.OccurredOn, context.CancellationToken);
    }
}

/// <summary>EHR → PDMS: a demographics change refreshes the cached name/MRN (DOB is not carried, so it's kept).</summary>
public sealed class PatientDemographicsUpdatedDirectoryConsumer : IConsumer<PatientDemographicsUpdatedIntegrationEvent>
{
    private readonly IPdmsRepository<PatientDirectoryEntry, Guid> _directory;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>Creates the consumer.</summary>
    public PatientDemographicsUpdatedDirectoryConsumer(
        IPdmsRepository<PatientDirectoryEntry, Guid> directory, IUnitOfWork unitOfWork)
    {
        _directory = directory;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public Task HandleAsync(ConsumeContext<PatientDemographicsUpdatedIntegrationEvent> context)
    {
        var m = context.Message;
        return PatientDirectoryWriter.UpsertAsync(
            _directory, _unitOfWork, m.PatientId, m.MedicalRecordNumber, m.GivenName, m.FamilyName,
            dateOfBirth: null, m.OccurredOn, context.CancellationToken);
    }
}

/// <summary>EHR → PDMS: a merge drops the superseded id's cached entry; the surviving entry stands on its own.</summary>
public sealed class PatientsMergedDirectoryConsumer : IConsumer<PatientsMergedIntegrationEvent>
{
    private readonly IPdmsRepository<PatientDirectoryEntry, Guid> _directory;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>Creates the consumer.</summary>
    public PatientsMergedDirectoryConsumer(
        IPdmsRepository<PatientDirectoryEntry, Guid> directory, IUnitOfWork unitOfWork)
    {
        _directory = directory;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task HandleAsync(ConsumeContext<PatientsMergedIntegrationEvent> context)
    {
        var superseded = await _directory.GetByIdAsync(context.Message.SupersededPatientId, context.CancellationToken)
            .ConfigureAwait(false);
        if (superseded is null) return;

        _directory.Remove(superseded);
        await _unitOfWork.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);
    }
}
