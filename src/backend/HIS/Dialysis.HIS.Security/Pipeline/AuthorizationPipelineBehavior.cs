using Dialysis.BuildingBlocks.Intercessor;
using Dialysis.HIS.Contracts.Security;

namespace Dialysis.HIS.Security.Pipeline;

public sealed class AuthorizationPipelineBehavior<TRequest, TResponse>(
    Authorization.IHisAuthorizationService authorization)
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
