using AgentMesh.Application.Models.AgentMemory;
using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Application.Services.Agents;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class AgentMemoryQueryExpanderEWStep(
        AgentMemoryQueryExpanderAgent agentMemoryQueryExpanderAgent,
        MissingValuesParameter missingValuesParameter,
        RequestDateTimeParameter requestDateTimeParameter,
        PastMemoriesQueryParameter pastMemoriesQueryParameter) : IEWStep
    {
        public string Name => "Agent Memory Query Expander";

        public bool IsAgentic => true;

        public string? AgentName => "AgentMemoryQueryExpander";

        public bool IsPipelineFirst => false;

        public bool IsPipelineLast => false;

        public async Task<EWStepResultRecord> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var agentOutput = await agentMemoryQueryExpanderAgent.ExecuteAsync([
                requestDateTimeParameter,
                missingValuesParameter], cancellationToken);

            var pastMemories = agentOutput.Result.Select(q => new AgentMemoryItem { Memory = q }).ToList();

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
