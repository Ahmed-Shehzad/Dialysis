using Dialysis.BuildingBlocks.Documents.Pdf.Macros;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Dialysis.BuildingBlocks.Documents.Pdf.Components;

/// <summary>
/// Reusable patient-identification block. Used at the top of every patient-scoped report
/// (discharge letter, billing summary, lab summary) so the patient identity is always
/// rendered the same way. PHI-bearing fields (name, MRN, date of birth) are explicit
/// inputs so the caller controls what's shown — useful for the GDPR-Art. 5(1)(c) minimised
/// variants of the same document.
/// </summary>
public sealed class PatientHeaderComponent : IComponent
{
    public required string DisplayName { get; init; }
    public required string MedicalRecordNumber { get; init; }
    public string? Title { get; init; }
    public string? Subtitle { get; init; }

    public void Compose(IContainer container)
    {
        container.ClinicalHeader().Column(col =>
        {
            if (!string.IsNullOrWhiteSpace(Title))
                col.Item().Text(Title).SemiBold().FontSize(16);
            if (!string.IsNullOrWhiteSpace(Subtitle))
                col.Item().Text(Subtitle).FontSize(11).FontColor(ClinicalDocumentMacros.MutedTextColor);
            col.Item().PaddingTop(6).Row(row =>
            {
                row.RelativeItem().Element(c => c.KeyValueRow("Patient", DisplayName));
                row.RelativeItem().Element(c => c.KeyValueRow("MRN", MedicalRecordNumber));
            });
        });
    }
}
