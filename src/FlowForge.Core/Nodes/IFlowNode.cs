using FlowForge.Core.Engine;
using FlowForge.Core.Models;

namespace FlowForge.Core.Nodes;

public interface IFlowNode
{
    string Type { get; }
    NodeDefinition Definition { get; }
    Task<ExecutionResult> ExecuteAsync(NodeConfig config, Engine.ExecutionContext context, CancellationToken ct = default);
}
