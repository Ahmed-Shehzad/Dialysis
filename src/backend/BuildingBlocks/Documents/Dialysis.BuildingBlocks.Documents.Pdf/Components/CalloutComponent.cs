using Dialysis.BuildingBlocks.Documents.Pdf.Macros;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Dialysis.BuildingBlocks.Documents.Pdf.Components;

/// <summary>
/// Single-block callout — short emphatic message inside the document body. Two variants
/// via <see cref="CalloutKind"/>: <c>Info</c> uses the accent tint; <c>Alert</c> uses the
/// danger palette. The discharge letter uses Alert for known drug allergies and Info for
/// scheduled follow-ups.
/// </summary>
public sealed class CalloutComponent : IComponent
{
    public required string Heading { get; init; }
    public required string Body { get; init; }
    public CalloutKind Kind { get; init; } = CalloutKind.Info;

    public void Compose(IContainer container)
    {
        var styled = Kind == CalloutKind.Alert
            ? container.AlertBox()
            : container.CalloutBox();
        styled.Column(col =>
        {
            col.Item().Text(Heading).SemiBold();
            col.Item().PaddingTop(2).Text(Body);
        });
    }
}

public enum CalloutKind
{
    Info = 0,
    Alert = 1,
}
