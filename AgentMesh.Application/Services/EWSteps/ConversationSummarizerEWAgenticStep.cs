using AgentMesh.Application.Models.ChatMessages;
using AgentMesh.Application.Models.Parameters;
using AgentMesh.Application.Services.Agents;
using AgentMesh.Models;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class ConversationSummarizerEWAgenticStep(
        ConversationSummarizerAgent agent,
        MessagesToSummarizeParameter messagesToSummarizeParameter) : IEWAgenticStep
    {
        public string Name => "Conversation Summarizer";

        public string? AgentName => "ConversationSummarizer";

        public bool CountInputTokensAsContextTokens => false;

        public bool CountOutputTokensAsContextTokens => true;

        public IEnumerable<Type> InputParameterTypes => [
            typeof(RequestDateTimeParameter),
            typeof(SummarizeLanguageParameter),
            typeof(MessagesToSummarizeParameter)
            ];

        public IEnumerable<Type> OutputParameterTypes => [
            typeof(SummarizedContentParameter),
            typeof(SummarizedContentDatetimeParameter)
            ];

        public async Task<EWStepExecutionResult> ExecuteAsync(IReadOnlyDictionary<Type, object?> Values, CancellationToken cancellationToken = default)
        {
            var messagesToSummarize = messagesToSummarizeParameter.ValueAs(Values[typeof(MessagesToSummarizeParameter)]);
            var lastSummarizedMessageTimeStamp = messagesToSummarize?.LastOrDefault()?.Date ?? DateTime.MinValue;

            var agentOutput = await agent.ExecuteAsync(Values, cancellationToken);

            return new EWAgenticStepExecutionResult
            {
                InputTokens = agentOutput.InputTokenCount,
                OutputTokens = agentOutput.OutputTokenCount,
                OutputMutations = new Dictionary<Type, object?>
                {
                    { typeof(SummarizedContentParameter), agentOutput.Result },
                    { typeof(SummarizedContentDatetimeParameter), lastSummarizedMessageTimeStamp }
                }
            };
        }
    }
}
