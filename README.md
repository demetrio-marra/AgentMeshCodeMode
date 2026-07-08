# AgentMesh Code Mode

[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-12.0-239120?logo=csharp&logoColor=white)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![OpenAI](https://img.shields.io/badge/OpenAI_Compatible-API-412991?logo=openai&logoColor=white)](https://platform.openai.com/)
[![Qdrant](https://img.shields.io/badge/Qdrant-Semantic_Search-DC244C)](https://qdrant.tech/)
[![GitHub stars](https://img.shields.io/github/stars/demetrio-marra/AgentMeshCodeMode)](https://github.com/demetrio-marra/AgentMeshCodeMode/stargazers)

A multi-agent AI workflow that leverages **dynamic JavaScript code generation** against a set of predefined company APIs. A mesh of specialized small agents collaborate to understand user intent, generate executable code, validate it, run it in a sandboxed environment, and present the results — all orchestrated through a configurable pipeline.

---

## :sparkles: Features

- **Multi-agent orchestrated workflow** – specialized agents collaborate in a pipeline: intent extraction, canonicalization, requirements collection, functional analysis, technical analysis, code generation, code fixing, execution, failure detection, and result presentation.
- **Dynamic code generation** – the Coder agent generates JavaScript code targeting your company's predefined API surface.
- **Sandboxed code execution** – generated code runs in an isolated JavaScript sandbox ([JSCodeSandbox](https://github.com/demetrio-marra/JSCodeSandbox)), deployed separately for security and isolation.
- **Self-healing code pipeline** – static analysis and runtime failure detection agents feed back into a Code Fixer agent, iterating automatically to resolve issues.
- **Semantic search with Qdrant** – retrieves contextual facts from a vector database (business processes, domain knowledge) relevant to the user's actionable requirements.
- **Conversation summarization** – a dedicated agent summarizes growing conversation history to keep context manageable within token limits.
- **Agent memory system** – leverages Mem0 for persistent, context-aware agent memory across conversations.
- **Knowledge base integration** – integrates with QMD for accessing knowledge bases and documentation.
- **Multi-provider LLM support** – configure different LLM providers (HuggingFace, Together, Fireworks AI, or any OpenAI-compatible endpoint) per agent.
- **Per-agent configuration** – each agent has its own LLM model, temperature, and system prompt, all configurable via `appsettings.json`.
- **Token usage tracking** – tracks input/output token consumption per agent for cost monitoring.

## :building_construction: Architecture

```
+--------------------------------------------------------------------+
|                            User (CLI)                              |
+--------------------------------------------------------------------+
                 |
                 v
+--------------------------+
|   Intent Extractor Agent |----> Extracts user intent from input
+--------------------------+
             |
             v
+--------------------------+
|  Intent Canonicalization |----> Normalizes intent format
|        Agent             |
+--------------------------+
             |
             v
+--------------------------+
| Requirements Collector   |----> Transforms intent into structured
|        Agent             |      requirements; queries QDrant/QMD
+--------------------------+
             |
             v
+--------------------------+
| Functional Analyst Agent |----> Analyzes business requirements
+--------------------------+
             |
             v
+--------------------------+
| Technical Analyst Agent  |----> Analyzes technical feasibility
+--------------------------+
             |
             v
+--------------------------+
| Relevant Facts Evaluator |----> Filters and ranks relevant facts
|        Agent             |      from knowledge base
+--------------------------+
             |
             v
+--------------------------+
|    Coder Agent           |----> Generates JavaScript code against
+--------------------------+      predefined API references
             |
             v
+--------------------------+
| Code Fixer Agent         |<---- Iterates on static/runtime errors
| (with retry loop)        |
+--------------------------+
             |
             v
+--------------------------+
|   JS Sandbox Executor    |----> Executes code in isolated environment
+--------------------------+
             |
             v
+--------------------------+
| Code Execution Failures  |----> Detects and analyzes runtime failures
|  Detector Agent          |
+--------------------------+
             |
             v
+--------------------------+
| Documentation Agent      |----> Generates human-readable output
|   & Results Presenter    |
+--------------------------+

### Side-by-side Services

- **Conversation Summarizer Agent** – runs periodically to compress conversation history
- **Personal Assistant Agent** – handles general conversational queries
- **Domain Expert Agent** – provides domain-specific guidance and context
- **Agent Memory Executor** – manages persistent agent memory via Mem0
```

## :file_folder: Project Structure

| Project | Description |
|---|---|
| `AgentMesh` | Core domain – agent interfaces, models, and contracts |
| `AgentMesh.Application` | Agent implementations, workflow orchestration, configuration models |
| `AgentMeshCLI` | Console application entry point and DI composition root |
| `AgentMesh.Infrastructure.OpenAIClient` | OpenAI-compatible API client with multi-provider support |
| `AgentMesh.Infrastructure.JSSandbox` | Client for the external [JSCodeSandbox](https://github.com/demetrio-marra/JSCodeSandbox) service |
| `AgentMesh.Infrastructure.QDrant` | Qdrant vector database integration for semantic search and caching |
| `AgentMesh.Infrastructure.Mem0` | Mem0 agent memory service integration for persistent context |
| `AgentMesh.Infrastructure.QMD` | QMD knowledge base and documentation integration for knowledge access |

## :rocket: Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- A running [Qdrant](https://qdrant.tech/) instance for semantic search
- A deployed [JSCodeSandbox](https://github.com/demetrio-marra/JSCodeSandbox) instance for sandboxed code execution
- A [Mem0](https://mem0.ai/) instance for agent memory (optional but recommended)
- A [QMD](https://github.com/tobi/qmd) instance for knowledge base access (optional)
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
   - **LLM providers** – endpoints and API keys (via environment variables) under `InferenceProviders`
   - **LLMs** – model names and providers for each tier under `LLMs`
   - **Agent settings** – per-agent LLM assignment, temperature, and system prompt files under `Agents`
   - **Qdrant** – host, port, and collection names under `QDrantQueriesCacheService`
   - **Embedding** – model endpoint and name under `Embedding`
   - **Sandbox** – URL and sandbox name under `SESJSSandbox`
   - **Agent Memory** – Mem0 service URL under `AgentMemoryService`
   - **QMD** – QMD proxy configuration under `QMDHttpProxy`

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
    "AnalysisLLM":    { "Model": "...", "Provider": "HuggingFace" },
    "CoderLLM":       { "Model": "...", "Provider": "HuggingFace" },
    "CompletionLLM":  { "Model": "...", "Provider": "HuggingFace" }
  },
  "Agents": {
    "Coder": {
      "LLM": "CoderLLM",
      "ModelTemperature": "0.6",
      "SystemPromptFile": "Prompts/Coder.SystemPrompt.txt"
    },
    "CodeFixer": {
      "LLM": "CoderLLM",
      "ModelTemperature": "0.7",
      "SystemPromptFile": "Prompts/CodeFixer.SystemPrompt.txt"
    }
    // ... other agents
  },
  "AgentMemoryService": {
    "BaseUrl": "http://localhost:8000",
    "TimeoutSeconds": 30
  },
  "QDrantQueriesCacheService": {
    "Host": "localhost",
    "Port": 6333,
    "CollectionName": "queries_cache"
  }
}
```

Each agent can use a different LLM tier, allowing cost optimization by assigning cheaper/smaller models to simpler tasks and more capable models to complex reasoning.

## :robot: Agents

| Agent | Role |
|---|---|
| **Intent Extractor** | Extracts user intent and actionable requirements from input |
| **Intent Canonicalization** | Normalizes and standardizes intent representation |
| **Requirements Collector** | Transforms user requests into structured business requirements; queries knowledge base |
| **Functional Analyst** | Analyzes business requirements and feasibility |
| **Technical Analyst** | Validates technical approach and architecture |
| **Domain Expert** | Provides domain-specific guidance and contextual knowledge |
| **Relevant Facts Evaluator** | Filters and prioritizes relevant facts from knowledge base |
| **Coder** | Generates JavaScript code against predefined API references |
| **Code Fixer** | Repairs code based on static analysis or runtime error feedback |
| **Code Execution Failures Detector** | Analyzes sandbox execution results for runtime failures |
| **Documentation Agent** | Generates formatted documentation and presents results |
| **Personal Assistant** | Handles general conversational queries and small talk |
| **Conversation Summarizer** | Compresses conversation history to stay within token limits |

## :link: External Dependencies

| Service | Purpose | Reference |
|---|---|---|
| **JSCodeSandbox** | Sandboxed JavaScript execution environment | [github.com/demetrio-marra/JSCodeSandbox](https://github.com/demetrio-marra/JSCodeSandbox) |
| **Qdrant** | Vector database for semantic search and query caching | [qdrant.tech](https://qdrant.tech/) |
| **Mem0** | Agent memory and context persistence service | [mem0.ai](https://mem0.ai/) |
| **QMD** | Knowledge base and documentation access | [github.com/tobi/qmd](https://github.com/tobi/qmd) |

## :gear: Tech Stack

- **.NET 8** / **C# 12**
- **Microsoft.Extensions** (DI, Configuration, Logging, Options, HttpClient)
- **OpenAI SDK** (OpenAI-compatible API client)
- **Qdrant.Client** for vector search
- **Polly** for resilience and retry policies
- **Mem0 SDK** for agent memory
- **QMD** for knowledge base integration

## :page_facing_up: License

See [LICENSE](LICENSE) for details.
