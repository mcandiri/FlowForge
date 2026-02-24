using FlowForge.Core.Models;

namespace FlowForge.Core.Engine;

public interface IWorkflowEngine
{
    Task<WorkflowExecutionResult> ExecuteAsync(
        Workflow workflow,
        ExecutionContext context,
        IProgress<ExecutionTrace>? progress = null,
        CancellationToken ct = default);
}
