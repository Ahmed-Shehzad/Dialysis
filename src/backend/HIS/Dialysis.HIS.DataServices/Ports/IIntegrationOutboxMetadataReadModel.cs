namespace Dialysis.HIS.DataServices.Ports;

public sealed record IntegrationOutboxMetadataRow
{
    public IntegrationOutboxMetadataRow(Guid Id,
        string AssemblyQualifiedEventType,
        DateTime CreatedAtUtc,
        DateTime? ProcessedAtUtc,
        string? CorrelationId)
    {
        this.Id = Id;
        this.AssemblyQualifiedEventType = AssemblyQualifiedEventType;
        this.CreatedAtUtc = CreatedAtUtc;
        this.ProcessedAtUtc = ProcessedAtUtc;
        this.CorrelationId = CorrelationId;
    }
    public Guid Id { get; init; }
    public string AssemblyQualifiedEventType { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? ProcessedAtUtc { get; init; }
    public string? CorrelationId { get; init; }
    public void Deconstruct(out Guid Id, out string AssemblyQualifiedEventType, out DateTime CreatedAtUtc, out DateTime? ProcessedAtUtc, out string? CorrelationId)
    {
        Id = this.Id;
        AssemblyQualifiedEventType = this.AssemblyQualifiedEventType;
        CreatedAtUtc = this.CreatedAtUtc;
        ProcessedAtUtc = this.ProcessedAtUtc;
        CorrelationId = this.CorrelationId;
    }
}

public interface IIntegrationOutboxMetadataReadModel
{
    Task<IReadOnlyList<IntegrationOutboxMetadataRow>> ListRecentAsync(int take, CancellationToken cancellationToken = default);
}
