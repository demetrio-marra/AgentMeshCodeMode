## Purpose

Define how AgentMesh accepts user requests through either interactive console mode or REST API mode, including secure API access and mode-specific runtime component activation.

## ADDED Requirements

### Requirement: REST request endpoint SHALL invoke the existing request pipeline
The system SHALL expose an HTTP endpoint that accepts a user message and returns the result produced by `AppInstance.ProcessRequest`.

#### Scenario: API request is processed successfully
- **WHEN** API mode is active and a client sends a valid request message to the REST endpoint
- **THEN** the endpoint returns a successful response containing the workflow output generated from that message

### Requirement: API mode SHALL enforce API key authentication
The REST endpoint SHALL require an API key for access, and the expected key value SHALL be loaded from application configuration.

#### Scenario: Request with valid API key is authorized
- **WHEN** API mode is active and a request includes the configured API key
- **THEN** the request is authenticated and processed

#### Scenario: Request without valid API key is rejected
- **WHEN** API mode is active and a request omits the API key or provides an invalid key
- **THEN** the request is rejected with an authentication failure response

### Requirement: Runtime mode selection SHALL be controlled by `--interactive`
The host SHALL support a command-line parameter `--interactive` that controls whether interactive console components or HTTP API components are activated.

#### Scenario: Interactive mode disables API components
- **WHEN** the application starts with `--interactive`
- **THEN** controllers and Swagger services/middleware are not instantiated

#### Scenario: API mode disables interactive input service
- **WHEN** the application starts without `--interactive`
- **THEN** `UserConsoleInputService` is not instantiated

### Requirement: Workflow progress notifier SHALL be mode-specific
The workflow progress notifier implementation SHALL be selected by runtime mode.

#### Scenario: API mode uses dummy notifier
- **WHEN** the application starts without `--interactive`
- **THEN** `IWorkflowProgressNotifier` resolves to a no-op implementation that performs no console output
