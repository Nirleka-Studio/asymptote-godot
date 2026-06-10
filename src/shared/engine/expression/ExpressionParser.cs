using System;
using System.Collections.Generic;
using System.Text;
using Asymptote.Shared.Engine.Expression.Context;
using Asymptote.Shared.Engine.Expression.Tree;
using StringReader = Asymptote.Util.String.StringReader;

namespace Asymptote.Shared.Engine.Expression;

public class ExpressionParser
{
    private const string FIRST_TERNARY_OPERATOR = "?";
    private const string SECOND_TERNARY_OPERATOR = ":";
    private const string NOT_OPERATOR = "!";
    private const string OR_OPERATOR = "||";
    private const string AND_OPERATOR = "&&";
    private const string EQUALITY_OPERATOR = "==";
    private const string LESS_THAN_OPERATOR = "<";
    private const string MORE_THAN_OPERATOR = ">";
    private const string LESS_THAN_OET_OPERATOR = "<=";
    private const string MORE_THAN_OET_OPERATOR = ">=";
    private const string ADDITION_OPERATOR = "+";
    private const string SUBTRACTION_OR_NEGATE_OPERATOR = "-";
    private const string MULTIPLICATION_OPERATOR = "*";
    private const string DIVISION_OPERATOR = "/";

    private const string GROUP_OPENING_CHAR = "(";
    private const string GROUP_CLOSING_CHAR = ")";
    private const string STRING_INTERP_OPENING_CHAR = "{";
    private const string STRING_INTERP_CLOSING_CHAR = "}";

    private static readonly Dictionary<string, int> OPERATOR_PRECEDENCES = new()
    {
        [FIRST_TERNARY_OPERATOR] = 10,
        [OR_OPERATOR] = 20,
        [AND_OPERATOR] = 30,
        [EQUALITY_OPERATOR] = 40,

        [LESS_THAN_OPERATOR] = 50,
        [MORE_THAN_OPERATOR] = 50,
        [LESS_THAN_OET_OPERATOR] = 50,
        [MORE_THAN_OET_OPERATOR] = 50,

        [ADDITION_OPERATOR] = 60,
        [SUBTRACTION_OR_NEGATE_OPERATOR] = 60,

        [MULTIPLICATION_OPERATOR] = 70,
        [DIVISION_OPERATOR] = 70,

        ["prefix"] = 80
    };

    private static readonly HashSet<string> STRING_CHARS = new()
    {
        "'",
        // '"'.ToString() // What in the unholy fuck
        "\"" // Oh, ja never mind
    };

    // Use native StringReader cuz I'm too lazy to write another one.
    // I'll regret it later.
    // Spoilers: I indeed regretted it.
    private readonly StringReader stringReader;

    public ExpressionParser(StringReader stringReader)
    {
        this.stringReader = stringReader;
    }

    public ExpressionParser(string expression)
    {
        this.stringReader = new StringReader(expression);
    }

    public object Evaluate(ExpressionNode node, ExpressionContext context)
    {
        return node switch
        {
            EmptyNode => null,
            LiteralNode literal => literal.Value,
            VariableNode variable => context.GetVariable(variable.Name),
            TernaryNode ternary => isTruthy(ternary.Condition, context)
                ? Evaluate(ternary.TrueExpression, context)
                : Evaluate(ternary.FalseExpression, context),

            UnaryNode unary => unary.Operator switch
            {
                OperatorType.NOT => !isTruthy(unary.Operand, context),
                OperatorType.NEGATE => -(double)Evaluate(unary.Operand, context),
                _ => throw new InvalidOperationException($"Unsupported unary operator: {unary.Operator}")
            },

            BinaryNode binary => binary.Operator switch
            {
                OperatorType.AND => isTruthy(binary.Left, context)
                    ? Evaluate(binary.Right, context)
                    : Evaluate(binary.Left, context),
                OperatorType.OR => isTruthy(binary.Left, context)
                    ? Evaluate(binary.Left, context)
                    : Evaluate(binary.Right, context),

                OperatorType.ADD => (double)Evaluate(binary.Left, context) + (double)Evaluate(binary.Right, context),
                OperatorType.SUBTRACT => (double)Evaluate(binary.Left, context) -
                                         (double)Evaluate(binary.Right, context),
                OperatorType.MULTIPLY => (double)Evaluate(binary.Left, context) *
                                         (double)Evaluate(binary.Right, context),
                OperatorType.DIVIDE => (double)Evaluate(binary.Left, context) / (double)Evaluate(binary.Right, context),

                OperatorType.EQUAL => Evaluate(binary.Left, context).Equals(Evaluate(binary.Right, context)),
                OperatorType.LESS_THAN => (double)Evaluate(binary.Left, context) <
                                          (double)Evaluate(binary.Right, context),
                OperatorType.GREATER_THAN => (double)Evaluate(binary.Left, context) >
                                             (double)Evaluate(binary.Right, context),
                OperatorType.LESS_THAN_OR_EQUAL => (double)Evaluate(binary.Left, context) <=
                                                   (double)Evaluate(binary.Right, context),
                OperatorType.GREATER_THAN_OR_EQUAL => (double)Evaluate(binary.Left, context) >=
                                                      (double)Evaluate(binary.Right, context),

                _ => throw new InvalidOperationException($"Unknown binary operator: {binary.Operator}")
            },

            _ => throw new ArgumentException($"Unknown node type: {node.GetType().Name}")
        };
    }

