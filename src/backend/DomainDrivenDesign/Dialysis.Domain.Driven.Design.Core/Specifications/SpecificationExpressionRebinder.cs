using System.Linq.Expressions;

namespace Dialysis.DomainDrivenDesign.Specifications;

internal sealed class SpecificationExpressionRebinder : ExpressionVisitor
{
    private readonly ParameterExpression _source;
    private readonly ParameterExpression _target;
    public SpecificationExpressionRebinder(ParameterExpression source, ParameterExpression target)
    {
        _source = source;
        _target = target;
    }
    public static Expression Replace(Expression body, ParameterExpression source, ParameterExpression target)
        => new SpecificationExpressionRebinder(source, target).Visit(body);

    protected override Expression VisitParameter(ParameterExpression node)
        => node == _source ? _target : base.VisitParameter(node);
}
