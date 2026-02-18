using Verifier.Abstractions;

namespace Verifier;

public sealed class AsyncRuleBuilder<T, TProperty> : IAsyncValidationRule<T>
{
    private readonly Func<T, TProperty> _getter;
    private readonly string _propertyName;

    // Each rule returns a failure or null (null == pass)
    private readonly List<Func<T, CancellationToken, Task<ValidationFailure?>>> _rules = [];

    public AsyncRuleBuilder(string propertyName, Func<T, TProperty> getter)
    {
        _propertyName = propertyName;
        _getter = getter;
    }

    public AsyncRuleBuilder<T, TProperty> MustAsync(
        Func<TProperty, CancellationToken, Task<bool>> predicateAsync,
        string message)
    {
        _rules.Add(async (obj, ct) =>
        {
            TProperty value = _getter(obj);
            bool ok = await predicateAsync(value, ct);

            return ok ? null : new ValidationFailure(_propertyName, message);
        });
        return this;
    }

    public async Task<IEnumerable<ValidationFailure>> ValidateAsync(T instance, CancellationToken ct = default)
    {
        var failures = new List<ValidationFailure>();

        foreach (Func<T, CancellationToken, Task<ValidationFailure?>> rule in _rules)
        {
            ct.ThrowIfCancellationRequested();
            ValidationFailure? failure = await rule(instance, ct);
            if (failure is not null) failures.Add(failure);
        }

        return failures;
    }
}
