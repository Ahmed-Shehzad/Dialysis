using System.Text.Json;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Entities;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Json;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.SmartConnect.Persistence.EntityFrameworkCore;

public sealed class EfIntegrationFlowRepository(SmartConnectDbContext db) : IIntegrationFlowRepository
{
    public async Task<IntegrationFlow?> GetByIdAsync(Guid flowId, CancellationToken cancellationToken)
    {
        var row = await db.IntegrationFlows.AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == flowId, cancellationToken)
            .ConfigureAwait(false);
        return row is null ? null : Map(row);
    }

    public async Task<IReadOnlyList<IntegrationFlow>> GetAllAsync(CancellationToken cancellationToken)
    {
        var rows = await db.IntegrationFlows.AsNoTracking()
            .OrderBy(f => f.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return rows.Select(Map).ToList();
    }

    public async Task AddAsync(IntegrationFlow flow, CancellationToken cancellationToken)
    {
        db.IntegrationFlows.Add(
            new IntegrationFlowEntity
            {
                Id = flow.Id,
                Name = flow.Name,
                RuntimeState = (int)flow.RuntimeState,
                PipelineJson = PipelineJsonSerializer.Serialize(flow.Pipeline),
                TagsJson = flow.Tags.Count > 0 ? JsonSerializer.Serialize(flow.Tags) : null,
                GroupId = flow.GroupId,
                Description = flow.Description,
            });
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> UpdateAsync(IntegrationFlow flow, CancellationToken cancellationToken)
    {
        var row = await db.IntegrationFlows.FirstOrDefaultAsync(f => f.Id == flow.Id, cancellationToken)
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
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid flowId, CancellationToken cancellationToken)
    {
        var row = await db.IntegrationFlows.FirstOrDefaultAsync(f => f.Id == flowId, cancellationToken)
            .ConfigureAwait(false);
        if (row is null)
        {
            return false;
        }

        db.IntegrationFlows.Remove(row);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<bool> SetRuntimeStateAsync(
        Guid flowId,
        FlowRuntimeState state,
        CancellationToken cancellationToken)
    {
        var row = await db.IntegrationFlows.FirstOrDefaultAsync(f => f.Id == flowId, cancellationToken)
            .ConfigureAwait(false);
        if (row is null)
        {
            return false;
        }

        row.RuntimeState = (int)state;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    private static IntegrationFlow Map(IntegrationFlowEntity row)
    {
        var pipeline = PipelineJsonSerializer.Deserialize(row.PipelineJson);
        var tags = string.IsNullOrWhiteSpace(row.TagsJson)
            ? []
            : JsonSerializer.Deserialize<List<string>>(row.TagsJson) ?? [];
        return new IntegrationFlow
        {
            Id = row.Id,
            Name = row.Name,
            RuntimeState = (FlowRuntimeState)row.RuntimeState,
            Pipeline = pipeline,
            Tags = tags,
            GroupId = row.GroupId,
            Description = row.Description,
        };
    }
}
