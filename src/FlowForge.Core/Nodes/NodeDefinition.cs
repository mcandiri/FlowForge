using FlowForge.Core.Models;

namespace FlowForge.Core.Nodes;

public class NodeDefinition
{
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public NodeCategory Category { get; set; }
    public List<PortDefinition> InputPorts { get; set; } = new();
    public List<PortDefinition> OutputPorts { get; set; } = new();
    public List<ConfigField> ConfigFields { get; set; } = new();
}

public class PortDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}

public class ConfigField
{
    public string Name { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Type { get; set; } = "string";
    public object? DefaultValue { get; set; }
    public List<string> Options { get; set; } = new();
    public bool Required { get; set; }
}
