using FlowForge.Core.Engine;
using FlowForge.Core.Models;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using ExecutionContext = FlowForge.Core.Engine.ExecutionContext;

namespace FlowForge.Core.Nodes.Actions;

public class CodeBlockNode : IFlowNode
{
    public string Type => "code-block";

    public NodeDefinition Definition => new()
    {
        Type = Type,
        Name = "Code Block",
        Description = "Execute a C# expression or code snippet. Has access to workflow variables via the 'variables' dictionary.",
        Icon = "\ud83d\udcbb",
        Category = NodeCategory.Action,
        InputPorts = new List<PortDefinition>
        {
            new() { Name = "input", Label = "Input" }
        },
        OutputPorts = new List<PortDefinition>
        {
            new() { Name = "output", Label = "Output" },
            new() { Name = "error", Label = "Error" }
        },
        ConfigFields = new List<ConfigField>
        {
            new() { Name = "Code", Label = "C# Code", Type = "text", Required = true }
        }
    };

    public async Task<ExecutionResult> ExecuteAsync(NodeConfig config, ExecutionContext context, CancellationToken ct = default)
    {
        var code = config.GetString("Code");

        if (string.IsNullOrWhiteSpace(code))
        {
            return ExecutionResult.Failed("Code is required.", "error");
        }

        try
        {
            // Build a globals object with workflow variables accessible
            var globals = new CodeBlockGlobals
            {
                variables = context.Variables.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value,
                    StringComparer.OrdinalIgnoreCase)
            };

            var scriptOptions = ScriptOptions.Default
                .AddReferences(typeof(object).Assembly)
                .AddImports("System", "System.Collections.Generic", "System.Linq", "System.Text");

            var scriptResult = await CSharpScript.EvaluateAsync<object?>(
                code,
                scriptOptions,
                globals,
                typeof(CodeBlockGlobals),
                ct);

            context.SetVariable("codeResult", scriptResult);

            var result = ExecutionResult.Succeeded(scriptResult, "output");
            result.OutputVariables["codeResult"] = scriptResult;

            return result;
        }
        catch (CompilationErrorException ex)
        {
            var errors = string.Join(Environment.NewLine, ex.Diagnostics);
            return ExecutionResult.Failed($"Compilation error: {errors}", "error");
        }
        catch (Exception ex)
        {
            return ExecutionResult.Failed($"Code execution failed: {ex.Message}", "error");
        }
    }
}

/// <summary>
/// Globals object passed to the C# scripting engine, providing access to workflow variables.
/// </summary>
public class CodeBlockGlobals
{
    public Dictionary<string, object?> variables { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
