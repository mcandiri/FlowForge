namespace FlowForge.Core.Models;

public class NodePort
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public PortType Type { get; set; }
    public string NodeId { get; set; } = string.Empty;
}
