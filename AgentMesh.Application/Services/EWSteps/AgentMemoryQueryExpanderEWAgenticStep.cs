using AgentMesh.Application.Models.AgentMemory;
using AgentMesh.Application.Models.Workflows;
using AgentMesh.Application.Services.Agents;
using AgentMesh.Models;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class AgentMemoryQueryExpanderEWAgenticStep(
        AgentMemoryQueryExpanderAgent agentMemoryQueryExpanderAgent,
        MissingValuesParameter missingValuesParameter,
        RequestDateTimeParameter requestDateTimeParameter,
        PastMemoriesQueryParameter pastMemoriesQueryParameter) : IEWAgenticStep
    {
        public string Name => "Agent Memory Query Expander";

        public string? AgentName => "AgentMemoryQueryExpander";

        public bool CountInputTokensAsContextTokens => false;

        public bool CountOutputTokensAsContextTokens => false;

        public async Task<EWAgenticStepResultRecord> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var agentOutput = await agentMemoryQueryExpanderAgent.ExecuteAsync([
                requestDateTimeParameter,
                missingValuesParameter], cancellationToken);

            var pastMemories = agentOutput.Result.Select(q => new AgentMemoryItem { Memory = q }).ToList();

            pastMemoriesQueryParameter.ParameterValue = pastMemories;

            var ret = new EWAgenticStepResultRecord
            {
                InputTokens = agentOutput.InputTokenCount,
                OutputTokens = agentOutput.OutputTokenCount
            };

            return ret;
        }
    }
}
