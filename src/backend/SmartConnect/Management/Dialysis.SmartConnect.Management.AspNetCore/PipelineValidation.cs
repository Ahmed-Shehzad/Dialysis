namespace Dialysis.SmartConnect.Management.AspNetCore;

/// <summary>Validates pipeline plugin kinds against a <see cref="IFlowPluginRegistry"/>.</summary>
public static class PipelineValidation
{
    /// <summary>Allowed values for <see cref="IntegrationFlow.DataTypes"/>; case-insensitive.</summary>
    private static readonly HashSet<string> _allowedDataTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "HL7v2",
        "FHIR",
        "NCPDP",
        "JSON",
        "XML",
        "Binary",
        "Other",
    };

    /// <summary>Cap per channel-level attachment (bytes after base64 decode). Bigger payloads
    /// should go through the per-message attachment store via the pipeline's attachment handler.</summary>
    private const int MaxChannelAttachmentBytes = 1 * 1024 * 1024;

    public static void ValidateOrThrow(IntegrationFlowPipelineDefinition pipeline, IFlowPluginRegistry registry)
    {
        foreach (var slot in pipeline.RouteFilters)
        {
            if (registry.TryResolveRouteFilter(slot.Kind) is null)
            {
                throw new InvalidOperationException($"Route filter kind '{slot.Kind}' is not registered.");
            }
        }

        foreach (var route in pipeline.OutboundRoutes)
        {
            if (registry.TryResolveOutboundAdapter(route.OutboundAdapterKind) is null)
            {
                throw new InvalidOperationException(
                    $"Outbound adapter kind '{route.OutboundAdapterKind}' is not registered.");
            }

            foreach (var stage in route.TransformStages)
            {
                if (registry.TryResolveTransformStage(stage.Kind) is null)
                {
                    throw new InvalidOperationException($"Transform stage kind '{stage.Kind}' is not registered.");
                }
            }

            foreach (var stage in route.ResponseTransformStages)
            {
                if (registry.TryResolveTransformStage(stage.Kind) is null)
                {
                    throw new InvalidOperationException($"Response transform stage kind '{stage.Kind}' is not registered.");
                }
            }
        }
    }

    /// <summary>
    /// Validates channel-level metadata (data types enum, dependency self-reference, attachment
    /// size cap). Dependency existence is not checked here because the repo lookup needs DI; the
    /// management endpoint does that check at create/update time after this method.
    /// </summary>
    public static void ValidateChannelMetadataOrThrow(IntegrationFlow flow)
    {
        ArgumentNullException.ThrowIfNull(flow);

        foreach (var dt in flow.DataTypes)
        {
            if (!_allowedDataTypes.Contains(dt))
            {
                throw new InvalidOperationException(
                    $"DataTypes entry '{dt}' is not one of the allowed values: {string.Join(", ", _allowedDataTypes)}.");
            }
        }

        if (flow.Dependencies.Exists(d => d == flow.Id))
        {
            throw new InvalidOperationException("A flow cannot list itself as a dependency.");
        }

        foreach (var att in flow.Attachments)
        {
            if (string.IsNullOrWhiteSpace(att.Name))
            {
                throw new InvalidOperationException("Channel attachment Name is required.");
            }
            if (string.IsNullOrWhiteSpace(att.MimeType))
            {
                throw new InvalidOperationException($"Channel attachment '{att.Name}' is missing MimeType.");
            }

            // Out-of-row attachments waive the inline cap; their bytes live in the blob backend.
            if (att.StorageRef is not null)
            {
                if (att.StorageRef.Id == Guid.Empty)
                {
                    throw new InvalidOperationException(
                        $"Channel attachment '{att.Name}' has a storageRef without a valid Id.");
                }
                if (string.IsNullOrWhiteSpace(att.StorageRef.Kind))
                {
                    throw new InvalidOperationException(
                        $"Channel attachment '{att.Name}' has a storageRef without a Kind discriminator.");
                }
                continue;
            }

            // base64 expands ~4/3; reject before allocating the decoded buffer.
            if (att.Base64Bytes.Length > MaxChannelAttachmentBytes * 4 / 3 + 16)
            {
                throw new InvalidOperationException(
                    $"Channel attachment '{att.Name}' exceeds the {MaxChannelAttachmentBytes / 1024} KiB cap; use the per-message attachment store for larger blobs.");
            }
        }
    }
}
