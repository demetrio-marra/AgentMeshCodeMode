using AgentMesh.Application.Models.AgentMemory;
using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Application.Services;
using AgentMesh.Models.AgentMemory;
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

        private readonly AgentMemoryExecutor _agentMemoryExecutor = agentMemoryExecutor;
        private readonly PastMemoriesQueryParameter _pastMemoriesQueryParameter = pastMemoriesQueryParameter;
        private readonly PastMemoriesQueryResultsParameter _pastMemoriesQueryResultsParameter = pastMemoriesQueryResultsParameter;

        public async Task<EWStepResultRecord> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var queriesList = (_pastMemoriesQueryParameter.ParameterValue ?? []).Select(s => s.Memory).ToList();

            var agentInput = new AgentMemoryRetrieverInput
            {
                Query = string.Join(", ", queriesList)
            };

            var executorOutput = await _agentMemoryExecutor.GetAsync(agentInput);

            var currentMemories = _pastMemoriesQueryResultsParameter.ParameterValue ?? [];

            var retrievedMemories = executorOutput.Items.ToList();
            var allMemories = currentMemories.Concat(retrievedMemories).ToList();

            _pastMemoriesQueryResultsParameter.ParameterValue = allMemories;

            return new EWStepResultRecord(null, null);
        }
    }
}
