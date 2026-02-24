using System.Text;
using FlowForge.Core.Models;

namespace FlowForge.Core.Export;

/// <summary>
/// Generates clean, readable, copy-pasteable C# code from a FlowForge workflow.
/// Walks the workflow graph in topological order from start nodes and maps each
/// node type to appropriate C# constructs.
/// </summary>
public class CSharpCodeGenerator : IWorkflowExporter
{
    public string Format => "csharp";

    private int _stepCounter;

    public string Export(Workflow workflow)
    {
        ArgumentNullException.ThrowIfNull(workflow);
        _stepCounter = 0;

        var sb = new StringBuilder();
        string className = CodeTemplate.ToClassName(workflow.Metadata.Name);

        // File header
        sb.AppendLine(CodeTemplate.FileHeader(workflow.Metadata.Name, DateTime.UtcNow));
        sb.AppendLine();

        // Using statements
        sb.AppendLine(CodeTemplate.UsingStatements());
        sb.AppendLine();

        // Class declaration
        sb.AppendLine(CodeTemplate.ClassHeader(className));
        sb.AppendLine();

        // ExecuteAsync method
        sb.AppendLine(CodeTemplate.MethodHeader());

        // Build adjacency data structures
        var nodeMap = workflow.Nodes.ToDictionary(n => n.Id);
        var outgoingEdges = BuildOutgoingEdges(workflow.Edges);
        var incomingEdges = BuildIncomingEdges(workflow.Edges);

        // Find start nodes (nodes with no incoming edges)
        var startNodes = workflow.Nodes
            .Where(n => !incomingEdges.ContainsKey(n.Id) || incomingEdges[n.Id].Count == 0)
            .ToList();

        // If no start nodes found, use the first node
        if (startNodes.Count == 0 && workflow.Nodes.Count > 0)
            startNodes.Add(workflow.Nodes[0]);

        // Walk the graph in topological order
        var visited = new HashSet<string>();
        var sortedNodes = new List<FlowNode>();
        TopologicalSort(startNodes, outgoingEdges, nodeMap, visited, sortedNodes);

        // Add any remaining unvisited nodes
        foreach (var node in workflow.Nodes)
        {
            if (!visited.Contains(node.Id))
                sortedNodes.Add(node);
        }

        // Generate code for each node, tracking branching structure
        GenerateNodeCode(sb, sortedNodes, outgoingEdges, nodeMap, indentLevel: 2);

        // Close method and class
        sb.AppendLine(CodeTemplate.MethodAndClassFooter());

        return sb.ToString();
    }

