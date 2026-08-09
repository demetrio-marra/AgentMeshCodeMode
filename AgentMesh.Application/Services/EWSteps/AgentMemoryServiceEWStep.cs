using AgentMesh.Application.Models.AgentMemory;
using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Application.Services.Executors;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class AgentMemoryServiceEWStep(
        AgentMemoryExecutor agentMemoryExecutor,
        PastMemoriesQueryParameter pastMemoriesQueryParameter,
        PastMemoriesQueryResultsParameter pastMemoriesQueryResultsParameter) : IEWStep
    {
        public string Name => "Agent Memory Service";

        public bool IsAgentic => false;

        public string? AgentName => null;

        public bool IsPipelineFirst => false;

        public bool IsPipelineLast => false;

        public async Task<EWStepResultRecord> ExecuteAsync(CancellationToken cancellationToken = default)
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

            return new EWStepResultRecord(null, null);
        }
    }
}
