## Context

See `proposal.md` for motivation.

Current state:
- Solution is layered across `AgentMeshCLI` (host/composition), `AgentMesh.Application` (pipelines/steps/agents), `AgentMesh` (core contracts/runtime abstractions), plus infrastructure adapters (`OpenAIClient`, `Mem0`, `LightRag`, `Cohere`, `JSSandbox`).
- Runtime orchestration is parameter-driven (`EWPipeline` + `ParameterStore`) with optimistic concurrency and auditable step stats.
- Architecture knowledge is distributed across many files and currently not captured in a reusable planning artifact.

Constraint:
- Explore/capture only; no implementation changes.

## Architecture Baseline Snapshot

### Project layering
- `AgentMeshCLI`: composition root, configuration binding, hosted console loop, runtime DI setup.
- `AgentMesh.Application`: pipeline implementations, step implementations, agent implementations, application contracts.
- `AgentMesh`: core interfaces/models for pipelines, steps, parameters, parameter store, workflow notifications.
- `AgentMesh.Infrastructure.*`: adapter implementations for external systems (`OpenAI`, `Mem0`, `LightRag`, `Cohere`, JS sandbox).

### Runtime execution flow
1. User input enters from `UserConsoleInputService`.
2. `AppInstance` creates scoped pipeline execution and initializes request parameters.
3. `ChatRequestPipeline` selects next `IEWStep` instances by branch conditions.
4. `EWPipeline` executes steps, records input/output stats, and commits parameter mutations through `ParameterStore`.
5. Final response is read from `FinalAnswerParameter` and appended to conversation context.
6. Optional summarization pipeline runs when token threshold is reached.

### External integration boundaries
- LLM boundary: `IOpenAIClient` via `OpenAIClientFactory` and `OpenAIClient`.
- Knowledge boundary: `IKnowledgeService` via `LightRagKnowledgeService`.
- Memory boundary: `IAgentMemoryService` via `Mem0AgentMemoryService`.
- Reranking boundary: `IRerankerService` via `CohereV1RerankerService`.
- Sandbox boundary: `IJSSandbox` via `SESJSSandboxClient` and `JSSandboxExecutor`.

### Extension points for future changes
- Add new business state via `BaseEWParameterConfiguration<T>` implementations.
- Add new execution behavior via `IEWStep` / `IEWAgenticStep` implementations.
- Add/alter orchestration branch logic in `ChatRequestPipeline` and `SummarizationPipeline`.
- Add new agent behavior through `AbstractAgent<T>` descendants and corresponding prompt/config entries.
- Register runtime dependencies through `Program.cs` composition root (or keep reflection-discovered patterns where applicable).

## Goals / Non-Goals

**Goals:**
- Capture a stable architecture baseline that coding agents can reuse.
- Make dependency boundaries explicit.
- Document core execution flow and extension points for new steps/agents.

**Non-Goals:**
- Changing runtime behavior, DI registrations, or pipeline ordering.
- Introducing new OpenSpec product requirements.

## Decisions

1) Use OpenSpec change artifacts as the storage medium
- Rationale: keeps architecture baseline versioned and colocated with future changes.
- Alternatives considered:
  - `README.md` update: too broad, mixes user docs and internal planning.
  - External wiki: not guaranteed in-repo availability for agents.

2) Capture architecture at four levels: project graph, runtime flow, boundaries, extension points
- Rationale: this is the minimum context needed for safe future edits.
- Alternatives considered:
  - File-by-file catalog: high maintenance, low signal.
  - Pure conceptual doc: misses concrete integration anchors.

3) Keep this change spec-less (documentation/planning only)
- Rationale: no product behavior changes are introduced.
- Alternative considered:
  - Forcing a synthetic capability spec: would misrepresent actual scope.

## Risks / Trade-offs

- [Risk] Baseline becomes stale as code evolves ? Mitigation: refresh in future architecture-focused changes.
- [Risk] Missing edge-case flows in first capture ? Mitigation: focus on mainline execution and clearly state known gaps.
- [Trade-off] Concise architecture summary over exhaustive cataloging ? Easier reuse, less guaranteed completeness.

## Migration Plan

1. Scaffold change `architecture-baseline-scan`.
2. Store proposal and design artifacts with the discovered architecture map.
3. Reference this change from later proposals that touch orchestration, steps, or DI boundaries.

Rollback:
- If this baseline is inaccurate or unwanted, delete/archive this OpenSpec change; runtime is unaffected.

## Open Questions

- Should the project adopt a periodic architecture-refresh cadence (for example, monthly or per major refactor)?
- Should a lightweight architecture summary also be mirrored into `README.md` for contributors outside OpenSpec workflows?
