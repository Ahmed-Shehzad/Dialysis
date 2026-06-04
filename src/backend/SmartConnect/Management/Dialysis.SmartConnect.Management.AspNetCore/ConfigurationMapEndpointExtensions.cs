using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dialysis.SmartConnect.Management.AspNetCore;

/// <summary>Maps <c>/smartconnect/v1/admin/config-map/*</c> CRUD routes for variable/configuration maps.</summary>
public static class ConfigurationMapEndpointExtensions
{
    extension(IEndpointRouteBuilder endpoints)
    {
        public IEndpointRouteBuilder MapSmartConnectConfigurationMapRoutes()
        {
            var group = endpoints.MapGroup("/smartconnect/v1/admin/config-map").WithTags("SmartConnect Admin");

            group.MapGet(
                    "/{scope}",
                    async (
                        string scope,
                        Guid? flowId,
                        IVariableMapStore store,
                        CancellationToken ct) =>
                    {
                        if (!TryParseScope(scope, out var parsed))
                            return Results.BadRequest(new { error = $"Invalid scope '{scope}'." });

                        var all = await store.GetAllAsync(parsed, flowId, ct).ConfigureAwait(false);
                        return Results.Ok(all);
                    })
                .WithName("SmartConnect_GetConfigMap");

            group.MapGet(
                    "/{scope}/{key}",
                    async (
                        string scope,
                        string key,
                        Guid? flowId,
                        IVariableMapStore store,
                        CancellationToken ct) =>
                    {
                        if (!TryParseScope(scope, out var parsed))
                            return Results.BadRequest(new { error = $"Invalid scope '{scope}'." });

                        var value = await store.GetAsync(parsed, flowId, key, ct).ConfigureAwait(false);
                        return value is null
                            ? Results.NotFound()
                            : Results.Ok(new { key, value });
                    })
                .WithName("SmartConnect_GetConfigMapEntry");

            group.MapPut(
                    "/{scope}/{key}",
                    async (
                        string scope,
                        string key,
                        Guid? flowId,
                        ConfigMapValueBody body,
                        IVariableMapStore store,
                        CancellationToken ct) =>
                    {
                        if (!TryParseScope(scope, out var parsed))
                            return Results.BadRequest(new { error = $"Invalid scope '{scope}'." });

                        await store.SetAsync(parsed, flowId, key, body.Value, ct).ConfigureAwait(false);
                        return Results.NoContent();
                    })
                .WithName("SmartConnect_SetConfigMapEntry");

            group.MapDelete(
                    "/{scope}/{key}",
                    async (
                        string scope,
                        string key,
                        Guid? flowId,
                        IVariableMapStore store,
                        CancellationToken ct) =>
                    {
                        if (!TryParseScope(scope, out var parsed))
                            return Results.BadRequest(new { error = $"Invalid scope '{scope}'." });

                        await store.RemoveAsync(parsed, flowId, key, ct).ConfigureAwait(false);
                        return Results.NoContent();
                    })
                .WithName("SmartConnect_DeleteConfigMapEntry");

            return endpoints;
        }
    }

    private static bool TryParseScope(string raw, out VariableMapScope scope)
    {
        scope = default;
        return Enum.TryParse(raw, ignoreCase: true, out scope);
    }

    public record ConfigMapValueBody
    {
        public ConfigMapValueBody(string Value) => this.Value = Value;
        public string Value { get; init; }
        public void Deconstruct(out string value) => value = this.Value;
    }
}