    private bool isTruthy(ExpressionNode node, ExpressionContext context)
    {
        var value = Evaluate(node, context);

        if (value is null) return false;
        if (value is bool booleanValue) return booleanValue;

        return true;
    }

    public ExpressionNode Parse()
    {
        if (!this.stringReader.CanRead()) return new EmptyNode();

        var result = parseExpression(0);

        this.stringReader.SkipWhitespace();
        if (this.stringReader.CanRead())
        {
            throw new Exception($"Unexpected character at end of expression: {(char)this.stringReader.Peek()}");
        }

        return result;
    }

    private ExpressionNode parseExpression(int minPrecedence)
    {
        this.stringReader.SkipWhitespace();
        var left = parsePrefix();

        while (true)
        {
            this.stringReader.SkipWhitespace();
            if (!this.stringReader.CanRead()) break;

            var op = peekOperator();
            if (op == null) break;

            var opPrecedence = OPERATOR_PRECEDENCES[op];
            if (opPrecedence <= minPrecedence) break;

            consumeOperator(op);

            if (op == FIRST_TERNARY_OPERATOR)
            {
                var trueBranch = parseExpression(0);

                this.stringReader.SkipWhitespace();
                if (!this.stringReader.CanRead() || this.stringReader.Peek().ToString() != SECOND_TERNARY_OPERATOR)
                    throw new InvalidOperationException("Expected ':' in ternary operator");
                this.stringReader.Read();

                var falseBranch = parseExpression(opPrecedence - 1);
                left = new TernaryNode(left, trueBranch, falseBranch);
            }
            else
            {
                var right = parseExpression(opPrecedence);
                left = new BinaryNode(stringToOperatorType(op), left, right);
            }
        }

        return left;
    }

    private ExpressionNode parsePrefix()
    {
        if (!this.stringReader.CanRead())
        {
            throw new InvalidOperationException("Unexpected end of expression");
        }

        var peekedInt = this.stringReader.Peek();

        var peekedChar = (char)peekedInt;
        var peekedCharStr = peekedChar.ToString();

        // Parentheses
        if (peekedCharStr == GROUP_OPENING_CHAR)
        {
            this.stringReader.Read();

            ExpressionNode expression = parseExpression(0);

            this.stringReader.SkipWhitespace();

            var nextPeek = this.stringReader.Peek();
            if (nextPeek.ToString() != GROUP_CLOSING_CHAR)
            {
                throw new Exception($"Unterminated group, expected {GROUP_CLOSING_CHAR}");
            }

            this.stringReader.Read();

            return expression;
        }

        // Unary operators
        if (peekedCharStr == NOT_OPERATOR || peekedCharStr == SUBTRACTION_OR_NEGATE_OPERATOR)
        {
            this.stringReader.Read();
            OperatorType operatorType = peekedCharStr == NOT_OPERATOR ? OperatorType.NOT : OperatorType.NEGATE;
            ExpressionNode operand = parseExpression(OPERATOR_PRECEDENCES["prefix"]);

            return new UnaryNode(operatorType, operand);
        }

        // Strings
        if (STRING_CHARS.Contains(peekedCharStr))
        {
            return parseString();
        }

        // Numbers
        if (isDigit(peekedChar))
        {
            return parseNumber();
        }

        // Variables
        if (isAlpha(peekedChar))
        {
            return parseIdentifier();
        }

        throw new InvalidOperationException($"Unexpected character: {peekedChar}");
    }

