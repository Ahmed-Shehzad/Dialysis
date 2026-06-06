using Dialysis.BuildingBlocks.Documents.Pdf;
using Dialysis.Simulation.Engine.Scenarios;

namespace Dialysis.Simulation.Engine.Documents;

/// <summary>
/// Builds a deterministic <see cref="DocumentModel"/> for a scenario document and renders it to PDF
/// bytes via the shared renderer. Every document carries the full simulation lineage in its metadata,
/// so a generated PDF can always be traced back to the session that produced it.
/// </summary>
public static class SimulationDocumentFactory
{
    /// <summary>Renders a PDF for the given scenario context, facts, and document kind/title.</summary>
    public static Task<byte[]> RenderAsync(
        IPdfDocumentRenderer renderer,
        SimulationContext context,
        string kind,
        string title,
        IReadOnlyList<KeyValuePair<string, string>> facts,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentNullException.ThrowIfNull(context);

        var session = context.Session;
        var model = new DocumentModel(
            Title: title,
            Subtitle: $"{session.ScenarioId} · session {session.Id:N}",
            Sections:
            [
                new DocumentSection("Document", [new KeyValueBlock(facts)]),
            ],
            Metadata: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["DocumentKind"] = kind,
                ["TenantId"] = session.TenantId,
                ["OrganizationId"] = session.OrganizationId,
                ["SimulationSessionId"] = session.Id.ToString(),
                ["ScenarioId"] = session.ScenarioId,
                ["WorkflowId"] = session.Id.ToString(),
                ["PatientJourneyId"] = session.PatientJourney.Id.ToString(),
                ["CorrelationId"] = session.CorrelationId,
                ["TraceId"] = session.TraceId,
            });

        return renderer.RenderAsync(model, cancellationToken);
    }
}
