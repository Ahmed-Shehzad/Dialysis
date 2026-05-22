using System.Collections;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Dialysis.BuildingBlocks.Verifier;

internal interface IExecutableValidationRule<T>
{
    ValueTask<IReadOnlyList<ValidationFailure>> ExecuteAsync(ValidationContext<T> context, CancellationToken cancellationToken);
}

internal interface IPropertyValidator<T, TProperty>
{
    IEnumerable<ValidationFailure> Evaluate(ValidationContext<T> context, TProperty value, string propertyName);
}

internal sealed class NotNullValidator<T, TProperty> : IPropertyValidator<T, TProperty>
{
    public IEnumerable<ValidationFailure> Evaluate(ValidationContext<T> context, TProperty value, string propertyName)
    {
        if (value is not null)
            yield break;

        yield return new ValidationFailure(propertyName, $"'{propertyName}' must not be null.", null);
    }
}

internal sealed class NotEmptyStringValidator<T> : IPropertyValidator<T, string?>
{
    public IEnumerable<ValidationFailure> Evaluate(ValidationContext<T> context, string? value, string propertyName)
    {
        if (!string.IsNullOrWhiteSpace(value))
            yield break;

        yield return new ValidationFailure(propertyName, $"'{propertyName}' must not be empty.", value);
    }
}

internal sealed class EnumerableNotEmptyValidator<T, TCollection> : IPropertyValidator<T, TCollection?>
    where TCollection : class, IEnumerable
{
    public IEnumerable<ValidationFailure> Evaluate(ValidationContext<T> context, TCollection? value, string propertyName)
    {
        if (value is null)
        {
            yield return new ValidationFailure(propertyName, $"'{propertyName}' must not be empty.", null);
            yield break;
        }

        if (value is string s)
        {
            if (!string.IsNullOrWhiteSpace(s))
                yield break;

            yield return new ValidationFailure(propertyName, $"'{propertyName}' must not be empty.", value);
            yield break;
        }

        if (HasAny(value))
            yield break;

        yield return new ValidationFailure(propertyName, $"'{propertyName}' must not be empty.", value);
    }

    private static bool HasAny(IEnumerable enumerable)
    {
        if (enumerable is ICollection collection)
            return collection.Count > 0;

        var enumerator = enumerable.GetEnumerator();
        try
        {
            return enumerator.MoveNext();
        }
        finally
        {
            if (enumerator is IDisposable disposable)
                disposable.Dispose();
        }
    }
}

internal sealed class LengthRangeValidator<T>(int min, int max) : IPropertyValidator<T, string?>
{
    public IEnumerable<ValidationFailure> Evaluate(ValidationContext<T> context, string? value, string propertyName)
    {
        if (value is null || (value.Length >= min && value.Length <= max))
            yield break;

        yield return new ValidationFailure(
            propertyName,
            $"'{propertyName}' must be between {min} and {max} characters. You entered {value?.Length ?? 0} characters.",
            value);
    }
}

internal sealed class PredicateValidator<T, TProperty>(Func<T, TProperty, bool> predicate, string message)
    : IPropertyValidator<T, TProperty>
{
    public IEnumerable<ValidationFailure> Evaluate(ValidationContext<T> context, TProperty value, string propertyName)
    {
        if (predicate(context.InstanceToValidate!, value!))
            yield break;

        yield return new ValidationFailure(propertyName, message.Replace("{PropertyName}", propertyName, StringComparison.Ordinal), value);
    }
}

internal sealed class RegularExpressionValidator<T>(Regex regex, string message) : IPropertyValidator<T, string?>
{
    public IEnumerable<ValidationFailure> Evaluate(ValidationContext<T> context, string? value, string propertyName)
    {
        if (string.IsNullOrEmpty(value) || regex.IsMatch(value))
            yield break;

        var text = message.Replace("{PropertyName}", propertyName, StringComparison.Ordinal);
        yield return new ValidationFailure(propertyName, text, value);
    }
}

internal enum ComparableKind
{
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    BetweenInclusive
}

