namespace Dialysis.BuildingBlocks.Transponder;

internal static class RoutingKey
{
    public static string For<TMessage>()
        where TMessage : class
    {
        var t = typeof(TMessage);
        return t.FullName ?? t.Name;
    }
}
