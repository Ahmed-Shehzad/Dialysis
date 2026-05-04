namespace Dialysis.SmartConnect.BuiltInPlugins;

/// <summary>
/// Route filter that always allows the message to continue.
/// </summary>
public sealed class AllowAllRouteFilter : IRouteFilter
{
    public const string KindValue = "allow-all";

    public string Kind => KindValue;

    public Task<RouteFilterResult> EvaluateAsync(IntegrationMessage message, CancellationToken cancellationToken) =>
        Task.FromResult(RouteFilterResult.Allow());
}
