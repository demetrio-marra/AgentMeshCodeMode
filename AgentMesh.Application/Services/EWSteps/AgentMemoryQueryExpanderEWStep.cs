using AgentMesh.Application.Configuration;
using AgentMesh.Application.Models.AgentMemoryQueryExpander;
using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Models.AgentMemory;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class AgentMemoryQueryExpanderEWStep(AgentMemoryQueryExpanderAgent agentMemoryQueryExpanderAgent,
        EWParametersProvider ewParametersProvider) : IEWStep
    {
        public string Name => "Agent Memory Query Expander";

        public bool IsAgentic => true;

        public string? AgentName => AgentMemoryQueryExpanderAgentConfiguration.AgentName;

        public bool IsPipelineFirst => false;

        public bool IsPipelineLast => false;

        public IEnumerable<string> InputParameters => [
            EWParameterNames.MissingValues
        ];

        private readonly AgentMemoryQueryExpanderAgent _agentMemoryQueryExpanderAgent = agentMemoryQueryExpanderAgent;
        private readonly EWParametersProvider _ewParametersProvider = ewParametersProvider;

        public async Task<EWStepResultRecord> ExecuteAsync(IEnumerable<IEWParameter> inputParameters, CancellationToken cancellationToken = default)
        {
            var missingValuesParameter = inputParameters.Single(p => p.Name == EWParameterNames.MissingValues);
            if (missingValuesParameter is not MissingValuesParameter missingValuesEWParameter)
            {
                throw new InvalidOperationException($"Parameter {EWParameterNames.MissingValues} is not of type MissingValuesParameter");
            }

            var agentInput = new AgentMemoryQueryExpanderAgentInput
            {
                MemoryTopics = [.. missingValuesEWParameter.ParameterValue!.Select(mv => new AgentMemoryItem { Memory = mv })]
            };

            var agentOutput = await _agentMemoryQueryExpanderAgent.ExecuteAsync(agentInput, cancellationToken);

            var pastMemories = agentOutput.SearchQueries.Select(q => new AgentMemoryItem { Memory = q }).ToList();

            _ewParametersProvider.UpdateParameterValue(EWParameterNames.PastMemoriesQueryResults, pastMemories);

            var ret = new EWStepResultRecord
            {
                InputTokens = agentOutput.InputTokenCount,
                OutputTokens = agentOutput.OutputTokenCount
            };

            return ret;
        }
    }
}
