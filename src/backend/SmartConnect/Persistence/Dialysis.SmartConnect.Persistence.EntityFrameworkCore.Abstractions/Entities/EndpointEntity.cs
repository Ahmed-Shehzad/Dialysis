namespace Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Entities;

/// <summary>
/// Named outbound endpoint — a reusable bundle of <c>(adapter kind, parameters JSON)</c> referenced
/// by flow outbound routes via the <c>{"endpointRef":"name"}</c> shape. Lets operators swap a
/// partner URL / lab MLLP host / etc. without editing every flow that targets it.
/// </summary>
public sealed class EndpointEntity
{
    public Guid Id { get; set; }

    /// <summary>Operator-facing name; unique. Routes reference this via <c>endpointRef</c>.</summary>
    public required string Name { get; set; }

    /// <summary>Adapter kind, e.g. <c>http</c>, <c>tcp</c>, <c>transponder-bus</c>.</summary>
    public required string Kind { get; set; }

    /// <summary>Adapter-specific parameters JSON (URL, auth, headers, routing key, …).</summary>
    public required string ParametersJson { get; set; }

    public string? Description { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }
}
