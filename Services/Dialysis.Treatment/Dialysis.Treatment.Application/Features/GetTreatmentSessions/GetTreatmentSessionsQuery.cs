using BuildingBlocks.ValueObjects;

using Intercessor.Abstractions;

namespace Dialysis.Treatment.Application.Features.GetTreatmentSessions;

/// <summary>
/// FHIR search params: subject, patient (MRN), date (or dateFrom/dateTo range).
/// </summary>
public sealed record GetTreatmentSessionsQuery(
    int Limit = 500,
    MedicalRecordNumber? Subject = null,
    DateTimeOffset? DateFrom = null,
    DateTimeOffset? DateTo = null) : IQuery<GetTreatmentSessionsResponse>;
