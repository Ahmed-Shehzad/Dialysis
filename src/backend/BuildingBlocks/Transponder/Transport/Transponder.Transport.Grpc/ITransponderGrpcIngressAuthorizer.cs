using Grpc.Core;

namespace Dialysis.BuildingBlocks.Transponder.Transport.Grpc;

/// <summary>Optional production hook: validate TLS identity, JWT, mTLS, or other policy before <see cref="TransponderGrpcIngressService"/> handles a call.</summary>
public interface ITransponderGrpcIngressAuthorizer
{
    /// <summary>Throw <see cref="RpcException"/> with <see cref="StatusCode.Unauthenticated"/> or <see cref="StatusCode.PermissionDenied"/> to reject the call.</summary>
    ValueTask AuthorizeAsync(ServerCallContext context, CancellationToken cancellationToken = default);
}
