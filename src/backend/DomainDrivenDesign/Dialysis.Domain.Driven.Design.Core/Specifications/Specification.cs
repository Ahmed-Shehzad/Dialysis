using System.Linq.Expressions;
using Dialysis.DomainDrivenDesign.Specifications;

namespace Dialysis.DomainDrivenDesign.Specifications;

/// <summary>
/// Abstract base for specifications. Subclasses implement <see cref="ToExpression"/>;
/// <see cref="IsSatisfiedBy"/> compiles and evaluates the expression in memory.
/// Use <see cref="And"/>, <see cref="Or"/>, and <see cref="Not"/> to compose rules.
/// </summary>
public abstract class Specification<T> : ISpecification<T>
{
    public abstract Expression<Func<T, bool>> ToExpression();

    public bool IsSatisfiedBy(T candidate) => ToExpression().Compile()(candidate);

    public Specification<T> And(Specification<T> other) => new AndSpecification<T>(this, other);

    public Specification<T> Or(Specification<T> other) => new OrSpecification<T>(this, other);

    public Specification<T> Not() => new NotSpecification<T>(this);
}
