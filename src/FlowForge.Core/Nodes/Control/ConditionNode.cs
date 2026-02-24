using FlowForge.Core.Engine;
using FlowForge.Core.Models;
using ExecutionContext = FlowForge.Core.Engine.ExecutionContext;

namespace FlowForge.Core.Nodes.Control;

public class ConditionNode : IFlowNode
{
    public string Type => "condition";

    public NodeDefinition Definition => new()
    {
        Type = Type,
        Name = "Condition",
        Description = "If/else branching based on an expression. Supports variable interpolation and comparison operators.",
        Icon = "\ud83d\udd00",
        Category = NodeCategory.Control,
        InputPorts = new List<PortDefinition>
        {
            new() { Name = "input", Label = "Input" }
        },
        OutputPorts = new List<PortDefinition>
        {
            new() { Name = "true", Label = "True" },
            new() { Name = "false", Label = "False" }
        },
        ConfigFields = new List<ConfigField>
        {
            new() { Name = "Expression", Label = "Expression", Type = "string", Required = true }
        }
    };

    public Task<ExecutionResult> ExecuteAsync(NodeConfig config, ExecutionContext context, CancellationToken ct = default)
    {
        var expression = config.GetString("Expression");

        if (string.IsNullOrWhiteSpace(expression))
        {
            return Task.FromResult(ExecutionResult.Failed("Expression is required."));
        }

        // Interpolate variables into the expression
        var interpolated = context.InterpolateString(expression);

        var conditionMet = EvaluateCondition(interpolated);

        var outputPort = conditionMet ? "true" : "false";
        var result = ExecutionResult.Succeeded(
            new Dictionary<string, object?>
            {
                ["expression"] = expression,
                ["interpolated"] = interpolated,
                ["result"] = conditionMet
            },
            outputPort
        );
        result.OutputVariables["conditionResult"] = conditionMet;

        return Task.FromResult(result);
    }

    private static bool EvaluateCondition(string expression)
    {
        // Support common comparison operators
        string[] operators = { "===", "!==", "==", "!=", ">=", "<=", ">", "<" };

        foreach (var op in operators)
        {
            var index = expression.IndexOf(op, StringComparison.Ordinal);
            if (index < 0) continue;

            var left = expression[..index].Trim();
            var right = expression[(index + op.Length)..].Trim();

            return op switch
            {
                "===" or "==" => CompareValues(left, right) == 0,
                "!==" or "!=" => CompareValues(left, right) != 0,
                ">=" => CompareValues(left, right) >= 0,
                "<=" => CompareValues(left, right) <= 0,
                ">" => CompareValues(left, right) > 0,
                "<" => CompareValues(left, right) < 0,
                _ => false
            };
        }

        // If no operator found, treat as truthy/falsy evaluation
        return IsTruthy(expression);
    }

    private static int CompareValues(string left, string right)
    {
        // Strip surrounding quotes if present
        left = StripQuotes(left);
        right = StripQuotes(right);

        // Try numeric comparison first
        if (double.TryParse(left, out var leftNum) && double.TryParse(right, out var rightNum))
        {
            return leftNum.CompareTo(rightNum);
        }

        // Boolean comparison
        if (bool.TryParse(left, out var leftBool) && bool.TryParse(right, out var rightBool))
        {
            return leftBool.CompareTo(rightBool);
        }

        // Fall back to string comparison (case-insensitive)
        return string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
    }

    private static string StripQuotes(string value)
    {
        if (value.Length >= 2)
        {
            if ((value.StartsWith('"') && value.EndsWith('"')) ||
                (value.StartsWith('\'') && value.EndsWith('\'')))
            {
                return value[1..^1];
            }
        }
        return value;
    }

    private static bool IsTruthy(string value)
    {
        var trimmed = value.Trim();

        if (string.IsNullOrWhiteSpace(trimmed) ||
            trimmed.Equals("false", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Equals("null", StringComparison.OrdinalIgnoreCase) ||
            trimmed.Equals("0", StringComparison.Ordinal) ||
            trimmed.Equals("undefined", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }
}
