using Dialysis.BuildingBlocks.Verifier;

namespace Dialysis.BuildingBlocks.Intercessor;

/// <summary>
/// Thrown when <see cref="IValidator{T}"/> instances registered for the request report failures.
/// </summary>
public sealed class IntercessorValidationException : Exception
{
    /// <summary>
    /// Thrown when <see cref="IValidator{T}"/> instances registered for the request report failures.
    /// </summary>
    public IntercessorValidationException(ValidationErrors errors) : base("Request validation failed.") => Errors = errors;
    public ValidationErrors Errors { get; }
}
