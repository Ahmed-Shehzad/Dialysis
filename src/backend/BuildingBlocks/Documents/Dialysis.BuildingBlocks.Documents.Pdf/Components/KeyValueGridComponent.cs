using Dialysis.BuildingBlocks.Documents.Pdf.Macros;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Dialysis.BuildingBlocks.Documents.Pdf.Components;

/// <summary>
/// Two-column key/value grid — the workhorse layout for clinical letters and discharge
/// summaries. Lays each pair out as one row; long values wrap inside the right column.
/// Use directly via <c>.Component(new KeyValueGridComponent { Pairs = ... })</c> or via the
/// model-driven <see cref="QuestPdfDocumentRenderer"/> for the <see cref="KeyValueBlock"/>
/// document block.
/// </summary>
public sealed class KeyValueGridComponent : IComponent
{
    public required IReadOnlyList<KeyValuePair<string, string>> Pairs { get; init; }

    public void Compose(IContainer container)
    {
        container.Column(col =>
        {
            foreach (var pair in Pairs)
                col.Item().Element(c => c.KeyValueRow(pair.Key, pair.Value));
        });
    }
}
