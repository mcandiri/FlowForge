using System.Collections.Concurrent;

namespace FlowForge.Core.Nodes;

public class NodeRegistry
{
    private readonly ConcurrentDictionary<string, IFlowNode> _nodes = new(StringComparer.OrdinalIgnoreCase);

    public void Register(IFlowNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        _nodes[node.Type] = node;
    }

    public IFlowNode? GetNode(string type)
    {
        ArgumentNullException.ThrowIfNull(type);
        _nodes.TryGetValue(type, out var node);
        return node;
    }

    public IReadOnlyList<IFlowNode> GetAllNodes() => _nodes.Values.ToList();

    public IReadOnlyList<NodeDefinition> GetAllDefinitions() => _nodes.Values.Select(n => n.Definition).ToList();

    public bool HasNode(string type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return _nodes.ContainsKey(type);
    }
}
