using FlowForge.Core.Engine;
using FlowForge.Core.Export;
using FlowForge.Core.Nodes;
using FlowForge.Core.Nodes.Actions;
using FlowForge.Core.Nodes.Control;
using FlowForge.Core.Nodes.Data;
using FlowForge.Core.Nodes.Triggers;
using FlowForge.Core.Templates;
using Microsoft.Extensions.DependencyInjection;

namespace FlowForge.Core.Extensions;

/// <summary>
/// Extension methods for registering all FlowForge Core services
/// with the Microsoft dependency-injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all FlowForge Core services: NodeRegistry (singleton, pre-populated),
    /// WorkflowEngine (scoped), BuiltInTemplateProvider (singleton), and all exporters.
    /// </summary>
    public static IServiceCollection AddFlowForgeCore(this IServiceCollection services)
    {
        // NodeRegistry: singleton, populated with all built-in node types
        services.AddSingleton<NodeRegistry>(sp =>
        {
            var registry = new NodeRegistry();

            // Action nodes
            registry.Register(new HttpRequestNode());
            registry.Register(new LoggerNode());
            registry.Register(new EmailSenderNode());
            registry.Register(new CodeBlockNode());
            registry.Register(new SetVariableNode());
            registry.Register(new DatabaseQueryNode());

            // Control nodes
            registry.Register(new ConditionNode());
            registry.Register(new DelayNode());
            registry.Register(new LoopNode());
            registry.Register(new RetryNode());

            // Data nodes
            registry.Register(new TransformNode());

            // Trigger nodes
            registry.Register(new WebhookTriggerNode());

            return registry;
        });

        // Workflow engine: scoped (one per request/scope)
        services.AddScoped<IWorkflowEngine, WorkflowEngine>();

        // Template provider: singleton
        services.AddSingleton<ITemplateProvider, BuiltInTemplateProvider>();

        // Exporters: JsonExporter is stateless so singleton is fine.
        // CSharpCodeGenerator uses instance state during Export(), so transient
        // avoids thread-safety issues when multiple scopes export concurrently.
        services.AddSingleton<IWorkflowExporter, JsonExporter>();
        services.AddTransient<IWorkflowExporter, CSharpCodeGenerator>();

        return services;
    }
}
