## Why

AgentMesh currently runs only as an interactive console host. This prevents service-to-service integration and automation scenarios that require HTTP access, and it mixes console-only workflow notifications with any future API runtime mode.

Introducing a REST mode with API key protection and an explicit runtime mode switch enables secure remote invocation of the existing request pipeline while preserving today’s interactive CLI behavior.

## What Changes

- Add a REST endpoint in `AgentMeshCLI` that invokes `AppInstance.ProcessRequest` and returns `WorkflowResult` data for a submitted message.
- Add API key authentication mode for HTTP requests, with API key value sourced from application configuration.
- Add a `--interactive` command-line switch to select runtime mode.
- When `--interactive` is set, run console mode only (no controllers, no Swagger services/middleware).
- When `--interactive` is not set, run API mode only (no `UserConsoleInputService` hosted service).
- In API mode, register a no-op implementation of `IWorkflowProgressNotifier` so workflow execution does not emit console progress output.

## Capabilities

### New Capabilities
- `request-access-modes`: Defines runtime mode behavior for interactive console vs REST API execution, API-key-protected request processing, and progress notification behavior per mode.

### Modified Capabilities
- None.

## Non-goals

- No changes to core pipeline decision logic in `ChatRequestPipeline`.
- No changes to `AppInstance.ProcessRequest` business behavior beyond exposing it via HTTP.
- No multi-user conversation isolation redesign.
- No OAuth/JWT or role-based authorization.

## Impact

- Affected projects: `AgentMeshCLI` (host configuration, runtime mode branching, API endpoint/auth setup, notifier registration), possibly small shared configuration model additions.
- New externally observable surface: HTTP endpoint protected by API key.
- Configuration impact: new API key setting and (if needed) endpoint hosting settings.
- Dependency impact: likely adds ASP.NET Core API/Swagger/auth packages to `AgentMeshCLI` if not already present.
