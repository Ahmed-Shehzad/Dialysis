using System.Linq.Expressions;

namespace Dialysis.DomainDrivenDesign.Specifications;

public sealed class OrSpecification<T> : Specification<T>
{
    private readonly Specification<T> _left;
    private readonly Specification<T> _right;
    public OrSpecification(Specification<T> left, Specification<T> right)
    {
        _left = left;
        _right = right;
    }
    public override Expression<Func<T, bool>> ToExpression()
    {
        var leftExpr = _left.ToExpression();
        var rightExpr = _right.ToExpression();
        var parameter = Expression.Parameter(typeof(T), "x");
        var body = Expression.OrElse(
            SpecificationExpressionRebinder.Replace(leftExpr.Body, leftExpr.Parameters[0], parameter),
            SpecificationExpressionRebinder.Replace(rightExpr.Body, rightExpr.Parameters[0], parameter));
        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }
}
