using AgentMesh.Application.Models.AgentMemory;
using AgentMesh.Application.Models.Workflows.Parameters;
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

        public bool IsInputTokensCountSource => false;

        public bool IsOutputTokensCountSource => false;

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
