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
        EWParametersProvider ewParametersProvider) : IEWStep
    {
        public string Name => "Agent Memory Service";

        public bool IsAgentic => false;

        public string? AgentName => null;

        public bool IsPipelineFirst => false;

        public bool IsPipelineLast => false;

        public IEnumerable<string> InputParameters => [
            EWParameterNames.PastMemoriesQuery
        ];

        private readonly AgentMemoryExecutor _agentMemoryExecutor = agentMemoryExecutor;
        private readonly EWParametersProvider _ewParametersProvider = ewParametersProvider;

        public async Task<EWStepResultRecord> ExecuteAsync(IEnumerable<IEWParameter> inputParameters, CancellationToken cancellationToken = default)
        {
            var pastMemoriesQueryParameter = inputParameters.Single(p => p.Name == EWParameterNames.PastMemoriesQuery);
            if (pastMemoriesQueryParameter is not PastMemoriesQueryParameter typedPastMemoriesQuery)
                throw new InvalidOperationException($"Parameter {EWParameterNames.PastMemoriesQuery} is not of type PastMemoriesQueryParameter");

            var queriesList = (typedPastMemoriesQuery.ParameterValue ?? []).Select(s => s.Memory).ToList();

            var agentInput = new AgentMemoryRetrieverInput
            {
                Query = string.Join(", ", queriesList)
            };

            var executorOutput = await _agentMemoryExecutor.GetAsync(agentInput);

            var currentMemories = (_ewParametersProvider.GetParameters([EWParameterNames.PastMemoriesQueryResults])
                .FirstOrDefault() as PastMemoriesQueryResultsParameter)?.ParameterValue ?? [];

            var retrievedMemories = executorOutput.Items.ToList();
            var allMemories = currentMemories.Concat(retrievedMemories).ToList();

            _ewParametersProvider.UpdateParameterValue(EWParameterNames.PastMemoriesQueryResults, (IEnumerable<AgentMemoryQueryResultItem>)allMemories);

            return new EWStepResultRecord(null, null);
        }
    }
}
