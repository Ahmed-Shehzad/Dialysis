using Dialysis.BuildingBlocks.Verifier;

namespace Dialysis.BuildingBlocks.Intercessor;

/// <summary>
/// Thrown when <see cref="IValidator{T}"/> instances registered for the request report failures.
/// </summary>
public sealed class IntercessorValidationException(ValidationErrors errors)
    : Exception("Request validation failed.")
{
    public ValidationErrors Errors { get; } = errors;
}
