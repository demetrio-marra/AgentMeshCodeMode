## 1. API surface and authentication scaffolding

- [x] 1.1 Update `AgentMeshCLI` hosting setup to support ASP.NET Core controllers and verify the project builds with required web/auth/swagger dependencies on `net8.0`.
- [x] 1.2 Add API key configuration model/binding and `appsettings` entry, then verify startup fails fast or logs a clear configuration error when the API key is missing in API mode.
- [x] 1.3 Implement API key authentication scheme/handler and verify requests without a valid key receive an authentication failure while valid-key requests are authorized.

## 2. Request endpoint integration

- [x] 2.1 Add REST request DTO/endpoint that calls `AppInstance.ProcessRequest` and verify a valid request returns workflow response payload fields from `WorkflowResult`.
- [x] 2.2 Add endpoint-level authorization metadata and verify anonymous requests are blocked before controller action execution.
- [x] 2.3 Add API-mode no-op `IWorkflowProgressNotifier` implementation and verify API requests do not emit console progress notifications.

## 3. Runtime mode composition with `--interactive`

- [x] 3.1 Parse `--interactive` in `Program.cs` and branch DI registration so interactive mode registers `UserConsoleInputService` + console notifier only; verify API services/controllers/swagger are not registered in this mode.
- [x] 3.2 Configure default (non-interactive) mode to register controllers/auth/swagger and exclude `UserConsoleInputService`; verify hosted service is absent and endpoint is reachable.
- [x] 3.3 Add startup-mode smoke checks (manual or automated) for both modes and verify: (a) interactive mode has no controller/swagger activation, (b) API mode uses dummy notifier and enforces API key auth.

## 4. Validation and readiness

- [x] 4.1 Run `openspec validate --changes "add-rest-endpoint-and-interactive-mode-switch"` and verify all planning artifacts validate successfully.
- [x] 4.2 Run solution build/tests for changed projects and verify no regressions in existing interactive request flow.
