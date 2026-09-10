## Context

See `proposal.md` for motivation.

AgentMesh already expresses architecture across code and `README.md`, but the information is distributed and unevenly detailed. Current structure is strongly layered and maps cleanly to a documentation model:

- `AgentMeshCLI`: composition root, startup configuration, hosted console execution.
- `AgentMesh.Application`: pipelines, EW steps, agents, executors, and application contracts.
- `AgentMesh`: core abstractions for parameters, steps, pipelines, and workflow notifications.
- `AgentMesh.Infrastructure.*`: adapters for OpenAI-compatible chat, memory, knowledge retrieval, reranking, and JS sandbox.

The runtime model is centered on request-scoped pipeline execution (`AppInstance` + `IChatRequestPipeline`) with parameter-state orchestration (`EWPipeline` + `ParameterStore`) and optional summarization (`ISummarizationPipeline`).

## Goals / Non-Goals

**Goals:**
- Define a durable, versioned architecture documentation capability in OpenSpec.
- Capture a coherent architecture baseline for future users.
- Make boundaries and extension points explicit to reduce accidental cross-layer coupling.

**Non-Goals:**
- No code implementation changes.
- No contract redesign across agents, steps, or parameter store.
- No operational behavior changes to request handling or summarization.

## Decisions

### Decision 1: Capture architecture as an OpenSpec capability
- **Choice:** Add `architecture-documentation` as a new capability spec.
- **Rationale:** Keeps architecture documentation versioned and reviewable alongside change planning.
- **Alternatives considered:**
  - README-only updates: simpler but weaker lifecycle control and traceability.
  - External wiki: flexible but detached from repository change flow.

### Decision 2: Document architecture by stable viewpoints
- **Choice:** Organize architecture documentation around: layering, runtime flow, parameter consistency model, boundaries, and extension points.
- **Rationale:** These are stable concerns that remain useful across implementation-level refactors.
- **Alternatives considered:**
  - Agent-by-agent documentation: detailed but brittle and high-maintenance.
  - File-by-file mapping: exhaustive but low signal for onboarding.

### Decision 3: Keep scope documentation-only within OpenSpec artifacts
- **Choice:** Limit change outputs to proposal/spec/design/tasks artifacts under this change root.
- **Rationale:** Matches user goal and avoids incidental source modifications.
- **Alternatives considered:**
  - Simultaneous source-doc updates: broader impact and mixed concerns.

## Risks / Trade-offs

- **[Risk]** Architecture baseline file referenced in `openspec/config.yaml` is currently missing.  
  **Mitigation:** Build this change from inspected source of truth (`Program.cs`, pipelines, step contracts, infrastructure adapters) and treat this artifact set as the new baseline.

- **[Risk]** Documentation can drift as code evolves.  
  **Mitigation:** Require future architecture-impacting changes to update this capability in OpenSpec.

- **[Trade-off]** High-level viewpoints omit some implementation nuances.  
  **Mitigation:** Link architecture sections to canonical code touchpoints for deeper follow-up.

## Migration Plan

1. Validate planning artifacts for `document-project-architecture`.
2. Use this change as the reference for future architecture-related proposals.
3. At archive time, preserve capability wording and decisions as baseline history.

Rollback is straightforward: discard this change branch if not adopted.

## Open Questions

None.