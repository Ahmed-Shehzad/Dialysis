namespace Dialysis.SmartConnect;

/// <summary>
/// Outcome of a <see cref="IRouteFilter"/> for the current <see cref="IntegrationMessage"/>.
/// </summary>
public readonly record struct RouteFilterResult
{
    /// <summary>
    /// Outcome of a <see cref="IRouteFilter"/> for the current <see cref="IntegrationMessage"/>.
    /// </summary>
    public RouteFilterResult(RouteFilterDisposition Disposition) => this.Disposition = Disposition;
    public static RouteFilterResult Allow() => new(RouteFilterDisposition.Allow);

    public static RouteFilterResult Drop() => new(RouteFilterDisposition.Drop);
    public RouteFilterDisposition Disposition { get; init; }
    public void Deconstruct(out RouteFilterDisposition disposition) => disposition = Disposition;
}

public enum RouteFilterDisposition
{
    Allow = 0,
    Drop = 1,
}
