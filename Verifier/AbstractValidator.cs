using System.Linq.Expressions;

using Verifier.Abstractions;

namespace Verifier;

public abstract class AbstractValidator<T> : IValidator<T>
{
    private readonly List<IValidationRule<T>> _rules = [];
    private readonly List<IAsyncValidationRule<T>> _asyncRules = [];

    protected RuleBuilder<T, TProperty> RuleFor<TProperty>(
        Expression<Func<T, TProperty>> expression)
    {
        string propertyName = GetPropertyName(expression) ?? "Unknown";
        Func<T, TProperty> getter = expression.Compile();

        var builder = new RuleBuilder<T, TProperty>(propertyName, getter);
        _rules.Add(builder);
        return builder;
    }

    protected AsyncRuleBuilder<T, TProperty> RuleForAsync<TProperty>(
        Expression<Func<T, TProperty>> expression)
    {
        string propertyName = GetPropertyName(expression) ?? "Unknown";
        Func<T, TProperty> getter = expression.Compile();

        var builder = new AsyncRuleBuilder<T, TProperty>(propertyName, getter);
        _asyncRules.Add(builder);
        return builder;
    }

    public ValidationResult Validate(T instance)
    {
        var result = new ValidationResult();

        foreach (IValidationRule<T> rule in _rules) result.Errors.AddRange(rule.Validate(instance));

        return result;
    }

    public async Task<ValidationResult> ValidateAsync(T instance, CancellationToken ct = default)
    {
        var result = new ValidationResult();

        foreach (IValidationRule<T> rule in _rules) result.Errors.AddRange(rule.Validate(instance));

        if (_asyncRules.Count == 0) return result;

        IEnumerable<ValidationFailure>[] failures = await Task.WhenAll(_asyncRules.Select(r => r.ValidateAsync(instance, ct)));

        foreach (IEnumerable<ValidationFailure> failureGroup in failures) result.Errors.AddRange(failureGroup);

        return result;
    }

    private static string? GetPropertyName<TProperty>(Expression<Func<T, TProperty>> expr)
    {
        // Handles x => x.Prop and x => (object)x.Prop
        return expr.Body switch
        {
            MemberExpression m => m.Member.Name,
            UnaryExpression { Operand: MemberExpression m } => m.Member.Name,
            _ => null
        };
    }
}
