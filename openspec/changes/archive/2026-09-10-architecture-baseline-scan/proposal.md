## Why

AI coding agents need a reliable, shared architecture baseline before proposing changes. Today, architecture knowledge is implicit across source files, which increases onboarding time, inconsistent edits, and avoidable re-discovery during each task.

This change captures a reusable architecture snapshot so future agents can reason from the same system map.

## What Changes

- Capture a project-wide architecture scan as OpenSpec planning artifacts.
- Document solution layering, runtime execution flow, dependency boundaries, and extension points.
- Record key files and ownership of responsibilities (pipeline, steps, agents, infrastructure adapters, composition root).
- Provide a stable baseline to reference in future proposals/designs.

## Non-goals

- No product behavior changes.
- No source-code implementation changes.
- No API contract changes.
- No dependency upgrades.

## Capabilities

### New Capabilities
- None. This change is documentation/planning only.

### Modified Capabilities
- None. No existing requirement behavior is being changed.

## Impact

- Affected artifacts: `openspec/changes/architecture-baseline-scan/proposal.md`, `openspec/changes/architecture-baseline-scan/design.md`.
- Affected systems: planning knowledge used by AI coding workflows.
- Runtime impact: none.
- Build/deploy impact: none.
