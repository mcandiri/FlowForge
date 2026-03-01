# FlowForge

> Design, execute, and export workflows visually -- a drag-and-drop pipeline builder for .NET.

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![CI](https://github.com/mcandiri/FlowForge/actions/workflows/ci.yml/badge.svg)](https://github.com/mcandiri/FlowForge/actions/workflows/ci.yml)
[![Tests](https://img.shields.io/badge/tests-118%20passing-brightgreen)]()
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

---

## Demo

```bash
git clone https://github.com/mcandiri/FlowForge.git
cd FlowForge
dotnet run --project src/FlowForge.Web
```

Open [http://localhost:5000](http://localhost:5000) in your browser.

Drag nodes onto the canvas, wire them together, hit **Run**, and watch the execution trace light up in real time.

---

## Features

- **Visual Designer** -- Drag-and-drop canvas with snap-to-grid, auto-layout, and live connection drawing.
- **10+ Node Types** -- HTTP requests, conditions, loops, delays, code blocks, email, database queries, and more.
- **Real-Time Execution** -- Execute workflows directly in the browser and watch each node highlight as it runs.
- **C# Code Export** -- Generate clean, copy-pasteable C# code from any workflow.
- **JSON Import / Export** -- Save, share, and version-control your workflows as JSON files.
- **Demo Templates** -- Five ready-made templates to explore patterns like data pipelines, notifications, loops, error handling, and approval flows.

---

## Limitations

- **Demo mode only** — HTTP requests, emails, and database queries are mocked. FlowForge demonstrates the visual workflow paradigm, not production integrations.

- **No persistence** — workflows live in memory during the session. JSON export/import is the save mechanism.

- **No authentication** — this is a single-user tool, not a multi-tenant platform.

- **Code Block node uses Roslyn scripting** — powerful but adds cold-start latency on first execution.

---

## Node Types

| Icon | Node | Category | Description |
|------|------|----------|-------------|
| :bell: | **Webhook Trigger** | Trigger | Entry point -- starts a workflow from an HTTP webhook |
| :globe_with_meridians: | **HTTP Request** | Action | GET / POST / PUT / DELETE with mock-mode support |
| :pencil: | **Logger** | Action | Log messages with variable interpolation |
| :email: | **Email Sender** | Action | Send email notifications (demo mode logs instead) |
| :computer: | **Code Block** | Action | Execute arbitrary C# expressions at runtime |
| :package: | **Set Variable** | Action | Store a value in the workflow context |
| :file_cabinet: | **Database Query** | Action | Run SQL queries (mock result sets in demo mode) |
| :twisted_rightwards_arrows: | **Condition** | Control | If / else branching on expressions |
| :alarm_clock: | **Delay** | Control | Pause execution for N milliseconds |
| :repeat: | **Loop** | Control | Iterate over a collection variable |
| :arrows_counterclockwise: | **Retry** | Control | Retry a failed step up to N times |
| :arrows_clockwise: | **Transform** | Data | Map and reshape data with variable templates |

---

## Architecture

```
FlowForge.sln
  src/
    FlowForge.Core/          Core engine, nodes, models, export, templates
      Engine/                 WorkflowEngine, ExecutionContext, NodeExecutor
      Models/                 Workflow, FlowNode, FlowEdge, NodeConfig, NodePort
      Nodes/                  IFlowNode interface + all built-in node implementations
        Actions/              HttpRequest, Logger, EmailSender, CodeBlock, SetVariable, DatabaseQuery
        Control/              Condition, Delay, Loop, Retry
        Data/                 Transform
        Triggers/             WebhookTrigger
      Export/                 JsonExporter, CSharpCodeGenerator
      Templates/              ITemplateProvider, BuiltInTemplateProvider
      Extensions/             ServiceCollectionExtensions (DI registration)
    FlowForge.Web/            Blazor Server front-end
  tests/
    FlowForge.Core.Tests/     xUnit + FluentAssertions + Moq
    FlowForge.Web.Tests/
```

**How it works:**

1. The **Visual Designer** (Blazor) lets you place `FlowNode` objects on a canvas and connect them via `FlowEdge` objects.
2. Pressing **Run** sends the `Workflow` graph to the `WorkflowEngine`.
3. The engine walks the graph topologically, starting from nodes with no incoming edges.
4. Each node is resolved via `NodeRegistry` and executed through `IFlowNode.ExecuteAsync`.
5. Condition nodes route to `true` or `false` ports; loop nodes iterate and re-visit body nodes.
6. Execution traces stream back to the UI via `IProgress<ExecutionTrace>` for real-time feedback.

---

## Born From Production

> Started as an internal tool to let non-developers define simple data pipelines without filing a ticket for every new integration. The visual canvas made it easier to discuss workflow logic in meetings than reading through nested if-else chains in code.

---

## What FlowForge Is NOT

| | FlowForge | Enterprise BPM | No-Code Platforms |
|---|---|---|---|
| **Target** | .NET developers & small teams | Large enterprises | Non-technical users |
| **Deployment** | Self-hosted, single binary | Cloud / on-prem clusters | SaaS only |
| **Customisation** | Add C# nodes by implementing `IFlowNode` | XML/BPMN config files | Limited plugins |
| **Cost** | Free & open source | $$$$ per seat | $$$ per month |
| **Overhead** | No external dependencies beyond .NET runtime | Message brokers, databases | Vendor lock-in |

---

## Roadmap

- [x] Visual drag-and-drop designer
- [x] 10+ built-in node types
- [x] Real-time execution with trace overlay
- [x] JSON import / export
- [x] C# code generation
- [x] Demo templates
- [ ] Persistent workflow storage (SQLite / PostgreSQL)
- [ ] Authentication & role-based access
- [ ] Webhook listener for live triggers
- [ ] Custom node plugin system
- [ ] Workflow versioning & diff
- [ ] Parallel branch execution visualisation
- [ ] Debugging mode with breakpoints
- [ ] Real database & email integration

---

## Running Tests

```bash
dotnet test
```

---

## License

[MIT](LICENSE) -- Copyright (c) 2026 Mehmet Can Diri
