using System.Diagnostics.CodeAnalysis;

namespace Dialysis.BuildingBlocks.DurableCommandBus;

/// <summary>
/// In-memory <see cref="IDurableCommandCatalog"/>. Populated at composition time by
/// <c>DurableCommandsBuilder.RegisterCommand</c>; immutable after host startup.
/// </summary>
public sealed class DurableCommandCatalog : IDurableCommandCatalog
{
    private readonly Dictionary<string, DurableCommandRegistration> _byTypeKey;
    private readonly Dictionary<Type, DurableCommandRegistration> _byClrType;

    public DurableCommandCatalog(IEnumerable<DurableCommandRegistration> registrations)
    {
        ArgumentNullException.ThrowIfNull(registrations);
        _byTypeKey = new(StringComparer.Ordinal);
        _byClrType = [];
        foreach (var r in registrations)
        {
            _byTypeKey[r.CommandTypeKey] = r;
            _byClrType[r.CommandType] = r;
        }
        All = [.. _byTypeKey.Values];
    }

    public IReadOnlyList<DurableCommandRegistration> All { get; }

    public bool TryGet(string commandTypeKey, [NotNullWhen(true)] out DurableCommandRegistration? registration) =>
        _byTypeKey.TryGetValue(commandTypeKey, out registration);

    public bool TryGetForType(Type commandType, [NotNullWhen(true)] out DurableCommandRegistration? registration) =>
        _byClrType.TryGetValue(commandType, out registration);
}
