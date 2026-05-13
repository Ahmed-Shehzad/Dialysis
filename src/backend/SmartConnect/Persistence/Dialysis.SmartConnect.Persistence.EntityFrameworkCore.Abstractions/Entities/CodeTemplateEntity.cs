namespace Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Entities;

public sealed class CodeTemplateEntity
{
    public Guid Id { get; set; }

    public Guid LibraryId { get; set; }

    public required string Name { get; set; }

    public required string Code { get; set; }

    public int Type { get; set; }

    /// <summary>JSON array of <c>CodeTemplateContext</c> enum values.</summary>
    public required string ContextsJson { get; set; } = "[]";

    public string? JsDoc { get; set; }

    public int Revision { get; set; } = 1;

    public DateTimeOffset LastModifiedUtc { get; set; }

    public int Position { get; set; }
}
