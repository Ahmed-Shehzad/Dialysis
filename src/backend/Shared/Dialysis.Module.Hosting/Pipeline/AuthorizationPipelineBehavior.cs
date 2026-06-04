using Dialysis.BuildingBlocks.Intercessor;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.Module.Hosting.Pipeline;

/// <summary>
/// Intercessor pipeline behavior that enforces <see cref="IPermissionedCommand.RequiredPermission"/>
/// on any request whose contract implements <see cref="IPermissionedCommand"/>.
/// </summary>
public sealed class AuthorizationPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, IPermissionedCommand
{
    private readonly IModuleAuthorizationService _authorization;
    /// <summary>
    /// Intercessor pipeline behavior that enforces <see cref="IPermissionedCommand.RequiredPermission"/>
    /// on any request whose contract implements <see cref="IPermissionedCommand"/>.
    /// </summary>
    public AuthorizationPipelineBehavior(IModuleAuthorizationService authorization) => _authorization = authorization;
    public async Task<TResponse> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        await _authorization.EnsurePermissionAsync(request.RequiredPermission, cancellationToken).ConfigureAwait(false);
        return await next().ConfigureAwait(false);
    }
}
