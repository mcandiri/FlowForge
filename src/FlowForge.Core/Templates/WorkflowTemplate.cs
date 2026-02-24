using FlowForge.Core.Models;

namespace FlowForge.Core.Templates;

public class WorkflowTemplate
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public Workflow Workflow { get; set; } = new();
}
