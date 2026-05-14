using System.Linq.Expressions;

namespace Dialysis.DomainDrivenDesign.Specifications;

/// <summary>
/// Encapsulates a business rule or selection predicate over <typeparamref name="T"/> in the domain.
/// Exposes both an in-memory predicate (<see cref="IsSatisfiedBy"/>) and an expression form
/// (<see cref="ToExpression"/>) so infrastructure can translate the same rule to a persistence query.
/// </summary>
public interface ISpecification<T>
{
    Expression<Func<T, bool>> ToExpression();

    bool IsSatisfiedBy(T candidate);
}
