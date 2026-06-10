using System.Collections.Generic;

namespace Asymptote.Shared.Engine.Expression.Tree;

public abstract record ExpressionNode;

public record LiteralNode(object Value) : ExpressionNode;

public record VariableNode(string Name) : ExpressionNode;

public record TernaryNode(ExpressionNode Condition, ExpressionNode TrueExpression, ExpressionNode FalseExpression)
    : ExpressionNode;

public record StringInterpolationNode(List<ExpressionNode> Parts) : ExpressionNode;

public record UnaryNode(OperatorType Operator, ExpressionNode Operand) : ExpressionNode;

public record BinaryNode(OperatorType Operator, ExpressionNode Left, ExpressionNode Right) : ExpressionNode;

public record EmptyNode : ExpressionNode;

public enum OperatorType
{
    NOT,
    NEGATE,

    ADD,
    SUBTRACT,
    MULTIPLY,
    DIVIDE,

    AND,
    OR,

    EQUAL,
    LESS_THAN,
    GREATER_THAN,
    LESS_THAN_OR_EQUAL,
    GREATER_THAN_OR_EQUAL,
}