using AgentMesh.Application.Models.Parameters;
using AgentMesh.Application.Services.Executors;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class AgentMemorySaverServiceEWCodeStep(
        AgentMemoryExecutor agentMemoryExecutor,
        RelevantMessagesToSaveInAgentMemoryParameter relevantConversationMessagesParameter) : IEWCodeStep
    {
        public string Name => "Agent Memory Saver Service";

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            await agentMemoryExecutor.SaveAsync(relevantConversationMessagesParameter.ParameterValue!);
        }
    }
}