internal sealed class NullableComparableValidator<T, TComparable>(ComparableKind kind, TComparable low, TComparable high)
    : IPropertyValidator<T, TComparable?> where TComparable : struct, IComparable<TComparable>
{
    public IEnumerable<ValidationFailure> Evaluate(ValidationContext<T> context, TComparable? value, string propertyName)
    {
        if (value is null)
            yield break;

        var v = value.Value;
        if (Passes(v))
            yield break;

        yield return new ValidationFailure(propertyName, BuildMessage(propertyName, v), value);
    }

    private bool Passes(TComparable v)
    {
        if (kind == ComparableKind.GreaterThan)
            return v.CompareTo(low) > 0;
        if (kind == ComparableKind.GreaterThanOrEqual)
            return v.CompareTo(low) >= 0;
        if (kind == ComparableKind.LessThan)
            return v.CompareTo(low) < 0;
        if (kind == ComparableKind.LessThanOrEqual)
            return v.CompareTo(low) <= 0;
        return v.CompareTo(low) >= 0 && v.CompareTo(high) <= 0;
    }

    private string BuildMessage(string propertyName, TComparable v)
    {
        if (kind == ComparableKind.GreaterThan)
            return $"'{propertyName}' must be greater than '{low}'.";
        if (kind == ComparableKind.GreaterThanOrEqual)
            return $"'{propertyName}' must be greater than or equal to '{low}'.";
        if (kind == ComparableKind.LessThan)
            return $"'{propertyName}' must be less than '{low}'.";
        if (kind == ComparableKind.LessThanOrEqual)
            return $"'{propertyName}' must be less than or equal to '{low}'.";
        return $"'{propertyName}' must be between {low} and {high}. You entered {v}.";
    }
}

internal sealed class NonNullComparableValidator<T, TComparable>(ComparableKind kind, TComparable low, TComparable high)
    : IPropertyValidator<T, TComparable> where TComparable : struct, IComparable<TComparable>
{
    public IEnumerable<ValidationFailure> Evaluate(ValidationContext<T> context, TComparable value, string propertyName)
    {
        if (Passes(value))
            yield break;

        yield return new ValidationFailure(propertyName, BuildMessage(propertyName, value), value);
    }

    private bool Passes(TComparable v)
    {
        if (kind == ComparableKind.GreaterThan)
            return v.CompareTo(low) > 0;
        if (kind == ComparableKind.GreaterThanOrEqual)
            return v.CompareTo(low) >= 0;
        if (kind == ComparableKind.LessThan)
            return v.CompareTo(low) < 0;
        if (kind == ComparableKind.LessThanOrEqual)
            return v.CompareTo(low) <= 0;
        return v.CompareTo(low) >= 0 && v.CompareTo(high) <= 0;
    }

    private string BuildMessage(string propertyName, TComparable v)
    {
        if (kind == ComparableKind.GreaterThan)
            return $"'{propertyName}' must be greater than '{low}'.";
        if (kind == ComparableKind.GreaterThanOrEqual)
            return $"'{propertyName}' must be greater than or equal to '{low}'.";
        if (kind == ComparableKind.LessThan)
            return $"'{propertyName}' must be less than '{low}'.";
        if (kind == ComparableKind.LessThanOrEqual)
            return $"'{propertyName}' must be less than or equal to '{low}'.";
        return $"'{propertyName}' must be between {low} and {high}. You entered {v}.";
    }
}

internal sealed class AsyncPredicateValidator<T, TProperty>(
    Func<T, TProperty, CancellationToken, ValueTask<bool>> predicate,
    string message) : IAsyncPropertyValidator<T, TProperty>
{
    public async IAsyncEnumerable<ValidationFailure> EvaluateAsync(
        ValidationContext<T> context,
        TProperty value,
        string propertyName,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (await predicate(context.InstanceToValidate!, value!, cancellationToken).ConfigureAwait(false))
            yield break;

        yield return new ValidationFailure(
            propertyName,
            message.Replace("{PropertyName}", propertyName, StringComparison.Ordinal),
            value);
    }
}

public sealed class RuleBuilder<T, TProperty> : IRuleBuilder<T, TProperty>, IExecutableValidationRule<T>
{
    private delegate ValueTask<IReadOnlyList<ValidationFailure>> Evaluator(
        ValidationContext<T> context,
        TProperty value,
        string propertyPath,
        CancellationToken cancellationToken);

    private readonly List<Evaluator> _evaluators = [];

    internal RuleBuilder(Func<T, TProperty> accessor, string propertyPath)
    {
        Accessor = accessor;
        PropertyPath = propertyPath;
    }

    internal Func<T, TProperty> Accessor { get; }

    internal string PropertyPath { get; }

    public bool StopAfterFirstFailure { get; private set; }

