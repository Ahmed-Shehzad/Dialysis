namespace Dialysis.HIS.Contracts.Security;

/// <summary>
/// Legacy HIS-local marker preserved for backwards-source-compatibility with handlers/commands written before
/// the shared <see cref="Dialysis.Module.Contracts.Authorization.IPermissionedCommand"/> existed. Inherits from the
/// shared abstraction so <c>Dialysis.Module.Hosting</c>'s authorization pipeline behavior recognises it.
/// </summary>
public interface IPermissionedCommand : Module.Contracts.Authorization.IPermissionedCommand
{
}
