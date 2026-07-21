# Workflow Steps Refactoring - Quick Reference

## The 4 Core Responsibilities Pattern

All workflow steps should ONLY do these 4 things:

```csharp
public async Task ExecuteXyzAsync(CodeModeWorkflowState state, CancellationToken cancellationToken = default)
{
    var stopwatch = Stopwatch.StartNew();

    // 1. LOG
    _logger.LogDebug("Engaging XYZ Agent...");

    // 2. CREATE INPUT from state
    var agentInput = new XyzAgentInput
    {
        Property1 = state.Property1,
        Property2 = state.Property2
        // ... (no filtering, no conditional logic, no transformations)
    };

    // 3. NOTIFY
    await _workflowProgressNotifier.NotifyWorkflowStepStart("XYZ Agent", agentInput.ToDictionary());

    // Execute agent
    var output = await _xyzAgent.ExecuteAsync(agentInput, cancellationToken);

    // 4. UPDATE STATE
    state.ResultProperty = output.Result;
    state.AddTokenUsage(...);

    // Notify end
    var notifyDict = output.ToDictionary();
    notifyDict["ELAPSED_TIME"] = WorkflowExecutorFormatting.GetElapsedTime(stopwatch.Elapsed);
    await _workflowProgressNotifier.NotifyWorkflowStepEnd("XYZ Agent", notifyDict);
}
```

## What Should NOT Be in Workflow Steps

? **Filtering/Querying Logic**
- MOVE TO: Agent or Executor

? **Conditional Business Logic**
- `if (condition) { doSomething() }`
- MOVE TO: Agent or Executor

? **Data Transformations**
- Grouping, deduplication, mapping
- MOVE TO: Agent or Executor

? **Method Parameters for Orchestration**
- `async Task DoSomething(state, string data, int iteration)`
- USE: `async Task DoSomething(state, CancellationToken)`
- Pass data via: `state.Property = value`

? **Return Values for Orchestration**
- `async Task<string> DoSomething()`
- USE: `async Task DoSomething()`
- Return data via: `state.Property = value`

## Example: From Problem to Solution

### Before (? Has Business Logic)
```csharp
public async Task ExecuteCoderAsync(CodeModeWorkflowState state)
{
    // ? Filtering logic in workflow step
    var filteredDocs = state.KnowledgeBaseAPIDocumentsContent
        .Where(doc => state.SelectedAPIsFileLocations
            .Contains(doc.File, StringComparer.OrdinalIgnoreCase))
        .ToList();

    var agentInput = new CoderAgentInput
    {
        KnowledgeBaseAPIDocumentsContent = filteredDocs
    };

    await _coderAgent.ExecuteAsync(agentInput);
}
```

### After (? Only Core Responsibilities)
```csharp
public async Task ExecuteCoderAsync(CodeModeWorkflowState state)
{
    // 1. LOG
    _logger.LogDebug("Engaging Coder Agent...");

    // 2. CREATE INPUT (no filtering!)
    var agentInput = new CoderAgentInput
    {
        SelectedAPIsFileLocations = state.SelectedAPIsFileLocations,
        KnowledgeBaseAPIDocumentsContent = state.KnowledgeBaseAPIDocumentsContent
    };

    // 3. NOTIFY
    await _workflowProgressNotifier.NotifyWorkflowStepStart("Coder Agent", ...);

    // 4. EXECUTE & UPDATE STATE
    var output = await _coderAgent.ExecuteAsync(agentInput);
    state.GeneratedCode = output.CodeToRun;

    // ... notify end
}
```

**Agent Now Handles Filtering**:
```csharp
public class CoderAgent
{
    public async Task<CoderAgentOutput> ExecuteAsync(CoderAgentInput input)
    {
        // ? Agent handles filtering
        var filteredDocuments = input.SelectedAPIsFileLocations.Any()
            ? input.KnowledgeBaseAPIDocumentsContent
                .Where(doc => input.SelectedAPIsFileLocations
                    .Contains(doc.File, StringComparer.OrdinalIgnoreCase))
                .ToList()
            : [];

        // ... rest of agent logic
    }
}
```

## When Adding New Workflow Steps

1. **Define Input DTO** with all data needed from state (no filtering)
2. **Define Output DTO** with all results to update state
3. **Create Workflow Step**:
   - Log
   - Create input from state
   - Notify start
   - Execute agent
   - Update state with output
   - Notify end
4. **Any business logic** (filtering, transformations, decisions) ? goes in Agent/Executor
5. **Any orchestration logic** ? goes in Workflow (not in step!)

## Testing Workflow Steps

A workflow step should be testable by:
1. Creating a state
2. Calling the step
3. Verifying the state was updated correctly

No complex business logic to mock, no conditional branches to test!

