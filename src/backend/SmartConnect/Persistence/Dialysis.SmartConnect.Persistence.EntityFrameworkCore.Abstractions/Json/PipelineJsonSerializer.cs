using System.Text.Json;

namespace Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Json;

internal static class PipelineJsonSerializer
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public static string Serialize(IntegrationFlowPipelineDefinition pipeline) =>
        JsonSerializer.Serialize(pipeline, _options);

    public static IntegrationFlowPipelineDefinition Deserialize(string json) =>
        JsonSerializer.Deserialize<IntegrationFlowPipelineDefinition>(json, _options)
        ?? new IntegrationFlowPipelineDefinition();
}
