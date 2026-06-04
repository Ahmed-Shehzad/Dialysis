using System.Linq.Expressions;

namespace Dialysis.DomainDrivenDesign.Specifications;

public sealed class NotSpecification<T> : Specification<T>
{
    private readonly Specification<T> _inner;
    public NotSpecification(Specification<T> inner) => _inner = inner;
    public override Expression<Func<T, bool>> ToExpression()
    {
        var innerExpr = _inner.ToExpression();
        var parameter = innerExpr.Parameters[0];
        return Expression.Lambda<Func<T, bool>>(Expression.Not(innerExpr.Body), parameter);
    }
}
