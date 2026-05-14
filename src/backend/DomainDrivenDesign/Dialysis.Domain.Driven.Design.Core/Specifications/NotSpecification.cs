using System.Linq.Expressions;

namespace Dialysis.DomainDrivenDesign.Specifications;

public sealed class NotSpecification<T>(Specification<T> inner) : Specification<T>
{
    public override Expression<Func<T, bool>> ToExpression()
    {
        var innerExpr = inner.ToExpression();
        var parameter = innerExpr.Parameters[0];
        return Expression.Lambda<Func<T, bool>>(Expression.Not(innerExpr.Body), parameter);
    }
}
