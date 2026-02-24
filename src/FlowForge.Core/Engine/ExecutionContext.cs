using System.Collections.Concurrent;

namespace FlowForge.Core.Engine;

public class ExecutionContext
{
    private readonly ConcurrentDictionary<string, object?> _variables = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, object?> Variables => _variables;

    public void SetVariable(string name, object? value)
    {
        ArgumentNullException.ThrowIfNull(name);
        _variables[name] = value;
    }

    public object? GetVariable(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        _variables.TryGetValue(name, out var value);
        return value;
    }

    public T? GetVariable<T>(string name, T? defaultValue = default)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (!_variables.TryGetValue(name, out var value) || value is null)
            return defaultValue;

        if (value is T typed)
            return typed;

        try
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return defaultValue;
        }
    }

    public bool HasVariable(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        return _variables.ContainsKey(name);
    }

    public string InterpolateString(string template)
    {
        if (string.IsNullOrEmpty(template))
            return template;

        var result = template;
        foreach (var kvp in _variables)
        {
            result = result.Replace($"{{{{{kvp.Key}}}}}", kvp.Value?.ToString() ?? "");
        }
        return result;
    }
}
