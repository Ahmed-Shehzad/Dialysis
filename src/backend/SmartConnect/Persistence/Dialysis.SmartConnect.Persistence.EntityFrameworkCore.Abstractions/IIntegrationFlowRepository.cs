namespace Dialysis.SmartConnect.Persistence;

public interface IIntegrationFlowRepository
{
    Task<IntegrationFlow?> GetByIdAsync(Guid flowId, CancellationToken cancellationToken);

    Task<IReadOnlyList<IntegrationFlow>> GetAllAsync(CancellationToken cancellationToken);

    Task AddAsync(IntegrationFlow flow, CancellationToken cancellationToken);

    Task<bool> UpdateAsync(IntegrationFlow flow, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid flowId, CancellationToken cancellationToken);

    Task<bool> SetRuntimeStateAsync(Guid flowId, FlowRuntimeState state, CancellationToken cancellationToken);
}
