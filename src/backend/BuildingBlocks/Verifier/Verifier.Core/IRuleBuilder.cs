namespace Dialysis.BuildingBlocks.Verifier;

/// <summary>
/// Fluent configuration for one member rule (from <see cref="AbstractValidator{T}.RuleFor{TProperty}"/>).
/// </summary>
public interface IRuleBuilder<out T, TProperty>
{
    IRuleBuilder<T, TProperty> StopOnFirstFailure();

    IRuleBuilder<T, TProperty> When(Func<T, bool> predicate);

    IRuleBuilder<T, TProperty> Unless(Func<T, bool> predicate);

    IRuleBuilder<T, TProperty> WithMessage(string message);
}
