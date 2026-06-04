using Dialysis.BuildingBlocks.Transponder.Transport.RabbitMq;
using Dialysis.CQRS;
using Dialysis.Identity.Api;
using Dialysis.Identity.Composition;
using Dialysis.Identity.Contracts.Security;
using Dialysis.Identity.Provisioning.Features.AssignRoleToUser;
using Dialysis.Identity.Provisioning.Features.DeactivateUser;
using Dialysis.Identity.Provisioning.Features.DefineRole;
using Dialysis.Identity.Provisioning.Features.ListRoles;
using Dialysis.Identity.Provisioning.Features.ListUserPermissions;
using Dialysis.Identity.Provisioning.Features.ProvisionUser;
using Dialysis.Identity.Provisioning.Features.RevokeRoleFromUser;
using Dialysis.Module.Hosting;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

const string connectionStringName = "Identity";
var connectionString = builder.Configuration.GetConnectionString(connectionStringName);
var enableOutbox = builder.Configuration.GetValue("Identity:Transponder:EnableOutboxRelay", false);
var rabbitUri = builder.Configuration["Identity:Transponder:RabbitMq:ConnectionUri"];
var rabbitQueue = builder.Configuration["Identity:Transponder:RabbitMq:QueueName"];
var rabbitExchange = builder.Configuration["Identity:Transponder:RabbitMq:ExchangeName"];

builder.AddModuleHost<IdentityPermissionCatalog>(new ModuleHostingOptions
{
    ModuleSlug = "identity",
    HandlerAssemblies =
    [
        typeof(Dialysis.Identity.Provisioning.IdentityProvisioningMarker).Assembly
    ],
});

builder.Services.AddIdentity(
    builder.Configuration,
    configurePersistence: string.IsNullOrWhiteSpace(connectionString)
        ? null
        : options => options.UseNpgsql(
            connectionString,
            pg => pg.MigrationsHistoryTable("__ef_migrations", "identity")),
    enableOutboxRelay: enableOutbox,
    configureTransponderTransport: string.IsNullOrWhiteSpace(rabbitUri)
        ? null
        : s => s.AddTransponderRabbitMq(o =>
        {
            o.ConnectionUri = rabbitUri;
            if (!string.IsNullOrWhiteSpace(rabbitQueue)) o.QueueName = rabbitQueue;
            if (!string.IsNullOrWhiteSpace(rabbitExchange)) o.ExchangeName = rabbitExchange;
        }));

var app = builder.Build();

app.UseModuleHost();
app.MapOpenApi();

app.MapGet("/", () => Results.Ok(new { module = "identity", version = "v1" })).AllowAnonymous();

var users = app.MapGroup("/api/v1/users");

users.MapPost("/", async (ProvisionUserCommand command, ICqrsGateway gateway, CancellationToken ct) =>
{
    var id = await gateway.SendCommandAsync<ProvisionUserCommand, Guid>(command, ct).ConfigureAwait(false);
    return Results.Created($"/api/v1/users/{id}", new { id });
});

users.MapPost("/{userId:guid}/deactivate", async (Guid userId, ICqrsGateway gateway, CancellationToken ct) =>
{
    await gateway.SendCommandAsync<DeactivateUserCommand, Unit>(new DeactivateUserCommand(userId), ct).ConfigureAwait(false);
    return Results.NoContent();
});

users.MapGet("/{userId:guid}/permissions", async (Guid userId, ICqrsGateway gateway, CancellationToken ct) =>
{
    var dto = await gateway.SendQueryAsync<ListUserPermissionsQuery, UserPermissionsDto?>(
        new ListUserPermissionsQuery(userId), ct).ConfigureAwait(false);
    return dto is null ? Results.NotFound() : Results.Ok(dto);
});

users.MapPost("/{userId:guid}/roles", async (
    Guid userId,
    AssignRoleBody body,
    ICqrsGateway gateway,
    CancellationToken ct) =>
{
    await gateway.SendCommandAsync<AssignRoleToUserCommand, Unit>(
        new AssignRoleToUserCommand(userId, body.RoleCode), ct).ConfigureAwait(false);
    return Results.NoContent();
});

users.MapDelete("/{userId:guid}/roles/{roleCode}", async (
    Guid userId,
    string roleCode,
    ICqrsGateway gateway,
    CancellationToken ct) =>
{
    await gateway.SendCommandAsync<RevokeRoleFromUserCommand, Unit>(
        new RevokeRoleFromUserCommand(userId, roleCode), ct).ConfigureAwait(false);
    return Results.NoContent();
});

var roles = app.MapGroup("/api/v1/roles");

roles.MapPost("/", async (DefineRoleCommand command, ICqrsGateway gateway, CancellationToken ct) =>
{
    var id = await gateway.SendCommandAsync<DefineRoleCommand, Guid>(command, ct).ConfigureAwait(false);
    return Results.Created($"/api/v1/roles/{id}", new { id });
});

roles.MapGet("/", async (ICqrsGateway gateway, CancellationToken ct) =>
{
    var list = await gateway.SendQueryAsync<ListRolesQuery, IReadOnlyList<RoleSummaryDto>>(new ListRolesQuery(), ct)
        .ConfigureAwait(false);
    return Results.Ok(list);
});

await app.RunAsync().ConfigureAwait(false);

namespace Dialysis.Identity.Api
{
    /// <summary>Body for assigning a role to a user.</summary>
    public sealed record AssignRoleBody
    {
        /// <summary>Body for assigning a role to a user.</summary>
        public AssignRoleBody(string RoleCode) => this.RoleCode = RoleCode;
        public string RoleCode { get; init; }
        public void Deconstruct(out string roleCode) => roleCode = this.RoleCode;
    }

    /// <summary>Test factory marker.</summary>
    public partial class Program;
}