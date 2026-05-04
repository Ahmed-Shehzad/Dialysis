using System.Text.Json;
using Dialysis.SmartConnect;

namespace Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Json;

internal static class PipelineJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public static string Serialize(IntegrationFlowPipelineDefinition pipeline) =>
        JsonSerializer.Serialize(pipeline, Options);

    public static IntegrationFlowPipelineDefinition Deserialize(string json) =>
        JsonSerializer.Deserialize<IntegrationFlowPipelineDefinition>(json, Options)
        ?? new IntegrationFlowPipelineDefinition();
}
