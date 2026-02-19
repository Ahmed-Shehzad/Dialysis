using Intercessor.Abstractions;

namespace Dialysis.Patient.Application.Features.GetPatients;

/// <summary>
/// FHIR search params: _id, identifier (MRN), name (given|family), birthdate.
/// </summary>
public sealed record GetPatientsQuery(
    int Limit = 1000,
    string? Id = null,
    string? Identifier = null,
    string? Name = null,
    DateOnly? Birthdate = null) : IQuery<GetPatientsResponse>;
