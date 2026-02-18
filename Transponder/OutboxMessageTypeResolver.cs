namespace Transponder;

internal static class OutboxMessageTypeResolver
{
    public static Type? Resolve(string? messageType)
    {
        if (string.IsNullOrWhiteSpace(messageType)) return null;

        var resolved = Type.GetType(messageType, throwOnError: false);
        if (resolved is not null) return resolved;

        foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            resolved = assembly.GetType(messageType, throwOnError: false);
            if (resolved is not null) return resolved;
        }

        return null;
    }
}