    internal void AddValidator(IPropertyValidator<T, TProperty> validator)
    {
        _evaluators.Add((ctx, value, path, ct) =>
        {
            ct.ThrowIfCancellationRequested();
            return new ValueTask<IReadOnlyList<ValidationFailure>>(CollectSync(validator, ctx, value!, path));
        });
    }

    internal void AddAsyncValidator(IAsyncPropertyValidator<T, TProperty> validator)
    {
        _evaluators.Add(async (ctx, value, path, ct) => await CollectAsync(validator, ctx, value!, path, ct).ConfigureAwait(false));
    }

    internal void AddNestedValidator(IValidator<TProperty> childValidator)
    {
        _evaluators.Add(async (ctx, value, path, ct) =>
        {
            ct.ThrowIfCancellationRequested();
            if (value is null)
            {
                return
                [
                    new ValidationFailure(path, $"'{path}' must not be null.", null)
                ];
            }

            var childResult = await childValidator.ValidateAsync(value, ct).ConfigureAwait(false);
            if (childResult.IsSuccess)
                return Array.Empty<ValidationFailure>();

            var list = new List<ValidationFailure>();
            foreach (var f in childResult.Error)
                list.Add(PrefixFailure(f, path));

            return list;
        });
    }

    internal void ApplyWhen(Func<T, bool> predicate)
    {
        if (_evaluators.Count == 0)
            return;

        for (var i = 0; i < _evaluators.Count; i++)
        {
            var inner = _evaluators[i];
            _evaluators[i] = async (ctx, val, path, ct) =>
            {
                if (!predicate(ctx.InstanceToValidate!))
                    return Array.Empty<ValidationFailure>();

                return await inner(ctx, val, path, ct).ConfigureAwait(false);
            };
        }
    }

    public IRuleBuilder<T, TProperty> StopOnFirstFailure()
    {
        StopAfterFirstFailure = true;
        return this;
    }

    public IRuleBuilder<T, TProperty> When(Func<T, bool> predicate)
    {
        ApplyWhen(predicate);
        return this;
    }

    public IRuleBuilder<T, TProperty> Unless(Func<T, bool> predicate)
    {
        ApplyWhen(x => !predicate(x));
        return this;
    }

    public IRuleBuilder<T, TProperty> WithMessage(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        if (_evaluators.Count == 0)
            throw new InvalidOperationException("No validators have been added to this rule.");

        var last = _evaluators.Count - 1;
        var inner = _evaluators[last];
        _evaluators[last] = async (ctx, val, path, ct) =>
        {
            var batch = await inner(ctx, val, path, ct).ConfigureAwait(false);
            if (batch.Count == 0)
                return batch;

            var mapped = new ValidationFailure[batch.Count];
            for (var i = 0; i < batch.Count; i++)
            {
                var f = batch[i];
                mapped[i] = new ValidationFailure(f.PropertyName, message, f.AttemptedValue, f.ErrorCode);
            }

            return mapped;
        };

        return this;
    }

    public async ValueTask<IReadOnlyList<ValidationFailure>> ExecuteAsync(ValidationContext<T> context, CancellationToken cancellationToken)
    {
        TProperty value;
        try
        {
            value = Accessor(context.InstanceToValidate);
        }
        catch (Exception ex)
        {
            return
            [
                new ValidationFailure(PropertyPath, $"Could not read property '{PropertyPath}': {ex.Message}", null)
            ];
        }

        var failures = new List<ValidationFailure>();
        foreach (var evaluator in _evaluators)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var batch = await evaluator(context, value!, PropertyPath, cancellationToken).ConfigureAwait(false);
            var produced = batch.Count > 0;
            if (produced)
                failures.AddRange(batch);

            if (produced && StopAfterFirstFailure)
                break;
        }

