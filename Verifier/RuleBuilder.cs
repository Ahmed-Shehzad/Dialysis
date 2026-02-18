using Verifier.Abstractions;

namespace Verifier;

public sealed class RuleBuilder<T, TProperty> : IValidationRule<T>
{
    private readonly Func<T, TProperty> _getter;
    private readonly string _propertyName;

    // Each rule returns a failure or null (null == pass)
    private readonly List<Func<T, ValidationFailure?>> _rules = [];

    public RuleBuilder(string propertyName, Func<T, TProperty> getter)
    {
        _propertyName = propertyName;
        _getter = getter;
    }

    public RuleBuilder<T, TProperty> NotNull(string? message = null)
    {
        _rules.Add(obj =>
        {
            TProperty value = _getter(obj);
            return value is null
                ? new ValidationFailure(
                    _propertyName, message ?? $"{_propertyName} must not be null.")
                : null;
        });
        return this;
    }

    public RuleBuilder<T, TProperty> NotEmpty(string? message = null)
    {
        _rules.Add(obj =>
        {
            TProperty value = _getter(obj);

            bool isEmpty =
                value is null ||
                (value is string s && string.IsNullOrWhiteSpace(s)) ||
                (value is Ulid g && g == Ulid.Empty);

            return isEmpty
                ? new ValidationFailure(
                    _propertyName, message ?? $"{_propertyName} must not be empty.")
                : null;
        });
        return this;
    }

    public RuleBuilder<T, TProperty> Must(Func<TProperty, bool> predicate, string message)
    {
        _rules.Add(obj =>
        {
            TProperty value = _getter(obj);
            return !predicate(value) ? new ValidationFailure(_propertyName, message) : null;
        });
        return this;
    }

    public IEnumerable<ValidationFailure> Validate(T instance) => _rules.Select(rule => rule(instance)).OfType<ValidationFailure>();
}
