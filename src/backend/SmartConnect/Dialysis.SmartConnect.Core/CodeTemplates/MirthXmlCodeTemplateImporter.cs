using System.Xml.Linq;

namespace Dialysis.SmartConnect.CodeTemplates;

/// <summary>
/// Parses Mirth Connect 4.5 Code Template Library XML exports (XStream-format) into <see cref="CodeTemplateLibrary"/>
/// POCOs ready for <see cref="ICodeTemplateLibraryRepository.UpsertAsync"/>. Tolerant of missing optional fields and
/// unknown elements. Maps Mirth's context bitmask enum integers to SmartConnect's <see cref="CodeTemplateContext"/>.
/// </summary>
public sealed class MirthXmlCodeTemplateImporter
{
    /// <summary>Maps Mirth's ContextType enum ordinal (UG p307 order) to SmartConnect contexts.</summary>
    private static readonly Dictionary<int, CodeTemplateContext> MirthContextMap = new()
    {
        // The Mirth enum ordinals roughly correspond to our enum but in a different order.
        // We accept both Mirth integers and our integers (importer is permissive).
        [0] = CodeTemplateContext.GlobalDeploy,
        [1] = CodeTemplateContext.GlobalUndeploy,
        [2] = CodeTemplateContext.GlobalPreprocessor,
        [3] = CodeTemplateContext.GlobalPostprocessor,
        [4] = CodeTemplateContext.ChannelDeploy,
        [5] = CodeTemplateContext.ChannelUndeploy,
        [6] = CodeTemplateContext.ChannelPreprocessor,
        [7] = CodeTemplateContext.ChannelPostprocessor,
        [8] = CodeTemplateContext.AttachmentHandler,
        [9] = CodeTemplateContext.BatchAdapter,
        [10] = CodeTemplateContext.SourceFilter,
        [11] = CodeTemplateContext.SourceTransformer,
        [12] = CodeTemplateContext.SourceConnector,
        [13] = CodeTemplateContext.DestinationFilter,
        [14] = CodeTemplateContext.DestinationTransformer,
        [15] = CodeTemplateContext.DestinationResponseTransformer,
        [16] = CodeTemplateContext.DestinationConnector,
    };

