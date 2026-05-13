namespace Dialysis.SmartConnect.CodeTemplates;

/// <summary>
/// One reusable code snippet within a <see cref="CodeTemplateLibrary"/>.
/// </summary>
public sealed class CodeTemplate
{
    public required Guid Id { get; init; }

    public required Guid LibraryId { get; init; }

    public required string Name { get; init; }

    public required string Code { get; init; }

    public CodeTemplateType Type { get; init; } = CodeTemplateType.Function;

    public IReadOnlyList<CodeTemplateContext> Contexts { get; init; } = [];

    public string? JsDoc { get; init; }

    public int Revision { get; init; } = 1;

    public DateTimeOffset LastModifiedUtc { get; init; }

    /// <summary>Ordinal position within the parent library; used to keep template ordering stable.</summary>
    public int Position { get; init; }
}