        return failures;
    }

    private static IReadOnlyList<ValidationFailure> CollectSync(
        IPropertyValidator<T, TProperty> validator,
        ValidationContext<T> context,
        TProperty value,
        string propertyPath)
    {
        var list = new List<ValidationFailure>();
        foreach (var f in validator.Evaluate(context, value, propertyPath))
            list.Add(f);

        return list.Count == 0 ? Array.Empty<ValidationFailure>() : list;
    }

    private static async ValueTask<IReadOnlyList<ValidationFailure>> CollectAsync(
        IAsyncPropertyValidator<T, TProperty> validator,
        ValidationContext<T> context,
        TProperty value,
        string propertyPath,
        CancellationToken cancellationToken)
    {
        var list = new List<ValidationFailure>();
        await foreach (var f in validator.EvaluateAsync(context, value, propertyPath, cancellationToken).ConfigureAwait(false))
            list.Add(f);

        return list.Count == 0 ? Array.Empty<ValidationFailure>() : list;
    }

    private static ValidationFailure PrefixFailure(ValidationFailure f, string propertyName)
    {
        string path;
        if (string.IsNullOrEmpty(propertyName))
            path = f.PropertyName;
        else if (string.IsNullOrEmpty(f.PropertyName))
            path = propertyName;
        else
            path = $"{propertyName}.{f.PropertyName}";

        return new ValidationFailure(path, f.ErrorMessage, f.AttemptedValue, f.ErrorCode);
    }
}

/// <summary>Fluent constraints for <see cref="IRuleBuilder{T,TProperty}"/>.</summary>
public static class VerifierRuleExtensions
{
    extension<T, TProperty>(IRuleBuilder<T, TProperty> ruleBuilder)
    {
        public IRuleBuilder<T, TProperty> NotNull()
        {
            Unwrap(ruleBuilder).AddValidator(new NotNullValidator<T, TProperty>());
            return ruleBuilder;
        }

        public IRuleBuilder<T, TProperty> Must(Func<T, TProperty, bool> predicate)
        {
            ArgumentNullException.ThrowIfNull(predicate);
            Unwrap(ruleBuilder).AddValidator(new PredicateValidator<T, TProperty>(predicate, "The specified condition was not met for '{PropertyName}'."));
            return ruleBuilder;
        }

        /// <summary>Async predicate (e.g. a remote uniqueness check). Overload of <see cref="Must"/>
        /// distinguished by predicate arity — the builder returns synchronously, so it
        /// doesn't carry the "Async" suffix that would mislead readers into expecting a Task.</summary>
        public IRuleBuilder<T, TProperty> Must(
            Func<T, TProperty, CancellationToken, Task<bool>> predicate)
        {
            ArgumentNullException.ThrowIfNull(predicate);
            Unwrap(ruleBuilder).AddAsyncValidator(
                new AsyncPredicateValidator<T, TProperty>(
                    async (t, p, ct) => await predicate(t, p, ct).ConfigureAwait(false),
                    "The specified condition was not met for '{PropertyName}'."));
            return ruleBuilder;
        }

        /// <summary>Async predicate (ValueTask variant). See sibling overload for the rename rationale.</summary>
        public IRuleBuilder<T, TProperty> Must(
            Func<T, TProperty, CancellationToken, ValueTask<bool>> predicate)
        {
            ArgumentNullException.ThrowIfNull(predicate);
            Unwrap(ruleBuilder).AddAsyncValidator(
                new AsyncPredicateValidator<T, TProperty>(
                    (t, p, ct) => predicate(t, p, ct),
                    "The specified condition was not met for '{PropertyName}'."));
            return ruleBuilder;
        }
    }

    extension<T>(IRuleBuilder<T, string?> ruleBuilder)
    {
        public IRuleBuilder<T, string?> NotEmpty()
        {
            Unwrap(ruleBuilder).AddValidator(new NotEmptyStringValidator<T>());
            return ruleBuilder;
        }
    }

    extension<T, TCollection>(IRuleBuilder<T, TCollection?> ruleBuilder) where TCollection : class, IEnumerable
    {
        public IRuleBuilder<T, TCollection?> NotEmpty()
        {
            Unwrap(ruleBuilder).AddValidator(new EnumerableNotEmptyValidator<T, TCollection>());
            return ruleBuilder;
        }
    }

