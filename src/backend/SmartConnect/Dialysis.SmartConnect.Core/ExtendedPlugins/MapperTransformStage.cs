using Dialysis.SmartConnect.Transforms;

namespace Dialysis.SmartConnect.ExtendedPlugins;

/// <summary>
/// Mapper step alias: same behavior as <see cref="JsonTransformStage"/> (JSON path mappings / expression).
/// </summary>
public sealed class MapperTransformStage : ITransformStage
{
    private readonly JsonTransformStage _inner;
    /// <summary>
    /// Mapper step alias: same behavior as <see cref="JsonTransformStage"/> (JSON path mappings / expression).
    /// </summary>
    public MapperTransformStage(JsonTransformStage inner) => _inner = inner;
    public const string KindValue = "mapper-transform";

    public string Kind => KindValue;

    public Task<IntegrationMessage> TransformAsync(IntegrationMessage message, CancellationToken cancellationToken) =>
        _inner.TransformAsync(message, cancellationToken);
}
