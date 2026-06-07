using System.Text.Json;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.SmartConnect.Management.AspNetCore;

/// <summary>Maps <c>/api/v1/admin/groups/*</c> CRUD routes.</summary>
public static class GroupEndpointExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    extension(IEndpointRouteBuilder endpoints)
    {
        public IEndpointRouteBuilder MapSmartConnectGroupRoutes()
        {
            var group = endpoints.MapGroup("/api/v1/admin/groups").WithTags("SmartConnect Admin");

            group.MapGet(
                    "/",
                    async (SmartConnectDbContext db, CancellationToken ct) =>
                    {
                        var groups = await db.FlowGroups.ToListAsync(ct).ConfigureAwait(false);
                        return Results.Ok(groups.Select(g => new FlowGroup
                        {
                            Id = g.Id,
                            Name = g.Name,
                            Description = g.Description,
                        }));
                    })
                .WithName("SmartConnect_ListGroups");

            group.MapGet(
                    "/{groupId:guid}/export",
                    async (Guid groupId, SmartConnectDbContext db, CancellationToken ct) =>
                    {
                        var entity = await db.FlowGroups.AsNoTracking()
                            .FirstOrDefaultAsync(g => g.Id == groupId, ct).ConfigureAwait(false);
                        if (entity is null)
                        {
                            return Results.NotFound();
                        }

                        var dto = new FlowGroup
                        {
                            Id = entity.Id,
                            Name = entity.Name,
                            Description = entity.Description,
                        };
                        var json = JsonSerializer.Serialize(dto, _jsonOptions);
                        return Results.Text(json, "application/json");
                    })
                .WithName("SmartConnect_ExportGroup");

            group.MapPost(
                    "/",
                    async (FlowGroup body, SmartConnectDbContext db, CancellationToken ct) =>
                    {
                        var entity = new FlowGroupEntity
                        {
                            Id = body.Id == Guid.Empty ? Guid.NewGuid() : body.Id,
                            Name = body.Name,
                            Description = body.Description,
                        };
                        db.FlowGroups.Add(entity);
                        await db.SaveChangesAsync(ct).ConfigureAwait(false);
                        return Results.Created($"/api/v1/admin/groups/{entity.Id}",
                            new FlowGroup { Id = entity.Id, Name = entity.Name, Description = entity.Description });
                    })
                .WithName("SmartConnect_CreateGroup");

            group.MapDelete(
                    "/{groupId:guid}",
                    async (Guid groupId, SmartConnectDbContext db, CancellationToken ct) =>
                    {
                        var deleted = await db.FlowGroups.Where(g => g.Id == groupId)
                            .ExecuteDeleteAsync(ct).ConfigureAwait(false);
                        return deleted > 0 ? Results.NoContent() : Results.NotFound();
                    })
                .WithName("SmartConnect_DeleteGroup");

            return endpoints;
        }
    }
}