    /// <summary>
    /// Performs a topological sort from the given start nodes via DFS.
    /// </summary>
    private static void TopologicalSort(
        List<FlowNode> startNodes,
        Dictionary<string, List<FlowEdge>> outgoingEdges,
        Dictionary<string, FlowNode> nodeMap,
        HashSet<string> visited,
        List<FlowNode> result)
    {
        var queue = new Queue<FlowNode>(startNodes);

        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            if (visited.Contains(node.Id))
                continue;

            visited.Add(node.Id);
            result.Add(node);

            if (!outgoingEdges.TryGetValue(node.Id, out var edges))
                continue;

            foreach (var edge in edges)
            {
                if (nodeMap.TryGetValue(edge.TargetNodeId, out var targetNode) && !visited.Contains(targetNode.Id))
                    queue.Enqueue(targetNode);
            }
        }
    }

    /// <summary>
    /// Generates C# code for a list of nodes in order, handling branching for condition nodes.
    /// </summary>
    private void GenerateNodeCode(
        StringBuilder sb,
        List<FlowNode> nodes,
        Dictionary<string, List<FlowEdge>> outgoingEdges,
        Dictionary<string, FlowNode> nodeMap,
        int indentLevel)
    {
        var processedInBranch = new HashSet<string>();

        foreach (var node in nodes)
        {
            if (processedInBranch.Contains(node.Id))
                continue;

            _stepCounter++;

            string nodeType = NormalizeType(node.Type);

            // For condition nodes, handle the branching structure
            if (nodeType is "condition")
            {
                GenerateConditionNode(sb, node, outgoingEdges, nodeMap, indentLevel, processedInBranch);
                continue;
            }

            // For loop nodes, handle the loop structure
            if (nodeType is "loop")
            {
                GenerateLoopNode(sb, node, outgoingEdges, nodeMap, indentLevel, processedInBranch);
                continue;
            }

            // Generate code for the individual node
            string code = GenerateSingleNodeCode(node, indentLevel);
            if (!string.IsNullOrEmpty(code))
            {
                sb.AppendLine(code);
                sb.AppendLine();
            }
        }
    }

    /// <summary>
    /// Generates C# code for a single node based on its type.
    /// </summary>
    private string GenerateSingleNodeCode(FlowNode node, int indentLevel)
    {
        string nodeType = NormalizeType(node.Type);
        var sb = new StringBuilder();
        string description = !string.IsNullOrWhiteSpace(node.Name) ? node.Name : GetDefaultDescription(nodeType);

        sb.AppendLine(CodeTemplate.StepComment(_stepCounter, description, indentLevel));

        switch (nodeType)
        {
            case "http-request":
                GenerateHttpRequestCode(sb, node, indentLevel);
                break;

            case "logger":
                GenerateLoggerCode(sb, node, indentLevel);
                break;

            case "delay":
                GenerateDelayCode(sb, node, indentLevel);
                break;

            case "set-variable":
                GenerateSetVariableCode(sb, node, indentLevel);
                break;

            case "transform":
                GenerateTransformCode(sb, node, indentLevel);
                break;

            case "email-sender":
                GenerateEmailCode(sb, node, indentLevel);
                break;

            case "database-query":
                GenerateDatabaseCode(sb, node, indentLevel);
                break;

            case "code-block":
                GenerateCodeBlockCode(sb, node, indentLevel);
                break;

            case "webhook-trigger":
                GenerateWebhookCode(sb, node, indentLevel);
                break;

            case "retry":
                GenerateRetryCode(sb, node, indentLevel);
                break;

            default:
                string indent = new(' ', indentLevel * 4);
                sb.Append($"{indent}// {node.Type}: {description}");
                break;
        }

        return sb.ToString();
    }

    private static void GenerateHttpRequestCode(StringBuilder sb, FlowNode node, int indentLevel)
    {
        string url = node.Config.GetString("Url", node.Config.GetString("url", "https://example.com/api"));
        string method = node.Config.GetString("Method", node.Config.GetString("method", "GET")).ToUpperInvariant();

        if (method == "POST")
            sb.Append(CodeTemplate.HttpPostRequest(url, "response", indentLevel));
        else
            sb.Append(CodeTemplate.HttpGetRequest(url, "response", indentLevel));
    }

    private static void GenerateLoggerCode(StringBuilder sb, FlowNode node, int indentLevel)
    {
        string message = node.Config.GetString("Message", node.Config.GetString("message", "Log output"));
        sb.Append(CodeTemplate.LogStatement(message, indentLevel));
    }

    private static void GenerateDelayCode(StringBuilder sb, FlowNode node, int indentLevel)
    {
        int milliseconds = node.Config.GetInt("Duration", node.Config.GetInt("milliseconds", node.Config.GetInt("delay", 1000)));
        sb.Append(CodeTemplate.DelayStatement(milliseconds, indentLevel));
    }

    private static void GenerateSetVariableCode(StringBuilder sb, FlowNode node, int indentLevel)
    {
        string variableName = node.Config.GetString("variableName", node.Config.GetString("name", "variable"));
        string value = node.Config.GetString("value", "null");
        sb.Append(CodeTemplate.VariableAssignment(variableName, $"\"{CodeTemplate.EscapeString(value)}\"", indentLevel));
    }

    private static void GenerateTransformCode(StringBuilder sb, FlowNode node, int indentLevel)
    {
        string expression = node.Config.GetString("Mapping", node.Config.GetString("expression", ""));
        string variableName = node.Config.GetString("outputVariable", "result");
        sb.Append(CodeTemplate.TransformBlock(expression, variableName, indentLevel));
    }

    private static void GenerateEmailCode(StringBuilder sb, FlowNode node, int indentLevel)
    {
        string to = node.Config.GetString("to", "recipient@example.com");
        string subject = node.Config.GetString("subject", "Notification");
        sb.Append(CodeTemplate.EmailBlock(to, subject, indentLevel));
    }

    private static void GenerateDatabaseCode(StringBuilder sb, FlowNode node, int indentLevel)
    {
        string query = node.Config.GetString("query", "SELECT * FROM table");
        sb.Append(CodeTemplate.DatabaseQueryBlock(query, indentLevel));
    }

    private static void GenerateCodeBlockCode(StringBuilder sb, FlowNode node, int indentLevel)
    {
        string code = node.Config.GetString("code", "// Custom code block");
        sb.Append(CodeTemplate.InlineCode(code, indentLevel));
    }

    private static void GenerateWebhookCode(StringBuilder sb, FlowNode node, int indentLevel)
    {
        string path = node.Config.GetString("path", "/webhook");
        string method = node.Config.GetString("method", "POST");
        sb.Append(CodeTemplate.WebhookEntry(path, method, indentLevel));
    }

    private static void GenerateRetryCode(StringBuilder sb, FlowNode node, int indentLevel)
    {
        int maxRetries = node.Config.GetInt("maxRetries", node.Config.GetInt("MaxRetries", 3));
        int delayMs = node.Config.GetInt("delayMs", node.Config.GetInt("DelayMs", 1000));
        sb.Append(CodeTemplate.RetryBlock(maxRetries, delayMs, indentLevel));
    }

    /// <summary>
    /// Generates a condition node with if/else branching, including nested child nodes.
    /// </summary>
    private void GenerateConditionNode(
        StringBuilder sb,
        FlowNode node,
        Dictionary<string, List<FlowEdge>> outgoingEdges,
        Dictionary<string, FlowNode> nodeMap,
        int indentLevel,
        HashSet<string> processedInBranch)
    {
        string condition = node.Config.GetString("condition", node.Config.GetString("expression", "true"));
        string description = !string.IsNullOrWhiteSpace(node.Name) ? node.Name : "Check condition";

        sb.AppendLine(CodeTemplate.StepComment(_stepCounter, description, indentLevel));
        sb.AppendLine(CodeTemplate.ConditionStart(condition, indentLevel));
        sb.AppendLine(CodeTemplate.OpenBrace(indentLevel));

        // Separate true/false branches based on output port names
        var trueBranch = new List<FlowNode>();
        var falseBranch = new List<FlowNode>();

        if (outgoingEdges.TryGetValue(node.Id, out var edges))
        {
            foreach (var edge in edges)
            {
                if (!nodeMap.TryGetValue(edge.TargetNodeId, out var targetNode))
                    continue;

                string portName = edge.SourcePortName.ToLowerInvariant();
                if (portName is "false" or "else" or "no")
                    falseBranch.Add(targetNode);
                else
                    trueBranch.Add(targetNode);
            }
        }

        // Generate true branch nodes
        if (trueBranch.Count > 0)
        {
            int savedStep = _stepCounter;
            foreach (var branchNode in trueBranch)
            {
                processedInBranch.Add(branchNode.Id);
                _stepCounter++;
                string branchCode = GenerateSingleNodeCode(branchNode, indentLevel + 1);
                if (!string.IsNullOrEmpty(branchCode))
                    sb.AppendLine(branchCode);
            }
        }
        else
        {
            string innerIndent = new(' ', (indentLevel + 1) * 4);
            sb.AppendLine($"{innerIndent}// True branch");
        }

        sb.AppendLine(CodeTemplate.CloseBrace(indentLevel));

        // Generate false branch if it exists
        if (falseBranch.Count > 0)
        {
            sb.AppendLine(CodeTemplate.ElseBlock(indentLevel));
            sb.AppendLine(CodeTemplate.OpenBrace(indentLevel));

            foreach (var branchNode in falseBranch)
            {
                processedInBranch.Add(branchNode.Id);
                _stepCounter++;
                string branchCode = GenerateSingleNodeCode(branchNode, indentLevel + 1);
                if (!string.IsNullOrEmpty(branchCode))
                    sb.AppendLine(branchCode);
            }

            sb.AppendLine(CodeTemplate.CloseBrace(indentLevel));
        }

        sb.AppendLine();
    }

    /// <summary>
    /// Generates a loop node with a foreach block, including loop body child nodes.
    /// </summary>
    private void GenerateLoopNode(
        StringBuilder sb,
        FlowNode node,
        Dictionary<string, List<FlowEdge>> outgoingEdges,
        Dictionary<string, FlowNode> nodeMap,
        int indentLevel,
        HashSet<string> processedInBranch)
    {
        string collection = node.Config.GetString("collection", "items");
        string variableName = node.Config.GetString("variable", node.Config.GetString("itemVariable", "item"));
        string description = !string.IsNullOrWhiteSpace(node.Name) ? node.Name : "Loop through items";

        sb.AppendLine(CodeTemplate.StepComment(_stepCounter, description, indentLevel));
        sb.AppendLine(CodeTemplate.ForEachStart(variableName, collection, indentLevel));
        sb.AppendLine(CodeTemplate.OpenBrace(indentLevel));

        // Find loop body nodes (connected via "body" or "loop" port, or default output)
        var bodyNodes = new List<FlowNode>();
        if (outgoingEdges.TryGetValue(node.Id, out var edges))
        {
            foreach (var edge in edges)
            {
                string portName = edge.SourcePortName.ToLowerInvariant();
                if (portName is "iteration" or "body" or "loop" or "output" or "")
                {
                    if (nodeMap.TryGetValue(edge.TargetNodeId, out var targetNode))
                        bodyNodes.Add(targetNode);
                }
            }
        }

        if (bodyNodes.Count > 0)
        {
            foreach (var bodyNode in bodyNodes)
            {
                processedInBranch.Add(bodyNode.Id);
                _stepCounter++;
                string bodyCode = GenerateSingleNodeCode(bodyNode, indentLevel + 1);
                if (!string.IsNullOrEmpty(bodyCode))
                    sb.AppendLine(bodyCode);
            }
        }
        else
        {
            string innerIndent = new(' ', (indentLevel + 1) * 4);
            sb.AppendLine($"{innerIndent}// Loop body");
        }

        sb.AppendLine(CodeTemplate.CloseBrace(indentLevel));
        sb.AppendLine();
    }

    /// <summary>
    /// Builds a dictionary mapping source node IDs to their outgoing edges.
    /// </summary>
    private static Dictionary<string, List<FlowEdge>> BuildOutgoingEdges(List<FlowEdge> edges)
    {
        var result = new Dictionary<string, List<FlowEdge>>();
        foreach (var edge in edges)
        {
            if (!result.ContainsKey(edge.SourceNodeId))
                result[edge.SourceNodeId] = new List<FlowEdge>();
            result[edge.SourceNodeId].Add(edge);
        }
        return result;
    }

    /// <summary>
    /// Builds a dictionary mapping target node IDs to their incoming edges.
    /// </summary>
    private static Dictionary<string, List<FlowEdge>> BuildIncomingEdges(List<FlowEdge> edges)
    {
        var result = new Dictionary<string, List<FlowEdge>>();
        foreach (var edge in edges)
        {
            if (!result.ContainsKey(edge.TargetNodeId))
                result[edge.TargetNodeId] = new List<FlowEdge>();
            result[edge.TargetNodeId].Add(edge);
        }
        return result;
    }

    /// <summary>
    /// Normalizes a node type string to a canonical short form for matching.
    /// Maps variants like "conditionnode", "conditionNode", "condition" all to "condition".
    /// </summary>
    private static string NormalizeType(string type)
    {
        var lower = type.ToLowerInvariant();
        return lower switch
        {
            "httprequestnode" or "httprequest" or "http-request" => "http-request",
            "conditionnode" or "condition" => "condition",
            "loopnode" or "loop" => "loop",
            "loggernode" or "logger" => "logger",
            "delaynode" or "delay" => "delay",
            "setvariablenode" or "setvariable" or "set-variable" => "set-variable",
            "transformnode" or "transform" => "transform",
            "emailsendernode" or "emailsender" or "email-sender" => "email-sender",
            "databasequerynode" or "databasequery" or "database-query" => "database-query",
            "codeblocknode" or "codeblock" or "code-block" => "code-block",
            "webhooktriggernode" or "webhooktrigger" or "webhook-trigger" => "webhook-trigger",
            "retrynode" or "retry" => "retry",
            _ => lower
        };
    }

    /// <summary>
    /// Returns a default description for a node type.
    /// </summary>
    private static string GetDefaultDescription(string nodeType)
    {
        return NormalizeType(nodeType) switch
        {
            "http-request" => "HTTP Request",
            "condition" => "Check condition",
            "loop" => "Loop through items",
            "logger" => "Log output",
            "delay" => "Delay execution",
            "set-variable" => "Set variable",
            "transform" => "Transform data",
            "email-sender" => "Send email notification",
            "database-query" => "Execute database query",
            "code-block" => "Execute code block",
            "webhook-trigger" => "Webhook entry point",
            "retry" => "Retry on failure",
            _ => "Process step"
        };
    }
}
