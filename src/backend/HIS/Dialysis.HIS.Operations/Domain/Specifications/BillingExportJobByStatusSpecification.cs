using System.Linq.Expressions;
using Dialysis.DomainDrivenDesign.Specifications;
using Dialysis.HIS.Operations.Domain.Enumerations;

namespace Dialysis.HIS.Operations.Domain.Specifications;

/// <summary>
/// Specification: selects <see cref="BillingExportJob"/> entries by <see cref="BillingExportJobStatus"/>.
/// Translates to EF via <c>ListAsync(spec)</c> or evaluates in memory via <c>IsSatisfiedBy</c>.
/// </summary>
public sealed class BillingExportJobByStatusSpecification : Specification<BillingExportJob>
{
    /// <summary>
    /// Specification: selects <see cref="BillingExportJob"/> entries by <see cref="BillingExportJobStatus"/>.
    /// Translates to EF via <c>ListAsync(spec)</c> or evaluates in memory via <c>IsSatisfiedBy</c>.
    /// </summary>
    public BillingExportJobByStatusSpecification(BillingExportJobStatus status) => Status = status;
    public BillingExportJobStatus Status { get; }

    public override Expression<Func<BillingExportJob, bool>> ToExpression()
        => job => job.Status == Status;
}
