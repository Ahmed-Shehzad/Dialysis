namespace Dialysis.SmartConnect.Tests.TestPlugins;

public sealed class DropAllRouteFilter : IRouteFilter
{
    public const string KindValue = "drop-all-test";

    public string Kind => KindValue;

    public Task<RouteFilterResult> EvaluateAsync(IntegrationMessage message, CancellationToken cancellationToken) =>
        Task.FromResult(RouteFilterResult.Drop());
}
