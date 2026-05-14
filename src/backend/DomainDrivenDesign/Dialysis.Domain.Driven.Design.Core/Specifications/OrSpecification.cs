using System.Linq.Expressions;

namespace Dialysis.DomainDrivenDesign.Specifications;

public sealed class OrSpecification<T>(Specification<T> left, Specification<T> right) : Specification<T>
{
    public override Expression<Func<T, bool>> ToExpression()
    {
        var leftExpr = left.ToExpression();
        var rightExpr = right.ToExpression();
        var parameter = Expression.Parameter(typeof(T), "x");
        var body = Expression.OrElse(
            SpecificationExpressionRebinder.Replace(leftExpr.Body, leftExpr.Parameters[0], parameter),
            SpecificationExpressionRebinder.Replace(rightExpr.Body, rightExpr.Parameters[0], parameter));
        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }
}
