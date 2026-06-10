using System;
using Asymptote.Shared.Engine.Expression.Tree;

namespace Asymptote.Shared.Engine.Expression.Context;

#nullable enable
public class ExpressionContext
{
    private readonly Func<string, object> _lookup;

    public ExpressionContext(Func<string, object?> lookup)
    {
        _lookup = lookup ?? throw new ArgumentNullException(nameof(lookup));
    }

    public object? GetVariable(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Variable name cannot be null or empty", nameof(name));

        var value = _lookup(name);
        return value;
    }
}