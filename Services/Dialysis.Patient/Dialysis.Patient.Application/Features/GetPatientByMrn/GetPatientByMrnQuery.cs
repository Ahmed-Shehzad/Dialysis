using BuildingBlocks.ValueObjects;

using Intercessor.Abstractions;

namespace Dialysis.Patient.Application.Features.GetPatientByMrn;

public sealed record GetPatientByMrnQuery(MedicalRecordNumber Mrn) : IQuery<GetPatientByMrnResponse?>;
