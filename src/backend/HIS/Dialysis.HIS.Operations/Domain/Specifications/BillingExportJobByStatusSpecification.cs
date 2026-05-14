using System.Linq.Expressions;
using Dialysis.DomainDrivenDesign.Specifications;
using Dialysis.HIS.Operations.Domain.Enumerations;

namespace Dialysis.HIS.Operations.Domain.Specifications;

/// <summary>
/// Specification: selects <see cref="BillingExportJob"/> entries by <see cref="BillingExportJobStatus"/>.
/// Translates to EF via <c>ListAsync(spec)</c> or evaluates in memory via <c>IsSatisfiedBy</c>.
/// </summary>
public sealed class BillingExportJobByStatusSpecification(BillingExportJobStatus status)
    : Specification<BillingExportJob>
{
    public BillingExportJobStatus Status { get; } = status;

    public override Expression<Func<BillingExportJob, bool>> ToExpression()
        => job => job.Status == Status;
}
