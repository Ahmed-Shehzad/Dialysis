using BuildingBlocks.ValueObjects;

using Intercessor.Abstractions;

namespace Dialysis.Prescription.Application.Features.GetPrescriptions;

/// <summary>
/// FHIR search params: subject, patient (both = Patient MRN).
/// </summary>
public sealed record GetPrescriptionsQuery(int Limit = 1000, MedicalRecordNumber? Subject = null) : IQuery<GetPrescriptionsResponse>;
