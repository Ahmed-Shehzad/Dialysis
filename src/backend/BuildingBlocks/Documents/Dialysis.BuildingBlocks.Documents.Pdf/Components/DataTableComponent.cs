using Dialysis.BuildingBlocks.Documents.Pdf.Macros;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Dialysis.BuildingBlocks.Documents.Pdf.Components;

/// <summary>
/// Generic tabular component — headers + ragged-OK rows of strings. The shift report uses
/// this for the per-session roll-up; the discharge letter uses it for the medications and
/// alarms tables. Cell padding is applied via the <c>TableCell</c> macro so every report's
/// tables align identically.
/// </summary>
public sealed class DataTableComponent : IComponent
{
    public required IReadOnlyList<string> Headers { get; init; }
    public required IReadOnlyList<IReadOnlyList<string>> Rows { get; init; }

    public void Compose(IContainer container)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                foreach (var _ in Headers)
                    columns.RelativeColumn();
            });
            table.Header(header =>
            {
                foreach (var heading in Headers)
                {
                    header.Cell().Element(c => c.TableCell())
                        .Text(heading).SemiBold();
                }
            });
            foreach (var row in Rows)
            {
                foreach (var cell in row)
                {
                    table.Cell().Element(c => c.TableCell()).Text(cell);
                }
            }
        });
    }
}
