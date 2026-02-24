using FlowForge.Core.Templates;
using FluentAssertions;

namespace FlowForge.Core.Tests.Templates;

public class BuiltInTemplateTests
{
    private readonly BuiltInTemplateProvider _provider = new();

    [Fact]
    public void GetTemplates_Returns5Templates()
    {
        // Act
        var templates = _provider.GetTemplates();

        // Assert
        templates.Should().HaveCount(5);
    }

    [Fact]
    public void GetTemplates_AllHaveUniqueIds()
    {
        // Act
        var templates = _provider.GetTemplates();

        // Assert
        templates.Select(t => t.Id).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void GetTemplates_AllHaveNonEmptyNames()
    {
        // Act
        var templates = _provider.GetTemplates();

        // Assert
        templates.Should().AllSatisfy(t =>
        {
            t.Name.Should().NotBeNullOrWhiteSpace();
        });
    }

    [Fact]
    public void GetTemplates_AllHaveDescriptions()
    {
        // Act
        var templates = _provider.GetTemplates();

        // Assert
        templates.Should().AllSatisfy(t =>
        {
            t.Description.Should().NotBeNullOrWhiteSpace();
        });
    }

    [Fact]
    public void GetTemplates_AllHaveValidWorkflows()
    {
        // Act
        var templates = _provider.GetTemplates();

        // Assert
        templates.Should().AllSatisfy(t =>
        {
            t.Workflow.Should().NotBeNull();
            t.Workflow.Nodes.Should().NotBeEmpty();
            t.Workflow.Edges.Should().NotBeEmpty();
            t.Workflow.Metadata.Should().NotBeNull();
            t.Workflow.Metadata.Name.Should().NotBeNullOrWhiteSpace();
        });
    }

    [Fact]
    public void GetTemplates_AllNodesHaveValidPositions()
    {
        // Act
        var templates = _provider.GetTemplates();

        // Assert
        foreach (var template in templates)
        {
            foreach (var node in template.Workflow.Nodes)
            {
                node.X.Should().BeGreaterThanOrEqualTo(0, $"Node '{node.Name}' in template '{template.Name}' has invalid X position");
                node.Y.Should().BeGreaterThanOrEqualTo(0, $"Node '{node.Name}' in template '{template.Name}' has invalid Y position");
                node.X.Should().BeLessThanOrEqualTo(1200, $"Node '{node.Name}' in template '{template.Name}' has X too far right");
                node.Y.Should().BeLessThanOrEqualTo(800, $"Node '{node.Name}' in template '{template.Name}' has Y too far down");
            }
        }
    }

    [Fact]
    public void GetTemplates_AllEdgesReferenceExistingNodes()
    {
        // Act
        var templates = _provider.GetTemplates();

        // Assert
        foreach (var template in templates)
        {
            var nodeIds = template.Workflow.Nodes.Select(n => n.Id).ToHashSet();

            foreach (var edge in template.Workflow.Edges)
            {
                nodeIds.Should().Contain(edge.SourceNodeId,
                    $"Edge in template '{template.Name}' references nonexistent source node '{edge.SourceNodeId}'");
                nodeIds.Should().Contain(edge.TargetNodeId,
                    $"Edge in template '{template.Name}' references nonexistent target node '{edge.TargetNodeId}'");
            }
        }
    }

    [Fact]
    public void GetTemplates_AllNodesHaveTypeAndName()
    {
        // Act
        var templates = _provider.GetTemplates();

        // Assert
        foreach (var template in templates)
        {
            foreach (var node in template.Workflow.Nodes)
            {
                node.Type.Should().NotBeNullOrWhiteSpace(
                    $"Node '{node.Id}' in template '{template.Name}' has no type");
                node.Name.Should().NotBeNullOrWhiteSpace(
                    $"Node '{node.Id}' in template '{template.Name}' has no name");
            }
        }
    }

    [Fact]
    public void GetTemplate_ById_ReturnsCorrectTemplate()
    {
        // Act
        var template = _provider.GetTemplate("api-data-pipeline");

        // Assert
        template.Should().NotBeNull();
        template!.Name.Should().Be("API Data Pipeline");
    }

    [Fact]
    public void GetTemplate_UnknownId_ReturnsNull()
    {
        // Act
        var template = _provider.GetTemplate("nonexistent-template");

        // Assert
        template.Should().BeNull();
    }

    [Fact]
    public void GetTemplate_CaseInsensitive()
    {
        // Act
        var template = _provider.GetTemplate("API-DATA-PIPELINE");

        // Assert
        template.Should().NotBeNull();
    }

    // ─────── Individual template tests ───────

    [Fact]
    public void ApiDataPipeline_HasCorrectStructure()
    {
        var template = _provider.GetTemplate("api-data-pipeline")!;
        template.Workflow.Nodes.Should().HaveCount(4);
        template.Workflow.Edges.Should().HaveCount(3);

        // Should have: webhook -> http -> transform -> logger
        template.Workflow.Nodes.Should().Contain(n => n.Type == "webhook-trigger");
        template.Workflow.Nodes.Should().Contain(n => n.Type == "http-request");
        template.Workflow.Nodes.Should().Contain(n => n.Type == "transform");
        template.Workflow.Nodes.Should().Contain(n => n.Type == "logger");
    }

    [Fact]
    public void SmartNotification_HasConditionBranching()
    {
        var template = _provider.GetTemplate("smart-notification")!;
        template.Workflow.Nodes.Should().HaveCount(7);

        template.Workflow.Nodes.Should().Contain(n => n.Type == "condition");
        template.Workflow.Nodes.Should().Contain(n => n.Type == "email-sender");
        template.Workflow.Nodes.Should().Contain(n => n.Type == "delay");

        // Condition node should have true/false edges
        var conditionNode = template.Workflow.Nodes.First(n => n.Type == "condition");
        template.Workflow.Edges.Should().Contain(e =>
            e.SourceNodeId == conditionNode.Id && e.SourcePortName == "true");
        template.Workflow.Edges.Should().Contain(e =>
            e.SourceNodeId == conditionNode.Id && e.SourcePortName == "false");
    }

    [Fact]
    public void DataSyncLoop_HasLoopNode()
    {
        var template = _provider.GetTemplate("data-sync-loop")!;
        template.Workflow.Nodes.Should().HaveCount(7);

        template.Workflow.Nodes.Should().Contain(n => n.Type == "loop");

        var loopNode = template.Workflow.Nodes.First(n => n.Type == "loop");
        template.Workflow.Edges.Should().Contain(e =>
            e.SourceNodeId == loopNode.Id && e.SourcePortName == "iteration");
        template.Workflow.Edges.Should().Contain(e =>
            e.SourceNodeId == loopNode.Id && e.SourcePortName == "completed");
    }

    [Fact]
    public void ErrorHandlingDemo_HasCodeBlock()
    {
        var template = _provider.GetTemplate("error-handling-demo")!;
        template.Workflow.Nodes.Should().HaveCount(4);

        template.Workflow.Nodes.Should().Contain(n => n.Type == "code-block");

        var codeBlockNode = template.Workflow.Nodes.First(n => n.Type == "code-block");
        template.Workflow.Edges.Should().Contain(e =>
            e.SourceNodeId == codeBlockNode.Id && e.SourcePortName == "output");
        template.Workflow.Edges.Should().Contain(e =>
            e.SourceNodeId == codeBlockNode.Id && e.SourcePortName == "error");
    }

    [Fact]
    public void MultiStepApproval_HasApprovalFlow()
    {
        var template = _provider.GetTemplate("multi-step-approval")!;
        template.Workflow.Nodes.Should().HaveCount(5);

        template.Workflow.Nodes.Should().Contain(n => n.Type == "condition");
        template.Workflow.Nodes.Should().Contain(n => n.Type == "email-sender");
        template.Workflow.Nodes.Count(n => n.Type == "logger").Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void AllTemplates_StartWithWebhookTrigger()
    {
        var templates = _provider.GetTemplates();

        foreach (var template in templates)
        {
            // The first node (which has no incoming edges) should be a webhook trigger
            var nodeIds = template.Workflow.Nodes.Select(n => n.Id).ToHashSet();
            var targetIds = template.Workflow.Edges.Select(e => e.TargetNodeId).ToHashSet();
            var startNodes = template.Workflow.Nodes.Where(n => !targetIds.Contains(n.Id)).ToList();

            startNodes.Should().ContainSingle(n => n.Type == "webhook-trigger",
                $"Template '{template.Name}' should start with a WebhookTrigger node");
        }
    }
}
