using Dialysis.SmartConnect.Persistence.EntityFrameworkCore;
using Dialysis.SmartConnect.Scripts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dialysis.SmartConnect.Management.AspNetCore;

/// <summary>Flow lifecycle routes (<c>/flows/{id}/start|stop|pause</c>, including cascade start and dependency enforcement).</summary>
public static partial class ManagementEndpointExtensions
{
    internal static void MapFlowLifecycleEndpoints(RouteGroupBuilder admin)
    {
        admin.MapPost(
                "/flows/{flowId:guid}/start",
                async (
                    Guid flowId,
                    bool? force,
                    bool? cascade,
                    IIntegrationFlowRepository repo,
                    ChannelScriptExecutor scripts,
                    CancellationToken ct) =>
                {
                    var flow = await repo.GetByIdAsync(flowId, ct).ConfigureAwait(false);
                    if (flow is null)
                    {
                        return Results.NotFound();
                    }

                    // Cascade Start — depth-first walk over Dependencies, Starting any flow that
                    // isn't already Started before the requested flow. Cycles refuse with 409 +
                    // the cycle path so the operator can fix the declaration. Cascade implies
                    // force at the leaf level: once we've decided to start a dep, its own deps
                    // also get started, not 422-blocked.
                    if (cascade == true)
                    {
                        var visited = new HashSet<Guid>();
                        var stack = new HashSet<Guid>();
                        var order = new List<IntegrationFlow>();
                        var cyclePath = new List<string>();

                        async Task<bool> WalkAsync(IntegrationFlow current)
                        {
                            if (!stack.Add(current.Id))
                            {
                                cyclePath.Add($"{current.Name} ({current.Id})");
                                return false;
                            }
                            foreach (var depId in current.Dependencies)
                            {
                                if (visited.Contains(depId))
                                {
                                    continue;
                                }
                                var dep = await repo.GetByIdAsync(depId, ct).ConfigureAwait(false);
                                if (dep is null)
                                {
                                    continue; // skip missing — Start will fail naturally if it matters
                                }
                                if (!await WalkAsync(dep).ConfigureAwait(false))
                                {
                                    cyclePath.Add($"{current.Name} ({current.Id})");
                                    return false;
                                }
                            }
                            stack.Remove(current.Id);
                            visited.Add(current.Id);
                            order.Add(current);
                            return true;
                        }

                        if (!await WalkAsync(flow).ConfigureAwait(false))
                        {
                            cyclePath.Reverse();
                            return Results.Conflict(new
                            {
                                error = "Dependency cycle detected; cannot cascade-start.",
                                cyclePath,
                            });
                        }

                        var started = new List<object>();
                        foreach (var f in order)
                        {
                            if (f.RuntimeState == FlowRuntimeState.Started)
                            {
                                continue;
                            }
                            if (!string.IsNullOrWhiteSpace(f.Pipeline.Scripts?.DeployScript))
                            {
                                scripts.RunLifecycleScript(f.Pipeline.Scripts.DeployScript!, f.Id);
                            }
                            var setOk = await repo.SetRuntimeStateAsync(f.Id, FlowRuntimeState.Started, ct)
                                .ConfigureAwait(false);
                            if (setOk)
                            {
                                started.Add(new { id = f.Id, name = f.Name });
                            }
                        }

                        return Results.Ok(new { started, count = started.Count });
                    }

                    // Dependency enforcement — refuse to Start unless every declared dependency is
                    // already Started, OR ?force=true is supplied (operator override for a known
                    // out-of-order startup, e.g. recovering after a crash).
                    if (force != true && flow.Dependencies.Count > 0)
                    {
                        var unmet = new List<object>();
                        foreach (var depId in flow.Dependencies)
                        {
                            var dep = await repo.GetByIdAsync(depId, ct).ConfigureAwait(false);
                            if (dep is null)
                            {
                                unmet.Add(new { id = depId, name = (string?)null, state = "missing" });
                            }
                            else if (dep.RuntimeState != FlowRuntimeState.Started)
                            {
                                unmet.Add(new
                                {
                                    id = dep.Id,
                                    name = dep.Name,
                                    state = dep.RuntimeState.ToString(),
                                });
                            }
                        }

                        if (unmet.Count > 0)
                        {
                            return Results.UnprocessableEntity(new
                            {
                                error = $"Cannot start flow '{flow.Name}': {unmet.Count} declared dependency/dependencies are not Started. Start them first or pass ?force=true.",
                                unmetDependencies = unmet,
                            });
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(flow.Pipeline.Scripts?.DeployScript))
                    {
                        scripts.RunLifecycleScript(flow.Pipeline.Scripts.DeployScript!, flowId);
                    }

                    var ok = await repo.SetRuntimeStateAsync(flowId, FlowRuntimeState.Started, ct)
                        .ConfigureAwait(false);
                    return ok ? Results.NoContent() : Results.NotFound();
                })
            .WithName("SmartConnect_StartFlow");

        admin.MapPost(
                "/flows/{flowId:guid}/stop",
                async (Guid flowId, IIntegrationFlowRepository repo, ChannelScriptExecutor scripts, CancellationToken ct) =>
                {
                    var flow = await repo.GetByIdAsync(flowId, ct).ConfigureAwait(false);
                    if (flow is null)
                    {
                        return Results.NotFound();
                    }

                    if (!string.IsNullOrWhiteSpace(flow.Pipeline.Scripts?.UndeployScript))
                    {
                        scripts.RunLifecycleScript(flow.Pipeline.Scripts.UndeployScript!, flowId);
                    }

                    var ok = await repo.SetRuntimeStateAsync(flowId, FlowRuntimeState.Stopped, ct)
                        .ConfigureAwait(false);
                    return ok ? Results.NoContent() : Results.NotFound();
                })
            .WithName("SmartConnect_StopFlow");

        admin.MapPost(
                "/flows/{flowId:guid}/pause",
                async (Guid flowId, IIntegrationFlowRepository repo, CancellationToken ct) =>
                {
                    var ok = await repo.SetRuntimeStateAsync(flowId, FlowRuntimeState.Paused, ct)
                        .ConfigureAwait(false);
                    return ok ? Results.NoContent() : Results.NotFound();
                })
            .WithName("SmartConnect_PauseFlow");
    }
}
