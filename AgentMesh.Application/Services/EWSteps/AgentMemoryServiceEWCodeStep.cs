using AgentMesh.Application.Models.AgentMemory;
using AgentMesh.Application.Models.Workflows;
using AgentMesh.Application.Services.Executors;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class AgentMemoryServiceEWCodeStep(
        AgentMemoryExecutor agentMemoryExecutor,
        PastMemoriesQueryParameter pastMemoriesQueryParameter,
        PastMemoriesQueryResultsParameter pastMemoriesQueryResultsParameter) : IEWCodeStep
    {
        public string Name => "Agent Memory Service";

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var queriesList = (pastMemoriesQueryParameter.ParameterValue ?? []).Select(s => s.Memory).ToList();

            var agentInput = new AgentMemoryRetrieverInput
            {
                Query = string.Join(", ", queriesList)
            };

            var executorOutput = await agentMemoryExecutor.GetAsync(agentInput);

            var currentMemories = pastMemoriesQueryResultsParameter.ParameterValue ?? [];

            var retrievedMemories = executorOutput.Items.ToList();
            var allMemories = currentMemories.Concat(retrievedMemories).ToList();

            pastMemoriesQueryResultsParameter.ParameterValue = allMemories;
        }
    }
}