    /// <summary>Maps Mirth's named ContextType values (UG p307) to SmartConnect contexts.</summary>
    private static readonly Dictionary<string, CodeTemplateContext> MirthContextNameMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["GLOBAL_DEPLOY"] = CodeTemplateContext.GlobalDeploy,
        ["GLOBAL_UNDEPLOY"] = CodeTemplateContext.GlobalUndeploy,
        ["GLOBAL_PREPROCESSOR"] = CodeTemplateContext.GlobalPreprocessor,
        ["GLOBAL_POSTPROCESSOR"] = CodeTemplateContext.GlobalPostprocessor,
        ["CHANNEL_DEPLOY"] = CodeTemplateContext.ChannelDeploy,
        ["CHANNEL_UNDEPLOY"] = CodeTemplateContext.ChannelUndeploy,
        ["CHANNEL_PREPROCESSOR"] = CodeTemplateContext.ChannelPreprocessor,
        ["CHANNEL_POSTPROCESSOR"] = CodeTemplateContext.ChannelPostprocessor,
        ["CHANNEL_ATTACHMENT"] = CodeTemplateContext.AttachmentHandler,
        ["CHANNEL_BATCH"] = CodeTemplateContext.BatchAdapter,
        ["SOURCE_FILTER_TRANSFORMER"] = CodeTemplateContext.SourceFilter,
        ["SOURCE_TRANSFORMER"] = CodeTemplateContext.SourceTransformer,
        ["SOURCE_RECEIVER"] = CodeTemplateContext.SourceConnector,
        ["DESTINATION_FILTER_TRANSFORMER"] = CodeTemplateContext.DestinationFilter,
        ["DESTINATION_TRANSFORMER"] = CodeTemplateContext.DestinationTransformer,
        ["DESTINATION_RESPONSE_TRANSFORMER"] = CodeTemplateContext.DestinationResponseTransformer,
        ["DESTINATION_DISPATCHER"] = CodeTemplateContext.DestinationConnector,
    };

    /// <summary>
    /// Parses XML and returns the libraries. Throws <see cref="ArgumentException"/> on malformed XML.
    /// </summary>
    public IReadOnlyList<CodeTemplateLibrary> Import(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
        {
            throw new ArgumentException("XML payload was empty.", nameof(xml));
        }

        XDocument doc;
        try
        {
            doc = XDocument.Parse(xml);
        }
        catch (System.Xml.XmlException ex)
        {
            throw new ArgumentException("XML payload was malformed.", nameof(xml), ex);
        }

        if (doc.Root is null)
        {
            return [];
        }

        // Accept either a root <list> containing <codeTemplateLibrary> entries,
        // or a single <codeTemplateLibrary> root.
        var libraryNodes = doc.Root.Name.LocalName.Equals("codeTemplateLibrary", StringComparison.Ordinal)
            ? new[] { doc.Root }
            : doc.Root.Descendants().Where(e => e.Name.LocalName == "codeTemplateLibrary").ToArray();

        var result = new List<CodeTemplateLibrary>(libraryNodes.Length);
        foreach (var libNode in libraryNodes)
        {
            result.Add(ImportLibrary(libNode));
        }
        return result;
    }

    private CodeTemplateLibrary ImportLibrary(XElement libNode)
    {
        var id = ParseGuid(libNode.Element("id")?.Value) ?? Guid.CreateVersion7();
        var name = libNode.Element("name")?.Value ?? "Imported library";
        var description = libNode.Element("description")?.Value;
        var revision = ParseInt(libNode.Element("revision")?.Value) ?? 1;
        var lastModified = ParseTimestamp(libNode.Element("lastModified"));

        var enabled = libNode.Element("enabledChannelIds");
        var linkedFlowIds = enabled?.Elements("string")
            .Select(e => ParseGuid(e.Value))
            .Where(g => g.HasValue)
            .Select(g => g!.Value)
            .ToList() ?? [];

        var templates = libNode.Element("codeTemplates")?
            .Elements("codeTemplate")
            .Select((t, i) => ImportTemplate(t, id, i))
            .ToList() ?? [];

        return new CodeTemplateLibrary
        {
            Id = id,
            Name = name,
            Description = description,
            LinkedFlowIds = linkedFlowIds,
            AutoLinkNewFlows = false,
            Revision = revision,
            LastModifiedUtc = lastModified,
            Templates = templates,
        };
    }

    private CodeTemplate ImportTemplate(XElement node, Guid libraryId, int position)
    {
        var id = ParseGuid(node.Element("id")?.Value) ?? Guid.CreateVersion7();
        var name = node.Element("name")?.Value ?? "Imported template";
        var revision = ParseInt(node.Element("revision")?.Value) ?? 1;
        var lastModified = ParseTimestamp(node.Element("lastModified"));

        var propertiesNode = node.Element("properties");
        var typeName = propertiesNode?.Attribute("class")?.Value ?? string.Empty;
        var type = typeName.Contains("DragAndDrop", StringComparison.Ordinal)
            ? CodeTemplateType.DragAndDropCodeBlock
            : typeName.Contains("Compiled", StringComparison.Ordinal)
                ? CodeTemplateType.CompiledCodeBlock
                : CodeTemplateType.Function;

        var code = propertiesNode?.Element("code")?.Value
            ?? node.Element("code")?.Value
            ?? string.Empty;

        var contextsNode = propertiesNode?.Element("contextSet")
            ?? node.Element("contextSet")
            ?? node.Element("contexts");
        var contexts = new List<CodeTemplateContext>();
        if (contextsNode is not null)
        {
            // Accept either <contextType>NAME</contextType> entries or raw <int>N</int> entries.
            foreach (var child in contextsNode.Elements())
            {
                var value = child.Value?.Trim();
                if (string.IsNullOrWhiteSpace(value)) continue;
                if (MirthContextNameMap.TryGetValue(value, out var mappedByName))
                {
                    contexts.Add(mappedByName);
                }
                else if (int.TryParse(value, out var ordinal) && MirthContextMap.TryGetValue(ordinal, out var mappedByOrdinal))
                {
                    contexts.Add(mappedByOrdinal);
                }
            }
        }

        var jsDoc = ExtractLeadingJsDoc(code);

        return new CodeTemplate
        {
            Id = id,
            LibraryId = libraryId,
            Name = name,
            Code = code,
            Type = type,
            Contexts = contexts,
            JsDoc = jsDoc,
            Revision = revision,
            LastModifiedUtc = lastModified,
            Position = position,
        };
    }

    private static Guid? ParseGuid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return Guid.TryParse(value, out var g) ? g : null;
    }

    private static int? ParseInt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return int.TryParse(value, out var i) ? i : null;
    }

    private static DateTimeOffset ParseTimestamp(XElement? node)
    {
        if (node is null) return DateTimeOffset.UtcNow;
        // Mirth's typical shape: <lastModified><time>1234567890</time><timezone>UTC</timezone></lastModified>
        var timeStr = node.Element("time")?.Value ?? node.Value?.Trim();
        if (long.TryParse(timeStr, out var millis) && millis > 0)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(millis);
        }
        if (DateTimeOffset.TryParse(timeStr, out var dto))
        {
            return dto;
        }
        return DateTimeOffset.UtcNow;
    }

    private static string? ExtractLeadingJsDoc(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        var trimmed = code.TrimStart();
        if (!trimmed.StartsWith("/**", StringComparison.Ordinal)) return null;
        var end = trimmed.IndexOf("*/", StringComparison.Ordinal);
        if (end <= 0) return null;
        return trimmed[..(end + 2)];
    }
}
