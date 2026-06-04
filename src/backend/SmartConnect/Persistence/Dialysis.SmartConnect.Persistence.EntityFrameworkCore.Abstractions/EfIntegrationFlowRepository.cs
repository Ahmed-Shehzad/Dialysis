using System.Text.Json;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Entities;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Json;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.SmartConnect.Persistence.EntityFrameworkCore;

public sealed class EfIntegrationFlowRepository : IIntegrationFlowRepository
{
    private readonly SmartConnectDbContext _db;
    public EfIntegrationFlowRepository(SmartConnectDbContext db) => _db = db;
    public async Task<IntegrationFlow?> GetByIdAsync(Guid flowId, CancellationToken cancellationToken)
    {
        var row = await _db.IntegrationFlows.AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == flowId, cancellationToken)
            .ConfigureAwait(false);
        return row is null ? null : Map(row);
    }

    public async Task<IReadOnlyList<IntegrationFlow>> GetAllAsync(CancellationToken cancellationToken)
    {
        var rows = await _db.IntegrationFlows.AsNoTracking()
            .OrderBy(f => f.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return [.. rows.Select(Map)];
    }

    public async Task AddAsync(IntegrationFlow flow, CancellationToken cancellationToken)
    {
        _db.IntegrationFlows.Add(
            new IntegrationFlowEntity
            {
                Id = flow.Id,
                Name = flow.Name,
                RuntimeState = (int)flow.RuntimeState,
                PipelineJson = PipelineJsonSerializer.Serialize(flow.Pipeline),
                TagsJson = flow.Tags.Count > 0 ? JsonSerializer.Serialize(flow.Tags) : null,
                GroupId = flow.GroupId,
                Description = flow.Description,
                DataTypesJson = flow.DataTypes.Count > 0 ? JsonSerializer.Serialize(flow.DataTypes) : null,
                DependenciesJson = flow.Dependencies.Count > 0 ? JsonSerializer.Serialize(flow.Dependencies) : null,
                AttachmentsJson = flow.Attachments.Count > 0 ? JsonSerializer.Serialize(flow.Attachments) : null,
            });
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> UpdateAsync(IntegrationFlow flow, CancellationToken cancellationToken)
    {
        var row = await _db.IntegrationFlows.FirstOrDefaultAsync(f => f.Id == flow.Id, cancellationToken)
            .ConfigureAwait(false);
        if (row is null)
        {
            return false;
        }

        row.Name = flow.Name;
        row.RuntimeState = (int)flow.RuntimeState;
        row.PipelineJson = PipelineJsonSerializer.Serialize(flow.Pipeline);
        row.TagsJson = flow.Tags.Count > 0 ? JsonSerializer.Serialize(flow.Tags) : null;
        row.GroupId = flow.GroupId;
        row.Description = flow.Description;
        row.DataTypesJson = flow.DataTypes.Count > 0 ? JsonSerializer.Serialize(flow.DataTypes) : null;
        row.DependenciesJson = flow.Dependencies.Count > 0 ? JsonSerializer.Serialize(flow.Dependencies) : null;
        row.AttachmentsJson = flow.Attachments.Count > 0 ? JsonSerializer.Serialize(flow.Attachments) : null;
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid flowId, CancellationToken cancellationToken)
    {
        var row = await _db.IntegrationFlows.FirstOrDefaultAsync(f => f.Id == flowId, cancellationToken)
            .ConfigureAwait(false);
        if (row is null)
        {
            return false;
        }

        _db.IntegrationFlows.Remove(row);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<bool> SetRuntimeStateAsync(
        Guid flowId,
        FlowRuntimeState state,
        CancellationToken cancellationToken)
    {
        var row = await _db.IntegrationFlows.FirstOrDefaultAsync(f => f.Id == flowId, cancellationToken)
            .ConfigureAwait(false);
        if (row is null)
        {
            return false;
        }

        row.RuntimeState = (int)state;
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    private static IntegrationFlow Map(IntegrationFlowEntity row)
    {
        var pipeline = PipelineJsonSerializer.Deserialize(row.PipelineJson);
        var tags = string.IsNullOrWhiteSpace(row.TagsJson)
            ? []
            : JsonSerializer.Deserialize<List<string>>(row.TagsJson) ?? [];
        var dataTypes = string.IsNullOrWhiteSpace(row.DataTypesJson)
            ? []
            : JsonSerializer.Deserialize<List<string>>(row.DataTypesJson) ?? [];
        var dependencies = string.IsNullOrWhiteSpace(row.DependenciesJson)
            ? []
            : JsonSerializer.Deserialize<List<Guid>>(row.DependenciesJson) ?? [];
        var attachments = string.IsNullOrWhiteSpace(row.AttachmentsJson)
            ? []
            : JsonSerializer.Deserialize<List<ChannelAttachmentReference>>(row.AttachmentsJson) ?? [];
        return new IntegrationFlow
        {
            Id = row.Id,
            Name = row.Name,
            RuntimeState = (FlowRuntimeState)row.RuntimeState,
            Pipeline = pipeline,
            Tags = tags,
            GroupId = row.GroupId,
            Description = row.Description,
            DataTypes = dataTypes,
            Dependencies = dependencies,
            Attachments = attachments,
        };
    }
}
