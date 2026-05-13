using Dialysis.BuildingBlocks.Intercessor;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.Module.Hosting.Pipeline;

/// <summary>
/// Intercessor pipeline behavior that enforces <see cref="IPermissionedCommand.RequiredPermission"/>
/// on any request whose contract implements <see cref="IPermissionedCommand"/>.
/// </summary>
public sealed class AuthorizationPipelineBehavior<TRequest, TResponse>(
    IModuleAuthorizationService authorization)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, IPermissionedCommand
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        await authorization.EnsurePermissionAsync(request.RequiredPermission, cancellationToken).ConfigureAwait(false);
        return await next().ConfigureAwait(false);
    }
}
