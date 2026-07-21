# Workflow Steps Refactoring - Architectural Overview

## Layered Architecture After Refactoring

```
???????????????????????????????????????????????????????????????
?                    CodeModeWorkflow                         ?
?         (Orchestration: Decides step execution order)       ?
?         (State management via CodeModeWorkflowState)        ?
???????????????????????????????????????????????????????????????
           ?
           ???? Workflow Step 1 ??? [Log, Create Input, Notify, Execute, Update State]
           ?                              ?
           ?                              ?
           ?                        Agent/Executor
           ?                        (Business Logic)
           ?
           ???? Workflow Step 2 ??? [Log, Create Input, Notify, Execute, Update State]
           ?                              ?
           ?                              ?
           ?                        Agent/Executor
           ?                        (Business Logic)
           ?
           ???? State is passed through each step
                (Each step reads and writes to state)
```

## Data Flow Pattern

```
State (t)
  ?
  ??? Workflow Step 1
  ?     ?? Read: state.InputData1, state.InputData2
  ?     ?? Create: Step1Input
  ?     ?? Execute: agent.ExecuteAsync(input)
  ?     ?? Write: state.OutputData1 = result
  ?
State (t+1)
  ?
  ??? Workflow Step 2
  ?     ?? Read: state.OutputData1 (from Step 1)
  ?     ?? Create: Step2Input
  ?     ?? Execute: agent.ExecuteAsync(input)
  ?     ?? Write: state.OutputData2 = result
  ?
State (t+2)
  ?
  ??? ... continue workflow
```

## Responsibility Distribution

### CodeModeWorkflow (Orchestration)
**Responsibilities**:
- Decide which steps to execute and in what order
- Handle control flow (if/else, loops, gotos)
- Manage workflow state lifecycle
- Decide branching based on state conditions

**Example**:
```csharp
if (state.FunctionalAnalystRejected)
{
    goto CompleteWorkflow;
}

for (int i = 0; i < 2 && state.CodeExecutionFailuresDetectorIterationCount < 2; i++)
{
    await _codeExecutionFailuresDetectorWorkflowStep.ExecuteCodeExecutionFailuresDetectorAsync(state);

    if (state.CodeExecutionAnalysis?.Equals(JavascriptCodeExecutionFailuresDetectorAgent.NO_ERROR) ?? false)
    {
        break;
    }

    await _codeFixerForRuntimeErrorsWorkflowStep.ExecuteCodeFixerForRuntimeErrorsAsync(state);
}
```

### Workflow Steps (Coordination)
**Responsibilities**:
- Coordinate between workflow and agents
- Bridge data from state to agent input
- Bridge data from agent output to state
- Notify progress to observers

**NOT Responsibilities**:
- Business logic
- Filtering/querying
- Conditional decisions
- Data transformations

### Agents/Executors (Business Logic)
**Responsibilities**:
- Accept input DTO (may include raw data + selection parameters)
- Implement all business logic (filtering, transformations, decisions)
- Return output DTO with results
- Handle validation and error cases

**Example** (CoderAgent):
```csharp
// Agent receives ALL data + selection parameters
public async Task<CoderAgentOutput> ExecuteAsync(CoderAgentInput input)
{
    // Agent implements the filtering logic
    var filteredDocuments = input.SelectedAPIsFileLocations.Any()
        ? input.KnowledgeBaseAPIDocumentsContent
            .Where(doc => input.SelectedAPIsFileLocations
                .Contains(doc.File, StringComparer.OrdinalIgnoreCase))
            .ToList()
        : [];

    // Agent implements business logic
    var inputMessages = BuildMessages(input, filteredDocuments);
    var result = await ExecuteWithRetryAsync(inputMessages);

    return new CoderAgentOutput { CodeToRun = result };
}
```

## Key Principles

### 1. State is the Single Source of Truth
- All data passes through `CodeModeWorkflowState`
- No data passed as method parameters between steps
- Enables workflow replay, debugging, and testing

### 2. Agents are Reusable
- Agents don't know about workflow steps
- Agents can be tested independently
- Agents can be reused in different workflows

### 3. Clear Contracts via DTOs
- Input DTOs specify what data an agent needs
- Output DTOs specify what results are produced
- DTOs are versioned and stable

### 4. Workflow as Choreography
- Workflow decides when to call which steps
- Steps don't coordinate with each other
- Clear, sequential execution model

## Example: Adding a New Agent

### Step 1: Create DTOs (AgentMesh project)
```csharp
public class MyAgentInput
{
    public string Data1 { get; set; }
    public IEnumerable<string> SelectionCriteria { get; set; }
}

public class MyAgentOutput : IAgentOutput
{
    public string Result { get; set; }
    public int TokenCount { get; set; }
    public int InputTokenCount { get; set; }
    public int OutputTokenCount { get; set; }
}
```

### Step 2: Create Agent (AgentMesh.Application)
```csharp
public class MyAgent : AgentBase<string>
{
    public async Task<MyAgentOutput> ExecuteAsync(MyAgentInput input)
    {
        // Implement business logic (filtering, transformations, etc.)
        var filtered = input.Data1
            .Where(item => input.SelectionCriteria.Contains(item))
            .ToList();

        var result = await ProcessAsync(filtered);

        return new MyAgentOutput { Result = result, ... };
    }
}
```

### Step 3: Create Workflow Step (AgentMesh.Application)
```csharp
public class MyWorkflowStep
{
    public async Task ExecuteMyAgentAsync(CodeModeWorkflowState state)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("Engaging My Agent...");

        var input = new MyAgentInput
        {
            Data1 = state.Data1,
            SelectionCriteria = state.SelectionCriteria
        };

        await _workflowProgressNotifier.NotifyWorkflowStepStart("My Agent", ...);

        var output = await _myAgent.ExecuteAsync(input);
        state.MyAgentResult = output.Result;

        await _workflowProgressNotifier.NotifyWorkflowStepEnd("My Agent", ...);
    }
}
```

### Step 4: Add State Properties
```csharp
public class CodeModeWorkflowState
{
    public string? Data1 { get; set; }
    public IEnumerable<string> SelectionCriteria { get; set; }
    public string? MyAgentResult { get; set; }
}
```

### Step 5: Add to Workflow
```csharp
public async Task ExecuteAsync(...)
{
    // ... earlier steps

    await _myWorkflowStep.ExecuteMyAgentAsync(state);

    // ... later steps
}
```

## Benefits of This Architecture

| Aspect | Benefit |
|--------|---------|
| **Testability** | Each agent can be tested independently with different inputs |
| **Reusability** | Agents can be used in multiple workflows |
| **Maintainability** | Business logic is in one place (agents) |
| **Scalability** | Easy to add new agents and steps without affecting existing ones |
| **Debugging** | State snapshot at each step makes debugging easier |
| **Monitoring** | Each step notifies progress; easy to track workflow execution |
| **Parallelization** | Workflow can execute independent steps in parallel (future) |

## Anti-Patterns to Avoid

? **Putting business logic in workflow steps**
- ? Move it to agents

? **Passing data between steps via method parameters**
- ? Pass it via state properties

? **Having workflow steps return values**
- ? Steps should store results in state

? **Having workflow steps call other workflow steps**
- ? Only workflow should orchestrate steps

? **Duplicating business logic across multiple agents**
- ? Create a reusable service/executor if logic is common

