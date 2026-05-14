using Dialysis.DomainDrivenDesign.Exceptions;
using Dialysis.DomainDrivenDesign.Primitives;

namespace Dialysis.HIS.Operations.Domain.Enumerations;

/// <summary>
/// Lifecycle state of a <see cref="BillingExportJob"/>: <c>Queued</c> on submission,
/// <c>Completed</c> once EHR finishes the corresponding export, <c>Failed</c> on terminal error.
/// </summary>
public sealed class BillingExportJobStatus : Enumeration
{
    public static readonly BillingExportJobStatus Queued = new(1, "Queued");
    public static readonly BillingExportJobStatus Completed = new(2, "Completed");
    public static readonly BillingExportJobStatus Failed = new(3, "Failed");

    private BillingExportJobStatus(int value, string name) : base(value, name)
    {
    }

    public static BillingExportJobStatus FromName(string name) =>
        GetAll<BillingExportJobStatus>().FirstOrDefault(s => s.Name == name)
            ?? throw new DomainException($"Unknown BillingExportJobStatus '{name}'.");
}
