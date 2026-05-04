namespace Dialysis.HIS.DataServices.Ports;

public sealed record IntegrationOutboxMetadataRow(
    Guid Id,
    string AssemblyQualifiedEventType,
    DateTime CreatedAtUtc,
    DateTime? ProcessedAtUtc,
    string? CorrelationId);

public interface IIntegrationOutboxMetadataReadModel
{
    Task<IReadOnlyList<IntegrationOutboxMetadataRow>> ListRecentAsync(int take, CancellationToken cancellationToken = default);
}
