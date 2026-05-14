using Dialysis.CQRS.Queries;
using Dialysis.Hie.Consent.Domain;
using Dialysis.Hie.Contracts.Security;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.Hie.Consent.Features.ListConsentsForPatient;

public sealed record ListConsentsForPatientQuery(Guid PatientId)
    : IQuery<IReadOnlyList<ConsentDto>>, IPermissionedCommand
{
    public string RequiredPermission => HiePermissions.ConsentManage;
}

public sealed record ConsentDto(
    Guid Id,
    Guid PatientId,
    string PartnerId,
    string Scope,
    ConsentDirection Direction,
    DateTime EffectiveFromUtc,
    DateTime? EffectiveToUtc,
    DateTime? RevokedAtUtc);
