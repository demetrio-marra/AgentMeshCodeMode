
# GitHub Copilot Instructions for AgentMesh

This file provides guidance for GitHub Copilot when working in this repository. It is organized by topic and should be extended over time as new conventions and patterns are established.

---

## Table of Contents

1. [Creating New Agents](#1-creating-new-agents)
2. [Updating existing Agents](#2-updating-existing-agents)
3. [Removing legacy Agents](#3-deleting-legacy-agents)

---

## 1. Creating New Agents

Every agent in AgentMesh follows a consistent, layered architecture spread across two projects:

- **`AgentMesh`** — the contracts/models project (interfaces, input/output DTOs)
- **`AgentMesh.Application`** — the application project (configurations, implementations)

Registration of each agent is done manually in **`AgentMeshCLI/Program.cs`**.

### 1.1 Layer Overview

| Layer | Project | What to create |
|---|---|---|
| Input DTO | `AgentMesh` | `Models/<AgentName>/<AgentName>AgentInput.cs` |
| Output DTO | `AgentMesh` | `Models/<AgentName>/<AgentName>AgentOutput.cs` |
| Interface | `AgentMesh` | `Services/I<AgentName>Agent.cs` |
| Configuration | `AgentMesh.Application` | `Configuration/<AgentName>AgentConfiguration.cs` |
| Implementation | `AgentMesh.Application` | `Services/<AgentName>Agent.cs` |
| System prompt | `AgentMeshCLI` | `Prompts/<AgentName>.SystemPrompt.txt` |
| `appsettings.json` entry | `AgentMeshCLI` | Add section under `Agents` |
| DI registration | `AgentMeshCLI` | `Program.cs` |

---

### 1.2 Step-by-Step Guide

#### Step 1 — Define the Input DTO (`AgentMesh` project)

Create `AgentMesh/Models/<AgentName>/<AgentName>AgentInput.cs`.

The input DTO is a plain class with no base type. Model its properties around what the agent needs to run:

```csharp
namespace AgentMesh.Models.<AgentName>
{
    public class <AgentName>AgentInput
    {
        public string SomeProperty { get; set; } = string.Empty;
    }
}
```

#### Step 2 — Define the Output DTO (`AgentMesh` project)

Create `AgentMesh/Models/<AgentName>/<AgentName>AgentOutput.cs`.

All output DTOs must implement `IAgentOutput`, which requires the three token-count properties:

```csharp
namespace AgentMesh.Models.<AgentName>
{
    public class <AgentName>AgentOutput : IAgentOutput
    {
        public string SomeResult { get; set; } = string.Empty;
        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }
    }
}
```

#### Step 3 — Define the Interface (`AgentMesh` project)

Create `AgentMesh/Services/I<AgentName>Agent.cs`.

Every agent interface extends `IExecutor<TInput, TOutput>` and is intentionally left empty — it serves only as a named contract for DI:

```csharp
using AgentMesh.Models.<AgentName>;

namespace AgentMesh.Services
{
    public interface I<AgentName>Agent : IExecutor<<AgentName>AgentInput, <AgentName>AgentOutput>
    {
    }
}
```

#### Step 4 — Define the Configuration (`AgentMesh.Application` project)

Create `AgentMesh.Application/Configuration/<AgentName>AgentConfiguration.cs`.

The configuration class holds the agent's DI key, its `appsettings.json` section name, and the LLM binding properties. All agents must expose at minimum `LLM`, `ModelTemperature`, `SystemPrompt`, and `SystemPromptFile`. Add extra properties only when the agent requires them (e.g., `AllowedRecipients` in `RouterAgentConfiguration`):

```csharp
namespace AgentMesh.Application.Configuration
{
    public class <AgentName>AgentConfiguration
    {
        public const string SectionName = "Agents:<AgentName>";
        public const string AgentName = "<AgentName>";

        public string LLM { get; set; } = string.Empty;
        public string ModelTemperature { get; set; } = string.Empty;
        public string SystemPrompt { get; set; } = string.Empty;
        public string? SystemPromptFile { get; set; }
    }
}
```

#### Step 5 — Implement the Agent (`AgentMesh.Application` project)

Create `AgentMesh.Application/Services/<AgentName>Agent.cs`.

- Inherit from `AgentBase<TParsed>`, where `TParsed` is the intermediate parsed type returned by `ParseStructuredResponse` (commonly `string`, but can be a tuple or a custom type for structured outputs).
- Implement `I<AgentName>Agent`.
- Resolve the keyed `IOpenAIClient` using `[FromKeyedServices(<AgentName>AgentConfiguration.AgentName)]`.
- Build the `List<AgentMessage>` inside `ExecuteAsync`, then call `ExecuteWithRetryAsync`.
- Map the `AgentResponse<TParsed>` result onto the output DTO, including all token counts.
- Implement `ParseStructuredResponse` to extract the structured data from the raw LLM text. Throw `BadStructuredResponseException` if the format is invalid — this triggers automatic retry via the `Resilience` policy.

```csharp
using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Models;
using AgentMesh.Models.<AgentName>;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AgentMesh.Application.Services
{
    public class <AgentName>Agent : AgentBase<string>, I<AgentName>Agent
    {
        private readonly ILogger<<AgentName>Agent> _logger;

        public <AgentName>Agent(
            [FromKeyedServices(<AgentName>AgentConfiguration.AgentName)] IOpenAIClient openAIClient,
            <AgentName>AgentConfiguration configuration,
            ILogger<<AgentName>Agent> logger) : base(logger, <AgentName>AgentConfiguration.AgentName, openAIClient)
        {
            _logger = logger;
        }

        public async Task<<AgentName>AgentOutput> ExecuteAsync(
            <AgentName>AgentInput input,
            CancellationToken cancellationToken = default)
        {
            var inputMessages = new List<AgentMessage>
            {
                new AgentMessage { Role = AgentMessageRole.System, Content = $"Today date is {DateTime.UtcNow:yyyy-MM-dd}." },
                new AgentMessage { Role = AgentMessageRole.User, Content = input.SomeProperty }
            };

            var result = await ExecuteWithRetryAsync(inputMessages, cancellationToken);

            return new <AgentName>AgentOutput
            {
                SomeResult = result.Result,
                TokenCount = result.TotalTokenCount,
                InputTokenCount = result.InputTokenCount,
                OutputTokenCount = result.OutputTokenCount
            };
        }

        protected override string ParseStructuredResponse(string rawResponseText)
        {
            // For plain-text responses, return as-is:
            return rawResponseText;

            // For structured responses, parse and throw BadStructuredResponseException on failure
            // to trigger the retry policy in Resilience.
        }
    }
}
```

#### Step 6 — Create the System Prompt (`AgentMeshCLI` project)

Create `AgentMeshCLI/Prompts/<AgentName>.SystemPrompt.txt` with the agent's system prompt text.

Set **Copy to Output Directory** to `Copy if newer` in the file's properties (or add the following to `AgentMeshCLI.csproj`):

```xml
<None Update="Prompts\<AgentName>.SystemPrompt.txt">
  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</None>
```

#### Step 7 — Add `appsettings.json` Configuration (`AgentMeshCLI` project)

Add a new entry under the `Agents` section in `AgentMeshCLI/appsettings.json`:

```json
"Agents": {
  "<AgentName>": {
    "LLM": "<llm-key-from-LLMs-section>",
    "ModelTemperature": "0.7",
    "SystemPromptFile": "Prompts/<AgentName>.SystemPrompt.txt"
  }
}
```

The `LLM` value must match a key defined in the top-level `LLMs` configuration dictionary.

#### Step 8 — Register in the DI Container (`AgentMeshCLI/Program.cs`)

Add the following registration block to `Program.cs`, following the same pattern used by all existing agents:

```csharp
// <AgentName> agent config and client
services
    .AddOptions<<AgentName>AgentConfiguration>()
    .Bind(configuration.GetSection(<AgentName>AgentConfiguration.SectionName))
    .PostConfigure(options =>
    {
        options.SystemPrompt = ResolveConfigText(options.SystemPrompt, options.SystemPromptFile);
    })
    .Services
    .AddSingleton(sp => sp.GetRequiredService<IOptions<<AgentName>AgentConfiguration>>().Value);

services.AddKeyedSingleton<IOpenAIClient>(<AgentName>AgentConfiguration.AgentName, (sp, _) =>
{
    var factory = sp.GetRequiredService<IOpenAIClientFactory>();
    var config = sp.GetRequiredService<<AgentName>AgentConfiguration>();
    var llmsConfig = sp.GetRequiredService<LLMsConfiguration>();
    var llmConfig = ResolveLLMConfiguration(config.LLM, llmsConfig);
    var systemPrompt = config.SystemPrompt;
    return factory.CreateOpenAIClient(llmConfig.Model, llmConfig.Provider, config.ModelTemperature, systemPrompt);
});

services.AddSingleton<I<AgentName>Agent, <AgentName>Agent>();
```

---

### 1.3 Key Conventions

- **Agent name constant** — `AgentName` in the configuration class is used as the DI key for the keyed `IOpenAIClient` and must be unique across all agents.
- **Keyed `IOpenAIClient`** — Each agent gets its own `IOpenAIClient` instance registered as a keyed singleton, pre-configured with the agent's LLM, provider, temperature, and system prompt. Always resolve it with `[FromKeyedServices(...)]` in the constructor.
- **Retry on parse failure** — Throw `BadStructuredResponseException` (or `EmptyAgentResponseException`) from `ParseStructuredResponse` to trigger the automatic retry policy defined in `Resilience`. Return `default(T)` only when the response is valid but intentionally empty.
- **Token counts** — Always propagate `TotalTokenCount`, `InputTokenCount`, and `OutputTokenCount` from `AgentResponse<T>` to the output DTO.
- **`SystemPromptFile` vs `SystemPrompt`** — Prefer `SystemPromptFile` in `appsettings.json` to keep prompts in dedicated `.txt` files. The `ResolveConfigText` helper in `Program.cs` resolves the file path at startup and populates `SystemPrompt` automatically.
- **Project placement** — Contracts and models belong in `AgentMesh`; all application logic and configuration belong in `AgentMesh.Application`. Do not add implementation details to the `AgentMesh` contracts project.
- **Workflow wiring** — Adding a new agent to an existing or new `IWorkflow` implementation is a separate concern. Inject the agent's interface via the workflow constructor and call `ExecuteAsync` as needed within the workflow logic.


## 2. Updating existing Agents
This section details the workflows on updating existing agents capabilities

### Adding/Removing or Changing an agent's feature
When the user asks for changes agent features, usually it means to update its input/output along the system prompt. Follow the script below.
- Update the system prompt to instruct the model to consider new properties/reconsider existing ones
- Change the DTOs to reflect new properties
- Update the Agent's class file, how input is sent and how response is parsed
- Update the Agent's executor in the `CodeModeWorkflow.cs` file to trace properties in `notifyDictionary`
- Update the `CodeModeWorkflowState.cs` properties involved in the change.

### Changes of name only
When the user asks to refactor the name of the agent keeping features unchanged, it is just a refactor/rephrase work. Do as follows:
- Rename any file/folder/class/enum and any kind of c# entity related.
- Update all references with the new names
- Update the system prompt to reflect the new name
- Update any text within  `CodeModeWorkflow.cs` and also trace properties in `notifyDictionary`
- Update the agent's configuration printed at program startup


## 3. Removing legacy Agents

When the user asks to remove a no more useful or superseed agent, follow this workflow:
1. Remove it from `CodeModeWorkflow.cs` first
2. Remove related-only properties from `CodeModeWorkflowState.cs`
3. Delete its configuration in `appSettings.json`
4. Remove configuration binding from `Program.cs` as well as Dependency Injection
5. Delete the .cs file
6. Delete related DTOs and Executors
7. Ensure all folders belonging to it are deleted across the entire solution
8. Delete the `<agentName>.SystemPrompt.txt` file 
