using Dialysis.SharedKernel.ValueObjects;

namespace Dialysis.SharedKernel.Abstractions;

/// <summary>
/// Provides the current tenant context. Implemented by the host (e.g. Gateway).
/// </summary>
public interface ITenantContext
{
    TenantId TenantId { get; }
}
