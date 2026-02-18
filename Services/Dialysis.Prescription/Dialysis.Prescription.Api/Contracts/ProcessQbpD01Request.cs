namespace Dialysis.Prescription.Api.Contracts;

/// <summary>
/// Request body for QBP^D01 prescription query processing.
/// </summary>
public sealed record ProcessQbpD01Request(string RawHl7Message);
