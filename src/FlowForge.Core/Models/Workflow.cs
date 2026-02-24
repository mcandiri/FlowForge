namespace FlowForge.Core.Models;

public class Workflow
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public WorkflowMetadata Metadata { get; set; } = new();
    public List<FlowNode> Nodes { get; set; } = new();
    public List<FlowEdge> Edges { get; set; } = new();
}
