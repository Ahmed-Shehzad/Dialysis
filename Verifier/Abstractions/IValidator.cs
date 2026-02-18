namespace Verifier.Abstractions;

public interface IValidator<in T>
{
    ValidationResult Validate(T instance);
    Task<ValidationResult> ValidateAsync(T instance, CancellationToken ct = default);
}
