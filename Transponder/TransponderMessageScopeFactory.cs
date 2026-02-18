using Transponder.Abstractions;

namespace Transponder;

internal static class TransponderMessageScopeFactory
{
    public static IDisposable? BeginSend(
        IReadOnlyList<ITransponderMessageScopeProvider> providers,
        TransponderMessageContext context)
        => Begin(providers, provider => provider.BeginSend(context));

    public static IDisposable? BeginPublish(
        IReadOnlyList<ITransponderMessageScopeProvider> providers,
        TransponderMessageContext context)
        => Begin(providers, provider => provider.BeginPublish(context));

    public static IDisposable? BeginConsume(
        IReadOnlyList<ITransponderMessageScopeProvider> providers,
        TransponderMessageContext context)
        => Begin(providers, provider => provider.BeginConsume(context));

    private static IDisposable? Begin(
        IReadOnlyList<ITransponderMessageScopeProvider> providers,
        Func<ITransponderMessageScopeProvider, IDisposable?> selector)
    {
        if (providers.Count == 0) return null;

        List<IDisposable>? scopes = null;
        foreach (ITransponderMessageScopeProvider provider in providers)
        {
            IDisposable? scope = selector(provider);
            if (scope is null) continue;
            scopes ??= [];
            scopes.Add(scope);
        }

        return scopes is null ? null : new CompositeScope(scopes);
    }

    private sealed class CompositeScope : IDisposable
    {
        private readonly List<IDisposable> _scopes;

        public CompositeScope(List<IDisposable> scopes)
        {
            _scopes = scopes;
        }

        public void Dispose()
        {
            for (int i = _scopes.Count - 1; i >= 0; i--) _scopes[i].Dispose();
        }
    }
}
