using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.BuildingBlocks.Fhir.Subscriptions;

public static class FhirSubscriptionEndpointExtensions
{
    /// <summary>
    /// Maps the FHIR <c>Subscription</c> management endpoints — the single controlled write
    /// exception to the v1 "no writes on resource routes" rule, since clients must be able to
    /// register interest. Also exposes the per-host <c>SubscriptionTopic</c> catalog.
    /// </summary>
    public static IEndpointRouteBuilder MapFhirSubscriptionEndpoints(
        this IEndpointRouteBuilder endpoints,
        string baseUrl = "/fhir",
        string? requireScope = null)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        var prefix = baseUrl.TrimEnd('/');

        var routes = new List<IEndpointConventionBuilder>
        {
            endpoints.MapPost(prefix + "/Subscription", CreateAsync),
            endpoints.MapGet(prefix + "/Subscription/{id}", GetAsync),
            endpoints.MapDelete(prefix + "/Subscription/{id}", DeleteAsync),
            endpoints.MapGet(prefix + "/SubscriptionTopic", ListTopicsAsync),
            endpoints.MapGet(prefix + "/SubscriptionTopic/{name}", GetTopicAsync),
        };

        if (!string.IsNullOrWhiteSpace(requireScope))
        {
            foreach (var route in routes)
            {
                route.RequireAuthorization(requireScope);
            }
        }

        return endpoints;
    }

    private static async Task CreateAsync(HttpContext context)
    {
        var catalog = context.RequestServices.GetRequiredService<SubscriptionTopicCatalog>();
        var registry = context.RequestServices.GetRequiredService<ISubscriptionRegistry>();

        var payload = await JsonSerializer.DeserializeAsync<SubscriptionCreateRequest>(
            context.Request.Body,
            cancellationToken: context.RequestAborted).ConfigureAwait(false);

        if (payload is null || string.IsNullOrWhiteSpace(payload.Topic) || string.IsNullOrWhiteSpace(payload.ChannelEndpoint))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        if (!catalog.TryGet(payload.Topic, out _))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsync($"Unknown topic '{payload.Topic}'.", context.RequestAborted).ConfigureAwait(false);
            return;
        }

        if (!Enum.TryParse<SubscriptionChannelType>(payload.ChannelType, ignoreCase: true, out var channelType))
        {
            channelType = SubscriptionChannelType.RestHook;
        }

        var registration = new FhirSubscriptionRegistration(
            Id: Guid.NewGuid().ToString("N"),
            TopicUrl: payload.Topic,
            ChannelType: channelType,
            ChannelEndpoint: payload.ChannelEndpoint,
            ChannelHeader: payload.Secret,
            FilterParameters: payload.Filters ?? new Dictionary<string, string>(StringComparer.Ordinal),
            Status: SubscriptionStatus.Active);

        await registry.RegisterAsync(registration, context.RequestAborted).ConfigureAwait(false);
        context.Response.Headers.Location = $"/fhir/Subscription/{registration.Id}";
        context.Response.StatusCode = StatusCodes.Status201Created;
        await context.Response.WriteAsJsonAsync(registration, context.RequestAborted).ConfigureAwait(false);
    }

    private static async Task GetAsync(HttpContext context, string id)
    {
        var registry = context.RequestServices.GetRequiredService<ISubscriptionRegistry>();
        var registration = await registry.GetAsync(id, context.RequestAborted).ConfigureAwait(false);
        if (registration is null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }
        context.Response.StatusCode = StatusCodes.Status200OK;
        await context.Response.WriteAsJsonAsync(registration, context.RequestAborted).ConfigureAwait(false);
    }

    private static async Task DeleteAsync(HttpContext context, string id)
    {
        var registry = context.RequestServices.GetRequiredService<ISubscriptionRegistry>();
        await registry.DeleteAsync(id, context.RequestAborted).ConfigureAwait(false);
        context.Response.StatusCode = StatusCodes.Status204NoContent;
    }

    private static async Task ListTopicsAsync(HttpContext context)
    {
        var catalog = context.RequestServices.GetRequiredService<SubscriptionTopicCatalog>();
        context.Response.StatusCode = StatusCodes.Status200OK;
        await context.Response.WriteAsJsonAsync(catalog.Topics, context.RequestAborted).ConfigureAwait(false);
    }

    private static async Task GetTopicAsync(HttpContext context, string name)
    {
        var catalog = context.RequestServices.GetRequiredService<SubscriptionTopicCatalog>();
        // `name` is the last URL segment used when the topic URL ends with a stable slug.
        var match = catalog.Topics.FirstOrDefault(t => t.Url.EndsWith('/' + name, StringComparison.Ordinal) || t.Url == name);
        if (match is null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }
        context.Response.StatusCode = StatusCodes.Status200OK;
        await context.Response.WriteAsJsonAsync(match, context.RequestAborted).ConfigureAwait(false);
    }

    private sealed record SubscriptionCreateRequest(
        string Topic,
        string ChannelType,
        string ChannelEndpoint,
        string? Secret,
        Dictionary<string, string>? Filters);
}
