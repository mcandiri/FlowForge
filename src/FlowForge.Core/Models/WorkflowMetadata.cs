namespace FlowForge.Core.Models;

public class WorkflowMetadata
{
    public string Name { get; set; } = "Untitled Workflow";
    public string Description { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    public string Version { get; set; } = "1.0.0";
}
