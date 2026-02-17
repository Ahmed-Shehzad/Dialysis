using Dialysis.DeviceIngestion.Features.Hl7.Stream;
using Dialysis.Persistence.Abstractions;
using Dialysis.SharedKernel.Abstractions;

using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.Hl7;

public sealed class RetryFailedHl7CommandHandler : ICommandHandler<RetryFailedHl7Command, RetryFailedHl7Result>
{
    private readonly IFailedHl7MessageStore _store;
    private readonly ISender _sender;
    private readonly ITenantContext _tenantContext;

    public RetryFailedHl7CommandHandler(IFailedHl7MessageStore store, ISender sender, ITenantContext tenantContext)
    {
        _store = store;
        _sender = sender;
        _tenantContext = tenantContext;
    }

    public async Task<RetryFailedHl7Result> HandleAsync(RetryFailedHl7Command request, CancellationToken cancellationToken = default)
    {
        if (!Ulid.TryParse(request.Id, out var ulid))
            return new RetryFailedHl7Result(null, NotFound: true);

        var tenantId = _tenantContext.TenantId;
        var failed = await _store.GetByIdAsync(tenantId, ulid, cancellationToken);
        if (failed is null)
            return new RetryFailedHl7Result(null, NotFound: true);

        await _store.IncrementRetryCountAsync(failed, cancellationToken);

        var command = new ProcessHl7StreamCommand(failed.RawMessage);
        var result = await _sender.SendAsync(command, cancellationToken);

        if (!result.Failed)
            await _store.DeleteAsync(failed, cancellationToken);

        return new RetryFailedHl7Result(result, NotFound: false);
    }
}
