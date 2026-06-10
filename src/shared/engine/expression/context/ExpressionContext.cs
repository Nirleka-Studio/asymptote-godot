using System;
using System.Collections;
using System.Collections.Generic;

namespace Asymptote.Shared.Engine.Expression.Context;

#nullable enable
public class ExpressionContext : IEnumerable<KeyValuePair<string, object?>>
{
    private readonly Dictionary<string, object?> _variables = new();
    private readonly Func<string, object?>? _lookup;

    public ExpressionContext()
    {
    }

    public ExpressionContext(Func<string, object?> lookup)
    {
        _lookup = lookup ?? throw new ArgumentNullException(nameof(lookup));
    }

    public object? this[string key]
    {
        get => GetVariable(key);
        set => _variables[key] = value;
    }

    public object? GetVariable(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Variable name cannot be null or empty", nameof(name));

        if (_variables.TryGetValue(name, out var value))
            return value;

        return _lookup?.Invoke(name);
    }

    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => _variables.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}