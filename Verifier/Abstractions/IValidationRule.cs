namespace Verifier.Abstractions;

internal interface IValidationRule<in T>
{
    IEnumerable<ValidationFailure> Validate(T instance);
}

internal interface IAsyncValidationRule<in T>
{
    Task<IEnumerable<ValidationFailure>> ValidateAsync(T instance, CancellationToken ct = default);
}
