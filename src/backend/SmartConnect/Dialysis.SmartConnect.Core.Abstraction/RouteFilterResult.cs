namespace Dialysis.SmartConnect;

/// <summary>
/// Outcome of a <see cref="IRouteFilter"/> for the current <see cref="IntegrationMessage"/>.
/// </summary>
public readonly record struct RouteFilterResult(RouteFilterDisposition Disposition)
{
    public static RouteFilterResult Allow() => new(RouteFilterDisposition.Allow);

    public static RouteFilterResult Drop() => new(RouteFilterDisposition.Drop);
}

public enum RouteFilterDisposition
{
    Allow = 0,
    Drop = 1,
}
