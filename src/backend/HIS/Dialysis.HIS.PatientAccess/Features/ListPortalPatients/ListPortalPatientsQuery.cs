using Dialysis.BuildingBlocks.Hipaa.Audit;
using Dialysis.CQRS.Queries;
using Dialysis.HIS.Contracts.Security;
using Dialysis.HIS.PatientAccess.Ports;

namespace Dialysis.HIS.PatientAccess.Features.ListPortalPatients;

/// <summary>
/// Lists patient ids that have portal-relevant HIS data, so the patient portal (which is scoped to a
/// single patient and otherwise needs a patient claim) can discover a patient to open. Gated by the
/// same portal-read permission as the summary it feeds into.
/// </summary>
[PhiAccess(PhiAccessAction.Read, "Patient")]
public sealed record ListPortalPatientsQuery : IQuery<IReadOnlyList<Guid>>, IPermissionedCommand
{
    /// <summary>Creates the query.</summary>
    public ListPortalPatientsQuery(int Take = 50) => this.Take = Take;

    /// <inheritdoc />
    public string RequiredPermission => HisPermissions.PatientPortalRead;

    /// <summary>Maximum number of patient ids to return.</summary>
    public int Take { get; init; }

    /// <summary>Deconstructs the query.</summary>
    public void Deconstruct(out int take) => take = Take;
}

/// <summary>Returns the discoverable portal patient ids from the read model.</summary>
public sealed class ListPortalPatientsQueryHandler : IQueryHandler<ListPortalPatientsQuery, IReadOnlyList<Guid>>
{
    private readonly IPatientPortalReadModel _readModel;

    /// <summary>Creates the handler.</summary>
    public ListPortalPatientsQueryHandler(IPatientPortalReadModel readModel) => _readModel = readModel;

    /// <inheritdoc />
    public Task<IReadOnlyList<Guid>> HandleAsync(ListPortalPatientsQuery request, CancellationToken cancellationToken)
        => _readModel.ListPatientIdsAsync(request.Take, cancellationToken);
}
