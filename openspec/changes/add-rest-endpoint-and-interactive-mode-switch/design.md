## Context

See `proposal.md` for motivation and `specs/request-access-modes/spec.md` for required behavior.

Current startup in `AgentMeshCLI/Program.cs` is a generic host (`HostApplicationBuilder`) that always registers `UserConsoleInputService` and `ConsoleWorkflowProgressNotifier`. There is no HTTP surface, no authentication middleware, and no runtime switch between console and API composition. `AppInstance.ProcessRequest` already provides a single entry point for request execution and result materialization, so the API endpoint can reuse this contract directly.

## Goals / Non-Goals

**Goals:**
- Add an HTTP entry point that delegates request processing to `AppInstance.ProcessRequest`.
- Enforce API key authentication for API mode.
- Introduce deterministic startup composition based on `--interactive`.
- Avoid dual activation of console and API runtime components.
- Provide a no-op `IWorkflowProgressNotifier` implementation for API mode.

**Non-Goals:**
- Reworking pipeline internals or workflow orchestration.
- Introducing per-user conversation tenancy/session isolation.
- Adding OAuth/JWT, RBAC, or external identity provider integration.

## Decisions

### Decision 1: Switch `AgentMeshCLI` host bootstrap to web-capable startup
- **Choice:** Use web host startup (`WebApplicationBuilder`) so controllers/auth/swagger can be conditionally registered and mapped.
- **Rationale:** It natively supports both API middleware and DI composition, while still allowing interactive mode execution logic.
- **Alternatives considered:**
  - Keep `HostApplicationBuilder` and self-host ASP.NET Core manually: more complex and non-idiomatic for conditional API setup.
  - Build a second executable for API mode: clearer separation but duplicates composition root and increases configuration drift risk.

### Decision 2: Use argument-driven composition with explicit mutually exclusive registrations
- **Choice:** Parse `--interactive` at startup and branch service registration/middleware setup:
  - Interactive mode: register `UserConsoleInputService` + `ConsoleWorkflowProgressNotifier`; do not register/map controllers or Swagger.
  - API mode: register controllers, authentication/authorization, Swagger; do not register `UserConsoleInputService`; register dummy workflow notifier.
- **Rationale:** Satisfies explicit mode constraints and prevents accidental mixed runtime behavior.
- **Alternatives considered:**
  - Feature flag in configuration only: easier ops control but does not satisfy required CLI switch contract.
  - Always register everything and gate at runtime: violates “must not be instanced at all” requirement.

### Decision 3: Implement API key authentication with custom authentication handler
- **Choice:** Add a simple API key auth scheme (header-based) using a dedicated options/config section (e.g., `ApiAuth:ApiKey`).
- **Rationale:** Keeps security boundary centralized in ASP.NET Core auth pipeline and avoids endpoint-level duplicated checks.
- **Alternatives considered:**
  - Inline key validation in controller action: simpler but bypasses standardized auth pipeline and complicates future expansion.
  - Query-string API key: weaker security hygiene and less standard for server-to-server APIs.

### Decision 4: Add a dedicated no-op workflow notifier implementation for API mode
- **Choice:** Create a dummy class implementing `IWorkflowProgressNotifier` with completed tasks/no output and register it only in API mode.
- **Rationale:** Preserves pipeline dependency contract while preventing console side effects in service mode.
- **Alternatives considered:**
  - Null object via lambda delegates: less discoverable and harder to test/trace.
  - Reuse console notifier in API mode: violates requirement and pollutes server logs with interactive formatting.

## Risks / Trade-offs

- **[Risk]** Existing `ConversationContext` is singleton-scoped and may be shared across API callers.  
  **Mitigation:** Keep behavior unchanged for this change; document as known limitation and defer conversation tenancy redesign.

- **[Risk]** Introducing web hosting may require project SDK/package updates in `AgentMeshCLI.csproj`.  
  **Mitigation:** Keep minimal dependency changes and reuse built-in ASP.NET Core auth/swagger packages compatible with `net8.0`.

- **[Trade-off]** Single binary supporting two modes increases startup branching complexity.  
  **Mitigation:** Keep mode branching centralized in `Program.cs` and ensure each branch has explicit registrations.

## Migration Plan

1. Add API authentication configuration entry to `appsettings.json` (and optional environment override path).
2. Introduce API controller/request DTO + API key authentication handler + dummy notifier.
3. Refactor `Program.cs` composition to parse `--interactive` and branch registrations/middleware.
4. Validate mode behavior:
   - Interactive mode: no controllers/swagger registered.
   - API mode: endpoint available and `UserConsoleInputService` absent.
5. Rollback strategy: remove startup mode branch and API-specific registrations, restoring current interactive-only startup path.

## Open Questions

None.
