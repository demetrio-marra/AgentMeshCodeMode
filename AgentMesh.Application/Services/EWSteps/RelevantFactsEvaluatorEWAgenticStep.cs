using AgentMesh.Application.Models.Parameters;
using AgentMesh.Application.Services.Agents;
using AgentMesh.Models;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class RelevantFactsEvaluatorEWAgenticStep(
        RelevantFactsEvaluatorAgent agent,
        RequestDateTimeParameter requestDateTimeParameter,
        MessagesToSummarizeParameter messagesToSummarizeParameter,
        RelevantMessagesToSaveInAgentMemoryParameter relevantMessagesToSaveInAgentMemoryParameter) : IEWAgenticStep
    {
        public string Name => "Relevant Facts Evaluator";
        
        public string? AgentName => "RelevantFactsEvaluator";

        public bool CountInputTokensAsContextTokens => false;

        public bool CountOutputTokensAsContextTokens => false;

        public async Task<EWAgenticStepResultRecord> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var agentOutput = await agent.ExecuteAsync([requestDateTimeParameter,
                messagesToSummarizeParameter], cancellationToken);

            relevantMessagesToSaveInAgentMemoryParameter.ParameterValue = agentOutput.Result;

            return new EWAgenticStepResultRecord(agentOutput.InputTokenCount, agentOutput.OutputTokenCount);
        }
    }
}
