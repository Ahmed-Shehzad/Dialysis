using Intercessor.Abstractions;

namespace Dialysis.Prescription.Application.Features.GetPrescriptionByMrn;

/// <summary>
/// Query for prescription by Medical Record Number (MDC_HDIALY_RX_QUERY).
/// </summary>
public sealed record GetPrescriptionByMrnQuery(string Mrn) : IQuery<GetPrescriptionByMrnResponse?>;
