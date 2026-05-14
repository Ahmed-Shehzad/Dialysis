using System.Linq.Expressions;

namespace Dialysis.DomainDrivenDesign.Specifications;

internal sealed class SpecificationExpressionRebinder(ParameterExpression source, ParameterExpression target) : ExpressionVisitor
{
    public static Expression Replace(Expression body, ParameterExpression source, ParameterExpression target)
        => new SpecificationExpressionRebinder(source, target).Visit(body);

    protected override Expression VisitParameter(ParameterExpression node)
        => node == source ? target : base.VisitParameter(node);
}
