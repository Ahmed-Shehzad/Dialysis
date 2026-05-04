namespace Dialysis.BuildingBlocks.Verifier;

/// <summary>
/// Async validation for a single property (for example I/O or remote checks). Register via Verifier.Core extension methods such as <c>MustAsync</c> or <c>AddAsyncValidator</c>.
/// </summary>
public interface IAsyncPropertyValidator<T, TProperty>
{
    IAsyncEnumerable<ValidationFailure> EvaluateAsync(
        ValidationContext<T> context,
        TProperty value,
        string propertyName,
        CancellationToken cancellationToken);
}
