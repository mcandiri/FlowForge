namespace FlowForge.Core.Templates;

public interface ITemplateProvider
{
    IReadOnlyList<WorkflowTemplate> GetTemplates();
    WorkflowTemplate? GetTemplate(string id);
}