    extension<T>(IRuleBuilder<T, string?> ruleBuilder)
    {
        /// <summary>Inclusive length; use <c>0</c> for max-only or <see cref="int.MaxValue"/> for min-only.</summary>
        public IRuleBuilder<T, string?> Length(int min, int max)
        {
            Unwrap(ruleBuilder).AddValidator(new LengthRangeValidator<T>(min, max));
            return ruleBuilder;
        }
        public IRuleBuilder<T, string?> Matches(string pattern, RegexOptions options = RegexOptions.None)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(pattern);
            var regex = new Regex(pattern, options, TimeSpan.FromMilliseconds(250));
            Unwrap(ruleBuilder).AddValidator(new RegularExpressionValidator<T>(regex, "'{PropertyName}' was not in the correct format."));
            return ruleBuilder;
        }
    }

    extension<T, TComparable>(IRuleBuilder<T, TComparable?> ruleBuilder) where TComparable : struct, IComparable<TComparable>
    {
        public IRuleBuilder<T, TComparable?> GreaterThan(TComparable threshold)
=>
        AddNullableComparable(ruleBuilder, ComparableKind.GreaterThan, threshold, default);

        public IRuleBuilder<T, TComparable?> GreaterThanOrEqualTo(TComparable threshold)
    =>
            AddNullableComparable(ruleBuilder, ComparableKind.GreaterThanOrEqual, threshold, default);

        public IRuleBuilder<T, TComparable?> LessThan(TComparable threshold)
    =>
            AddNullableComparable(ruleBuilder, ComparableKind.LessThan, threshold, default);

        public IRuleBuilder<T, TComparable?> LessThanOrEqualTo(TComparable threshold)
    =>
            AddNullableComparable(ruleBuilder, ComparableKind.LessThanOrEqual, threshold, default);

        public IRuleBuilder<T, TComparable?> InclusiveBetween(TComparable from, TComparable to)
    =>
            AddNullableComparable(ruleBuilder, ComparableKind.BetweenInclusive, from, to);
    }

    extension<T, TComparable>(IRuleBuilder<T, TComparable> ruleBuilder) where TComparable : struct, IComparable<TComparable>
    {
        public IRuleBuilder<T, TComparable> GreaterThan(TComparable threshold)
=>
        AddComparable(ruleBuilder, ComparableKind.GreaterThan, threshold, default);

        public IRuleBuilder<T, TComparable> GreaterThanOrEqualTo(TComparable threshold)
    =>
            AddComparable(ruleBuilder, ComparableKind.GreaterThanOrEqual, threshold, default);

        public IRuleBuilder<T, TComparable> LessThan(TComparable threshold)
    =>
            AddComparable(ruleBuilder, ComparableKind.LessThan, threshold, default);

        public IRuleBuilder<T, TComparable> LessThanOrEqualTo(TComparable threshold)
    =>
            AddComparable(ruleBuilder, ComparableKind.LessThanOrEqual, threshold, default);

        public IRuleBuilder<T, TComparable> InclusiveBetween(TComparable from, TComparable to)
    =>
            AddComparable(ruleBuilder, ComparableKind.BetweenInclusive, from, to);
    }

    extension<T, TProperty>(IRuleBuilder<T, TProperty> ruleBuilder)
    {
        /// <summary>Registers a custom async property validator (for example network I/O).</summary>
        public IRuleBuilder<T, TProperty> AddAsyncValidator(IAsyncPropertyValidator<T, TProperty> validator)
        {
            ArgumentNullException.ThrowIfNull(validator);
            Unwrap(ruleBuilder).AddAsyncValidator(validator);
            return ruleBuilder;
        }
        public IRuleBuilder<T, TProperty> SetValidator(IValidator<TProperty> validator)
        {
            ArgumentNullException.ThrowIfNull(validator);
            Unwrap(ruleBuilder).AddNestedValidator(validator);
            return ruleBuilder;
        }
    }

    private static IRuleBuilder<T, TComparable?> AddNullableComparable<T, TComparable>(
        IRuleBuilder<T, TComparable?> ruleBuilder,
        ComparableKind kind,
        TComparable low,
        TComparable high)
        where TComparable : struct, IComparable<TComparable>
    {
        Unwrap(ruleBuilder).AddValidator(new NullableComparableValidator<T, TComparable>(kind, low, high));
        return ruleBuilder;
    }

    private static IRuleBuilder<T, TComparable> AddComparable<T, TComparable>(
        IRuleBuilder<T, TComparable> ruleBuilder,
        ComparableKind kind,
        TComparable low,
        TComparable high)
        where TComparable : struct, IComparable<TComparable>
    {
        Unwrap(ruleBuilder).AddValidator(new NonNullComparableValidator<T, TComparable>(kind, low, high));
        return ruleBuilder;
    }

    private static RuleBuilder<T, TProperty> Unwrap<T, TProperty>(IRuleBuilder<T, TProperty> ruleBuilder) =>
        ruleBuilder as RuleBuilder<T, TProperty>
        ?? throw new ArgumentException("Unsupported rule builder implementation.", nameof(ruleBuilder));
}
