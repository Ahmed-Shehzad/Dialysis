namespace Dialysis.Module.Contracts.Authorization;

/// <summary>Marker for commands/queries that require an authorization check before the handler runs.</summary>
public interface IPermissionedCommand
{
    string RequiredPermission { get; }
}
