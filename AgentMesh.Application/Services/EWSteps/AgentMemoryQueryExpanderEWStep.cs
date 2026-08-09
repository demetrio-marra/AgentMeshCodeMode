using AgentMesh.Application.Configuration;
using AgentMesh.Application.Models.AgentMemory;
using AgentMesh.Application.Models.AgentMemoryQueryExpander;
using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class AgentMemoryQueryExpanderEWStep(
        AgentMemoryQueryExpanderAgent agentMemoryQueryExpanderAgent,
        MissingValuesParameter missingValuesParameter,
        PastMemoriesQueryParameter pastMemoriesQueryParameter) : IEWStep
    {
        public string Name => "Agent Memory Query Expander";

        public bool IsAgentic => true;

        public string? AgentName => AgentMemoryQueryExpanderAgentConfiguration.AgentName;

        public bool IsPipelineFirst => false;

        public bool IsPipelineLast => false;

        public async Task<EWStepResultRecord> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var agentInput = new AgentMemoryQueryExpanderAgentInput
            {
                MemoryTopics = [.. missingValuesParameter.ParameterValue!.Select(mv => new AgentMemoryItem { Memory = mv })]
            };

            var agentOutput = await agentMemoryQueryExpanderAgent.ExecuteAsync(agentInput, cancellationToken);

            var pastMemories = agentOutput.SearchQueries.Select(q => new AgentMemoryItem { Memory = q }).ToList();

            pastMemoriesQueryParameter.ParameterValue = pastMemories;

            var ret = new EWStepResultRecord
            {
                InputTokens = agentOutput.InputTokenCount,
                OutputTokens = agentOutput.OutputTokenCount
            };

            return ret;
        }
    }
}
