namespace Dialysis.BuildingBlocks.Verifier;

/// <summary>
/// Validates instances of <typeparamref name="T"/>.
/// </summary>
public interface IValidator<T>
{
    /// <summary>
    /// Runs validation rules (including async ones — e.g. remote uniqueness checks).
    /// </summary>
    Task<ValidationResult<T>> ValidateAsync(T instance, CancellationToken cancellationToken = default);
}
