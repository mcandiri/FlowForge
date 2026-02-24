namespace FlowForge.Core.Models;

public class FlowNode
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public NodeConfig Config { get; set; } = new();
    public List<NodePort> InputPorts { get; set; } = new();
    public List<NodePort> OutputPorts { get; set; } = new();
}
