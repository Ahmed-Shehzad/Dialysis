namespace Dialysis.BuildingBlocks.Verifier;

/// <summary>
/// Validates instances of <typeparamref name="T"/>.
/// </summary>
public interface IValidator<T>
{
    ValidationResult<T> Validate(T instance);

    /// <summary>
    /// Runs validation including async rules (for example remote checks). The default implementation delegates to <see cref="Validate"/>.
    /// </summary>
    Task<ValidationResult<T>> ValidateAsync(T instance, CancellationToken cancellationToken = default);
}
