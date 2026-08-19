using AgentMesh.Application.Models.Parameters;
using AgentMesh.Application.Services.Executors;
using AgentMesh.Models;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class AgentMemorySaverServiceEWCodeStep(
        AgentMemoryExecutor agentMemoryExecutor,
        RelevantMessagesToSaveInAgentMemoryParameter relevantConversationMessagesParameter) : IEWStep
    {
        public string Name => "Agent Memory Saver Service";

        public IEnumerable<Type> InputParameterTypes => [typeof(RelevantMessagesToSaveInAgentMemoryParameter)];

        public IEnumerable<Type> OutputParameterTypes => [];

        public async Task<EWStepExecutionResult> ExecuteAsync(IReadOnlyDictionary<Type, object?> Values, CancellationToken cancellationToken = default)
        {
            var relevantMessages = relevantConversationMessagesParameter.ValueAs(Values[typeof(RelevantMessagesToSaveInAgentMemoryParameter)]);
            if (relevantMessages is null)
            {
                return await Task.FromResult(new EWStepExecutionResult());
            }
            await agentMemoryExecutor.SaveAsync(relevantMessages);
            return await Task.FromResult(new EWStepExecutionResult());
        }
    }
}
