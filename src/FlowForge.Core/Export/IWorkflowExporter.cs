using FlowForge.Core.Models;

namespace FlowForge.Core.Export;

public interface IWorkflowExporter
{
    string Format { get; }
    string Export(Workflow workflow);
}
