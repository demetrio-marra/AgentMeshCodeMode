# AgentMesh Code Mode

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-12.0-239120?logo=csharp&logoColor=white)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![License](https://img.shields.io/github/license/demetrio-marra/AgentMeshCodeMode)](LICENSE)
[![GitHub issues](https://img.shields.io/github/issues/demetrio-marra/AgentMeshCodeMode)](https://github.com/demetrio-marra/AgentMeshCodeMode/issues)
[![GitHub stars](https://img.shields.io/github/stars/demetrio-marra/AgentMeshCodeMode)](https://github.com/demetrio-marra/AgentMeshCodeMode/stargazers)
[![OpenAI](https://img.shields.io/badge/OpenAI_Compatible-API-412991?logo=openai&logoColor=white)](https://platform.openai.com/)
[![Qdrant](https://img.shields.io/badge/Qdrant-Semantic_Search-DC244C)](https://qdrant.tech/)

A multi-agent AI workflow that leverages **dynamic JavaScript code generation** against a set of predefined company APIs. A mesh of specialized small agents collaborate to understand user intent, generate executable code, validate it, run it in a sandboxed environment, and present the results — all orchestrated through a configurable pipeline.

---

## :sparkles: Features

- **Multi-agent orchestrated workflow** — specialized agents collaborate in a pipeline: routing, context analysis, business requirements extraction, code generation, static analysis, code fixing, execution, failure detection, and result presentation.
- **Dynamic code generation** — the Coder agent generates JavaScript code targeting your company's predefined API surface.
- **Sandboxed code execution** — generated code runs in an isolated JavaScript sandbox ([JSCodeSandbox](https://github.com/demetrio-marra/JSCodeSandbox)), deployed separately for security and isolation.
- **Self-healing code pipeline** — static analysis and runtime failure detection agents feed back into a Code Fixer agent, iterating automatically to resolve issues.
- **Semantic search with Qdrant** — retrieves contextual facts from a vector database (business processes, domain knowledge) relevant to the user's actionable requirements.
- **Conversation summarization** — a dedicated agent summarizes growing conversation history to keep context manageable within token limits.
- **Intelligent routing** — a Router agent classifies user intent and dispatches to the appropriate workflow branch (code generation, business advisory, or personal assistant).
- **Multi-provider LLM support** — configure different LLM providers (HuggingFace, Together, Fireworks AI, or any OpenAI-compatible endpoint) per agent.
- **Per-agent configuration** — each agent has its own LLM model, temperature, and system prompt, all configurable via `appsettings.json`.
- **Token usage tracking** — tracks input/output token consumption per agent for cost monitoring.

## :building_construction: Architecture

```
+--------------------------------------------------------------------+
|                            User (CLI)                              |
+--------------------------------------------------------------------+
                 |
                 v
+--------------------------+
|   Context Analyzer Agent |----> Extracts relevant context & actionable
+--------------------------+      requirements from conversation history
             |
             v
+--------------------------+
|        Router Agent      |----> Classifies intent -> routes to branch
+--------------------------+
   |             |       |
   v             v       v
+--------+   +---------+  +------------------------+
|Personal|   |Business |  |   Code Generation      |
|Assistant   |Advisor  |  |   Pipeline             |
+--------+   +---------+  |                        |
                          |  +------------------+  |
                          |  |Biz Requirements  |  |
                          |  |Creator + Qdrant  |  |
                          |  +------------------+  |
                          |          |             |
                          |  +------------------+  |
                          |  |   Coder Agent    |  |
                          |  +------------------+  |
                          |          |             |
                          |  +------------------+  |----> Code Fixer
                          |  | Static Analyzer  |  |      (iterative)
                          |  +------------------+  |
                          |          |             |
                          |  +------------------+  |----> Failure Detector
                          |  | JS Sandbox Exec  |  |      + Code Fixer
                          |  +------------------+  |      (iterative)
                          |          |             |
                          |  +------------------+  |
                          |  |Results Presenter |  |
                          |  +------------------+  |
                          +------------------------+
```

### Conversation Summarizer

A dedicated **Conversation Summarizer** agent runs in the background to compress conversation history when it exceeds a configurable token threshold, preserving the most recent messages while summarizing older context.

## :file_folder: Project Structure

| Project | Description |
|---|---|
| `AgentMesh` | Core domain — agent interfaces, models, and contracts |
| `AgentMesh.Application` | Agent implementations, workflow orchestration, configuration models |
| `AgentMeshCLI` | Console application entry point and DI composition root |
| `AgentMesh.Infrastructure.OpenAIClient` | OpenAI-compatible API client with multi-provider support |
| `AgentMesh.Infrastructure.JSSandbox` | Client for the external [JSCodeSandbox](https://github.com/demetrio-marra/JSCodeSandbox) service |
| `AgentMesh.Infrastructure.SemanticSearch` | Qdrant vector database integration for semantic search |

## :rocket: Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- A running [Qdrant](https://qdrant.tech/) instance for semantic search
- A deployed [JSCodeSandbox](https://github.com/demetrio-marra/JSCodeSandbox) instance for sandboxed code execution
- API keys for your chosen LLM provider(s) (HuggingFace, Together, Fireworks AI, etc.)

### Setup

1. **Clone the repository**

   ```bash
   git clone https://github.com/demetrio-marra/AgentMeshCodeMode.git
   cd AgentMeshCodeMode
   ```

2. **Deploy the JS Code Sandbox** (separate service)

   Follow the instructions at [JSCodeSandbox](https://github.com/demetrio-marra/JSCodeSandbox) to deploy the sandbox service. Update the `SESJSSandbox` section in `appsettings.json` with your sandbox URL.

3. **Configure `appsettings.json`**

   Edit `AgentMeshCLI/appsettings.json` to set:
   - **LLM providers** — endpoints and API keys (via environment variables) under `InferenceProviders`
   - **LLMs** — model names and providers for each tier under `LLMs`
   - **Agent settings** — per-agent LLM assignment, temperature, and system prompt files under `Agents`
   - **Qdrant** — host, port, and collection name under `QDrantSemanticSearchService`
   - **Embedding** — model endpoint and name under `Embedding`
   - **Sandbox** — URL and sandbox name under `SESJSSandbox`

4. **Set environment variables** for API keys as required by your LLM providers.

5. **Build and run**

   ```bash
   dotnet build
   cd AgentMeshCLI
   dotnet run
   ```

### Configuration Overview

The system is configured entirely through `appsettings.json`. Key sections:

```jsonc
{
  "InferenceProviders": {
    "HuggingFace": { "Endpoint": "https://router.huggingface.co/v1" },
    "Together":    { "Endpoint": "https://api.together.xyz/v1/" },
    "FireworksAI": { "Endpoint": "https://api.fireworks.ai/inference/v1/" }
  },
  "LLMs": {
    "CoderLLM":    { "Model": "...", "Provider": "HuggingFace" },
    "20B-Class-LLM": { "Model": "...", "Provider": "HuggingFace" }
    // ...
  },
  "Agents": {
    "Coder": {
      "LLM": "CoderLLM",
      "ModelTemperature": "0.6",
      "SystemPromptFile": "Prompts/Coder.SystemPrompt.txt",
      "ApiReferenceFile": "Prompts/MyPlatform_Statistics/ApiReference.txt"
    }
    // ... other agents
  }
}
```

Each agent can use a different LLM tier, allowing cost optimization by assigning cheaper/smaller models to simpler tasks and more capable models to complex reasoning.

## :robot: Agents

| Agent | Role |
|---|---|
| **Context Analyzer** | Extracts relevant context and actionable requirements from conversation history |
| **Router** | Classifies user intent and dispatches to the appropriate workflow branch |
| **Business Requirements Creator** | Transforms user requests into structured business requirements; queries Qdrant for related domain facts |
| **Business Advisor** | Answers business-related questions using domain knowledge |
| **Coder** | Generates JavaScript code against predefined API references |
| **Code Static Analyzer** | Validates generated code for correctness and code smells |
| **Code Fixer** | Repairs code based on static analysis or runtime error feedback |
| **Code Execution Failures Detector** | Analyzes sandbox execution results for runtime failures |
| **Results Presenter** | Formats and presents execution results to the user |
| **Personal Assistant** | Handles general conversational queries |
| **Conversation Summarizer** | Compresses conversation history to stay within token limits |

## :link: External Dependencies

| Service | Purpose | Repository |
|---|---|---|
| **JSCodeSandbox** | Sandboxed JavaScript execution environment | [github.com/demetrio-marra/JSCodeSandbox](https://github.com/demetrio-marra/JSCodeSandbox) |
| **Qdrant** | Vector database for semantic search | [qdrant.tech](https://qdrant.tech/) |

## :gear: Tech Stack

- **.NET 8** / **C# 12**
- **Microsoft.Extensions** (DI, Configuration, Logging, Options, HttpClient)
- **OpenAI SDK** (OpenAI-compatible API client)
- **Qdrant.Client** for vector search
- **Polly** for resilience and retry policies

## :page_facing_up: License

See [LICENSE](LICENSE) for details.
