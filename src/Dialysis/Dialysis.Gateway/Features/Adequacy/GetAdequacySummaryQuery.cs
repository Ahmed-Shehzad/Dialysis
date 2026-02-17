using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.Adequacy;

public sealed record GetAdequacySummaryQuery(string PatientId) : IQuery<AdequacySummaryDto>;

public sealed record AdequacySummaryDto(
    string PatientId,
    AdequacyValueDto? Urr,
    AdequacyValueDto? KtV,
    AdequacyValueDto? Hemoglobin,
    AdequacyValueDto? Ferritin,
    AdequacyValueDto? Tsat,
    AdequacyValueDto? Pth,
    AdequacyValueDto? Albumin,
    AdequacyValueDto? Potassium);

public sealed record AdequacyValueDto(decimal? Value, string? Unit, DateTimeOffset Effective);
