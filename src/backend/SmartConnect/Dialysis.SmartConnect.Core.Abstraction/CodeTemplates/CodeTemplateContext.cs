namespace Dialysis.SmartConnect.CodeTemplates;

/// <summary>
/// Execution-context tag controlling where a <see cref="CodeTemplate"/> is injected. Maps 1:1 to Mirth's
/// context categories (UG p307). A template lists one or more contexts; the engine injects the template
/// when the currently-executing script's context matches any of those entries.
/// </summary>
public enum CodeTemplateContext
{
    GlobalDeploy = 0,
    GlobalUndeploy = 1,
    GlobalPreprocessor = 2,
    GlobalPostprocessor = 3,
    ChannelDeploy = 4,
    ChannelUndeploy = 5,
    ChannelPreprocessor = 6,
    ChannelPostprocessor = 7,
    AttachmentHandler = 8,
    BatchAdapter = 9,
    SourceFilter = 10,
    SourceTransformer = 11,
    SourceConnector = 12,
    DestinationFilter = 13,
    DestinationTransformer = 14,
    DestinationResponseTransformer = 15,
    DestinationConnector = 16,
}
