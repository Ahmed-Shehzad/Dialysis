namespace Dialysis.CQRS;

/// <summary>
/// Sentinel return type for commands that do not produce a domain payload.
/// </summary>
public readonly record struct Unit
{
    public static Unit Value => default;
}
