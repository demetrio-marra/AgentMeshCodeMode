using AgentMesh.Application.Models.ChatMessages;
using AgentMesh.Application.Models.Parameters;
using AgentMesh.Application.Services.Agents;
using AgentMesh.Models;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class ConversationSummarizerEWAgenticStep(
        ConversationSummarizerAgent agent,
        RequestDateTimeParameter requestDateTimeParameter,
        SummarizeLanguageParameter summarizeLanguageParameter,
        MessagesToSummarizeParameter messagesToSummarizeParameter,
        SummarizedContentParameter summarizedContentParameter,
        SummarizedContentDatetimeParameter summarizedContentDatetime) : IEWAgenticStep
    {
        public string Name => "Conversation Summarizer";
        
        public string? AgentName => "ConversationSummarizer";

        public bool CountInputTokensAsContextTokens => false;

        public bool CountOutputTokensAsContextTokens => true;

        public async Task<EWAgenticStepResultRecord> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var lastSummarizedMessageTimeStamp = messagesToSummarizeParameter.ParameterValue?.LastOrDefault()?.Date ?? DateTime.MinValue;

            var agentOutput = await agent.ExecuteAsync([requestDateTimeParameter,
                summarizeLanguageParameter,
                messagesToSummarizeParameter], cancellationToken);

            summarizedContentParameter.ParameterValue = agentOutput.Result;
            summarizedContentDatetime.ParameterValue = lastSummarizedMessageTimeStamp;

            return new EWAgenticStepResultRecord(agentOutput.InputTokenCount, agentOutput.OutputTokenCount);
        }
    }
}
