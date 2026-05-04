namespace Dialysis.HIS.Contracts.Security;

/// <summary>Commands that require an authorization check before the handler runs.</summary>
public interface IPermissionedCommand
{
    string RequiredPermission { get; }
}
