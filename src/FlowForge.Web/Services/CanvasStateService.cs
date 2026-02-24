using FlowForge.Core.Models;
using FlowForge.Core.Nodes;

namespace FlowForge.Web.Services;

/// <summary>
/// Central state manager for the workflow canvas. Holds nodes, edges,
/// selection state, zoom/pan, and raises change notifications so that
/// Blazor components can re-render reactively.
/// </summary>
public sealed class CanvasStateService
{
    private readonly NodeRegistry _nodeRegistry;

    public CanvasStateService(NodeRegistry nodeRegistry)
    {
        _nodeRegistry = nodeRegistry;
    }

    // ── State ────────────────────────────────────────────────
    public List<FlowNode> Nodes { get; private set; } = new();
    public List<FlowEdge> Edges { get; private set; } = new();
    public string? SelectedNodeId { get; private set; }
    public double ZoomLevel { get; set; } = 1.0;
    public double PanX { get; set; } = 0;
    public double PanY { get; set; } = 0;

    // ── Events ───────────────────────────────────────────────
    public event Action? OnStateChanged;
    public event Action<string>? OnNodeSelected;
    public event Action? OnCanvasCleared;

    // ── Node operations ──────────────────────────────────────

    public FlowNode? AddNode(string nodeType, double x, double y)
    {
        var definition = _nodeRegistry.GetAllDefinitions()
            .FirstOrDefault(d => d.Type == nodeType);

        if (definition is null)
            return null;

        // Snap to 20px grid
        x = Math.Round(x / 20) * 20;
        y = Math.Round(y / 20) * 20;

        var node = new FlowNode
        {
            Id = Guid.NewGuid().ToString(),
            Type = definition.Type,
            Name = definition.Name,
            X = x,
            Y = y,
            InputPorts = definition.InputPorts.Select(p => new NodePort
            {
                Id = Guid.NewGuid().ToString(),
                Name = p.Name,
                Type = PortType.Input,
                NodeId = string.Empty // set below
            }).ToList(),
            OutputPorts = definition.OutputPorts.Select(p => new NodePort
            {
                Id = Guid.NewGuid().ToString(),
                Name = p.Name,
                Type = PortType.Output,
                NodeId = string.Empty // set below
            }).ToList()
        };

        // Set NodeId on ports
        foreach (var port in node.InputPorts) port.NodeId = node.Id;
        foreach (var port in node.OutputPorts) port.NodeId = node.Id;

        // Set default config values
        foreach (var field in definition.ConfigFields)
        {
            if (field.DefaultValue is not null)
                node.Config[field.Name] = field.DefaultValue;
        }

        Nodes.Add(node);
        NotifyStateChanged();
        return node;
    }

    public void RemoveNode(string nodeId)
    {
        Nodes.RemoveAll(n => n.Id == nodeId);
        Edges.RemoveAll(e => e.SourceNodeId == nodeId || e.TargetNodeId == nodeId);

        if (SelectedNodeId == nodeId)
            SelectedNodeId = null;

        NotifyStateChanged();
    }

    public void MoveNode(string nodeId, double x, double y)
    {
        var node = Nodes.FirstOrDefault(n => n.Id == nodeId);
        if (node is null) return;

        node.X = Math.Round(x / 20) * 20;
        node.Y = Math.Round(y / 20) * 20;
        NotifyStateChanged();
    }

    public void SelectNode(string? nodeId)
    {
        SelectedNodeId = nodeId;
        OnNodeSelected?.Invoke(nodeId ?? string.Empty);
        NotifyStateChanged();
    }

    public FlowNode? GetSelectedNode()
    {
        return SelectedNodeId is null
            ? null
            : Nodes.FirstOrDefault(n => n.Id == SelectedNodeId);
    }

    public void UpdateNodeConfig(string nodeId, string key, object? value)
    {
        var node = Nodes.FirstOrDefault(n => n.Id == nodeId);
        if (node is null) return;

        node.Config[key] = value;
        NotifyStateChanged();
    }

    // ── Edge operations ──────────────────────────────────────

    public FlowEdge? AddEdge(string sourceNodeId, string sourcePortName,
                              string targetNodeId, string targetPortName)
    {
        // Prevent duplicate edges
        var exists = Edges.Any(e =>
            e.SourceNodeId == sourceNodeId &&
            e.SourcePortName == sourcePortName &&
            e.TargetNodeId == targetNodeId &&
            e.TargetPortName == targetPortName);

        if (exists) return null;

        // Prevent self-connections
        if (sourceNodeId == targetNodeId) return null;

        var edge = new FlowEdge
        {
            Id = Guid.NewGuid().ToString(),
            SourceNodeId = sourceNodeId,
            SourcePortName = sourcePortName,
            TargetNodeId = targetNodeId,
            TargetPortName = targetPortName
        };

        Edges.Add(edge);
        NotifyStateChanged();
        return edge;
    }

    public void RemoveEdge(string edgeId)
    {
        Edges.RemoveAll(e => e.Id == edgeId);
        NotifyStateChanged();
    }

    // ── Canvas operations ────────────────────────────────────

    public void Clear()
    {
        Nodes.Clear();
        Edges.Clear();
        SelectedNodeId = null;
        ZoomLevel = 1.0;
        PanX = 0;
        PanY = 0;
        OnCanvasCleared?.Invoke();
        NotifyStateChanged();
    }

    public Workflow GetWorkflow()
    {
        return new Workflow
        {
            Nodes = new List<FlowNode>(Nodes),
            Edges = new List<FlowEdge>(Edges)
        };
    }

    public void LoadWorkflow(Workflow workflow)
    {
        Nodes = new List<FlowNode>(workflow.Nodes);
        Edges = new List<FlowEdge>(workflow.Edges);
        SelectedNodeId = null;
        NotifyStateChanged();
    }

    public void SetZoom(double zoom)
    {
        ZoomLevel = Math.Clamp(zoom, 0.2, 3.0);
        NotifyStateChanged();
    }

    public void SetPan(double x, double y)
    {
        PanX = x;
        PanY = y;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnStateChanged?.Invoke();
}
