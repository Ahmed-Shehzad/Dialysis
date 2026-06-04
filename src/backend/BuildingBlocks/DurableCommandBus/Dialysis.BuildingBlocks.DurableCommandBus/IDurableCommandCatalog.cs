using System.Diagnostics.CodeAnalysis;

namespace Dialysis.BuildingBlocks.DurableCommandBus;

/// <summary>
/// Per-module allowlist of routable command types. The consumer ONLY runs commands listed
/// here; a malformed envelope claiming any other <c>CommandTypeKey</c> is dead-lettered.
/// Avoids the security hole of a blanket <c>Type.GetType(wireString)</c> + handler invoke.
/// </summary>
public interface IDurableCommandCatalog
{
    /// <summary>
    /// Returns the registration for a wire type key, or <c>false</c> when the type is not
    /// registered (consumer dead-letters; status endpoint 404s).
    /// </summary>
    bool TryGet(string commandTypeKey, [NotNullWhen(true)] out DurableCommandRegistration? registration);

    /// <summary>All registrations. Used by the bus to look up an outbound command's wire key.</summary>
    IReadOnlyList<DurableCommandRegistration> All { get; }

    /// <summary>Returns the registration for an outbound command's CLR type.</summary>
    bool TryGetForType(Type commandType, [NotNullWhen(true)] out DurableCommandRegistration? registration);
}
