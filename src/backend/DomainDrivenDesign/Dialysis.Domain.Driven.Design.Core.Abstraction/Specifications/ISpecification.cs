namespace Dialysis.DomainDrivenDesign.Specifications;

/// <summary>
/// Encapsulates a business rule or selection predicate over <typeparamref name="T"/> in the domain
/// (evaluate in memory); map to persistence queries in the infrastructure layer when needed.
/// </summary>
public interface ISpecification<in T>
{
    bool IsSatisfiedBy(T candidate);
}
