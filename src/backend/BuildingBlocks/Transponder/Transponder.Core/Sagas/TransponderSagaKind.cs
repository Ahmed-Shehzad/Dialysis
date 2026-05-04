namespace Dialysis.BuildingBlocks.Transponder;

/// <summary>Stable saga family key used with <see cref="ITransponderSagaStore"/>.</summary>
public static class TransponderSagaKind
{
    public static string For<TState>() =>
        typeof(TState).FullName ?? typeof(TState).Name;
}
