namespace Dialysis.BuildingBlocks.Verifier;

/// <summary>
/// Base class for defining validation rules in a fluent style (no external validation packages required).
/// </summary>
public abstract class AbstractValidator<T> : IValidator<T>
{
    private readonly List<IExecutableValidationRule<T>> _rules = [];

    /// <summary>
    /// Defines a strongly-typed rule for the member returned by <paramref name="accessor"/>.
    /// For the whole instance use <c>RuleFor(static t => t, ValidationPath.Root)</c>.
    /// </summary>
    /// <param name="propertyPath">Logical path for failures; use <c>nameof</c> and <see cref="ValidationPath.Child"/>.</param>
    protected IRuleBuilder<T, TProperty> RuleFor<TProperty>(Func<T, TProperty> accessor, string propertyPath)
    {
        ArgumentNullException.ThrowIfNull(accessor);
        ArgumentNullException.ThrowIfNull(propertyPath);

        var rule = new RuleBuilder<T, TProperty>(accessor, propertyPath);
        _rules.Add(rule);
        return rule;
    }

    public virtual ValidationResult<T> Validate(T instance) =>
        ValidateAsync(instance, CancellationToken.None).GetAwaiter().GetResult();

    public async virtual Task<ValidationResult<T>> ValidateAsync(T instance, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(instance);

        var context = new ValidationContext<T>(instance);
        var failures = new List<ValidationFailure>();
        foreach (var rule in _rules)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var batch = await rule.ExecuteAsync(context, cancellationToken).ConfigureAwait(false);
            failures.AddRange(batch);
        }

        return failures.Count == 0
            ? ValidationResult<T>.Ok(instance)
            : ValidationResult<T>.Fail(ValidationErrors.From(failures));
    }
}

/// <summary>
/// Builds dotted member paths for validation messages without reflection. Combine with <c>nameof</c> at call sites.
/// </summary>
public static class ValidationPath
{
    /// <summary>Path representing the root model (empty segment).</summary>
    public static string Root => string.Empty;

    /// <summary>
    /// Appends <paramref name="memberName"/> under <paramref name="parentPath"/> (omit parent for a top-level member).
    /// </summary>
    public static string Child(string? parentPath, string memberName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(memberName);
        return string.IsNullOrEmpty(parentPath) ? memberName : $"{parentPath}.{memberName}";
    }
}