    private ExpressionNode parseNumber()
    {
        var numStr = new StringBuilder();

        while (this.stringReader.CanRead())
        {
            var peekedInt = this.stringReader.Peek();

            var peekedChar = (char)peekedInt;
            if (!isDigit(peekedChar) && peekedChar != '.') break;

            this.stringReader.Read();
            numStr.Append(peekedChar);
        }

        if (!double.TryParse(numStr.ToString(), out var num))
            throw new InvalidOperationException($"Invalid number format: {numStr}");

        return new LiteralNode(num);
    }

    private ExpressionNode parseIdentifier()
    {
        var name = new StringBuilder();

        while (this.stringReader.CanRead())
        {
            var peekedInt = this.stringReader.Peek();

            var peekedChar = (char)peekedInt;
            if (!isAlphaNumeric(peekedChar)) break;

            this.stringReader.Read();
            name.Append(peekedChar);
        }

        var nameStr = name.ToString();

        if (nameStr == "true") return new LiteralNode(true);
        if (nameStr == "false") return new LiteralNode(false);

        return new VariableNode(nameStr);
    }

    private StringInterpolationNode parseString()
    {
        var quote = (char)this.stringReader.Read(); // consume opening quote
        var parts = new List<ExpressionNode>();
        var currentString = new StringBuilder();

        while (this.stringReader.CanRead())
        {
            var ch = (char)this.stringReader.Read();

            if (ch == quote)
            {
                // End of string
                if (currentString.Length > 0)
                    parts.Add(new LiteralNode(currentString.ToString()));

                return new StringInterpolationNode(parts);
            }

            if (ch.ToString() == STRING_INTERP_OPENING_CHAR)
            {
                // Flush current buffer
                if (currentString.Length > 0)
                {
                    parts.Add(new LiteralNode(currentString.ToString()));
                    currentString.Clear();
                }

                // Parse inner expression
                var expr = parseExpression(0);
                parts.Add(expr);

                // Expect closing brace
                if (this.stringReader.CanRead() &&
                    ((char)this.stringReader.Peek()).ToString() == STRING_INTERP_CLOSING_CHAR)
                    this.stringReader.Read();
                else
                    throw new InvalidOperationException(
                        $"Expected '{STRING_INTERP_CLOSING_CHAR}' closing string interpolation"
                    );
            }
            else
            {
                currentString.Append(ch);
            }
        }

        throw new InvalidOperationException("Unterminated string literal");
    }

#nullable enable
    private string? peekOperator()
    {
        if (!this.stringReader.CanRead()) return null;
        var c1 = this.stringReader.Peek().ToString();
        var c2 = this.stringReader.PeekOffset(1);

        if (c2 != null)
        {
            var twoChars = c1 + c2;
            if (twoChars is EQUALITY_OPERATOR or AND_OPERATOR or OR_OPERATOR
                or LESS_THAN_OET_OPERATOR or MORE_THAN_OET_OPERATOR)
                return twoChars;
        }

        if (c1 is ADDITION_OPERATOR or SUBTRACTION_OR_NEGATE_OPERATOR or
            MULTIPLICATION_OPERATOR or DIVISION_OPERATOR or
            LESS_THAN_OPERATOR or MORE_THAN_OPERATOR or FIRST_TERNARY_OPERATOR)
            return c1;

        return null;
    }

    private void consumeOperator(string op)
    {
        for (var i = 0; i < op.Length; i++)
            this.stringReader.Read();
    }

    private static OperatorType stringToOperatorType(string op) => op switch
    {
        EQUALITY_OPERATOR => OperatorType.EQUAL,
        AND_OPERATOR => OperatorType.AND,
        OR_OPERATOR => OperatorType.OR,
        LESS_THAN_OPERATOR => OperatorType.LESS_THAN,
        MORE_THAN_OPERATOR => OperatorType.GREATER_THAN,
        LESS_THAN_OET_OPERATOR => OperatorType.LESS_THAN_OR_EQUAL,
        MORE_THAN_OET_OPERATOR => OperatorType.GREATER_THAN_OR_EQUAL,
        ADDITION_OPERATOR => OperatorType.ADD,
        SUBTRACTION_OR_NEGATE_OPERATOR => OperatorType.SUBTRACT,
        MULTIPLICATION_OPERATOR => OperatorType.MULTIPLY,
        DIVISION_OPERATOR => OperatorType.DIVIDE,
        _ => throw new InvalidOperationException($"Unknown operator: {op}")
    };

    private static bool isDigit(char c)
    {
        return c is >= '0' and <= '9';
    }

    private static bool isAlpha(char c)
    {
        return c is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or '_';
    }

    private static bool isAlphaNumeric(char c)
    {
        return isAlpha(c) || isDigit(c);
    }
}