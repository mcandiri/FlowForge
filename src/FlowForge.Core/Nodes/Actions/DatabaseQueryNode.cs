using FlowForge.Core.Engine;
using FlowForge.Core.Models;
using ExecutionContext = FlowForge.Core.Engine.ExecutionContext;

namespace FlowForge.Core.Nodes.Actions;

public class DatabaseQueryNode : IFlowNode
{
    public string Type => "database-query";

    public NodeDefinition Definition => new()
    {
        Type = Type,
        Name = "Database Query",
        Description = "Execute a SQL query against a database. Returns mock result set in demo mode.",
        Icon = "\ud83d\uddc4\ufe0f",
        Category = NodeCategory.Action,
        InputPorts = new List<PortDefinition>
        {
            new() { Name = "input", Label = "Input" }
        },
        OutputPorts = new List<PortDefinition>
        {
            new() { Name = "success", Label = "Success" },
            new() { Name = "error", Label = "Error" }
        },
        ConfigFields = new List<ConfigField>
        {
            new() { Name = "ConnectionString", Label = "Connection String", Type = "string", Required = true },
            new() { Name = "Query", Label = "SQL Query", Type = "text", Required = true },
            new() { Name = "Parameters", Label = "Parameters (JSON)", Type = "text", DefaultValue = "{}" }
        }
    };

    public Task<ExecutionResult> ExecuteAsync(NodeConfig config, ExecutionContext context, CancellationToken ct = default)
    {
        try
        {
            var connectionString = context.InterpolateString(config.GetString("ConnectionString"));
            var query = context.InterpolateString(config.GetString("Query"));

            if (string.IsNullOrWhiteSpace(query))
            {
                return Task.FromResult(ExecutionResult.Failed("SQL query is required.", "error"));
            }

            // Determine query type for appropriate mock response
            var queryUpper = query.TrimStart().ToUpperInvariant();

            object mockResult;
            int rowsAffected;

            if (queryUpper.StartsWith("SELECT"))
            {
                // Return a mock result set
                var rows = new List<Dictionary<string, object?>>
                {
                    new() { ["id"] = 1, ["name"] = "Alice", ["email"] = "alice@example.com" },
                    new() { ["id"] = 2, ["name"] = "Bob", ["email"] = "bob@example.com" },
                    new() { ["id"] = 3, ["name"] = "Charlie", ["email"] = "charlie@example.com" }
                };
                mockResult = rows;
                rowsAffected = rows.Count;
            }
            else if (queryUpper.StartsWith("INSERT"))
            {
                mockResult = new Dictionary<string, object?> { ["insertedId"] = 42 };
                rowsAffected = 1;
            }
            else if (queryUpper.StartsWith("UPDATE"))
            {
                mockResult = new Dictionary<string, object?> { ["message"] = "Rows updated" };
                rowsAffected = 3;
            }
            else if (queryUpper.StartsWith("DELETE"))
            {
                mockResult = new Dictionary<string, object?> { ["message"] = "Rows deleted" };
                rowsAffected = 1;
            }
            else
            {
                mockResult = new Dictionary<string, object?> { ["message"] = "Query executed" };
                rowsAffected = 0;
            }

            var response = new Dictionary<string, object?>
            {
                ["result"] = mockResult,
                ["rowsAffected"] = rowsAffected,
                ["query"] = query
            };

            context.SetVariable("queryResult", mockResult);
            context.SetVariable("rowsAffected", rowsAffected);

            var executionResult = ExecutionResult.Succeeded(response, "success");
            executionResult.OutputVariables["queryResult"] = mockResult;
            executionResult.OutputVariables["rowsAffected"] = rowsAffected;

            return Task.FromResult(executionResult);
        }
        catch (Exception ex)
        {
            return Task.FromResult(ExecutionResult.Failed($"Database query failed: {ex.Message}", "error"));
        }
    }
}
