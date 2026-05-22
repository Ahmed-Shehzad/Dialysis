namespace Dialysis.SmartConnect.Prescription;

/// <summary>
/// Port that turns a parsed <see cref="PrescriptionQuery"/> into a
/// <see cref="PrescriptionDocument"/>. Returns <c>null</c> when no prescription is on file
/// — the responder maps that to a <c>QAK NF</c> reply per IG §5.4.4. Production impl
/// lives in <c>Dialysis.SmartConnect.Api</c> and calls a PDMS endpoint over HTTP (per
/// the module-boundary invariant — SmartConnect must not project-reference PDMS
/// internals).
/// </summary>
public interface IPrescriptionResolver
{
    Task<PrescriptionDocument?> ResolveAsync(PrescriptionQuery query, CancellationToken cancellationToken = default);
}
