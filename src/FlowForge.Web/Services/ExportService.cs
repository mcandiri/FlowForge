using FlowForge.Core.Export;

namespace FlowForge.Web.Services;

/// <summary>
/// Handles workflow export by delegating to registered IWorkflowExporter implementations.
/// </summary>
public sealed class ExportService
{
    private readonly CanvasStateService _canvasState;
    private readonly IEnumerable<IWorkflowExporter> _exporters;

    public ExportService(CanvasStateService canvasState, IEnumerable<IWorkflowExporter> exporters)
    {
        _canvasState = canvasState;
        _exporters = exporters;
    }

    public string Export(string format)
    {
        var workflow = _canvasState.GetWorkflow();
        var exporter = _exporters.FirstOrDefault(e =>
            string.Equals(e.Format, format, StringComparison.OrdinalIgnoreCase));

        if (exporter is null)
        {
            // Fallback: serialize as JSON manually
            return System.Text.Json.JsonSerializer.Serialize(workflow,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        }

        return exporter.Export(workflow);
    }

    public IReadOnlyList<string> GetAvailableFormats()
    {
        return _exporters.Select(e => e.Format).ToList();
    }
}
