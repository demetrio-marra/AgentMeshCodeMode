using AgentMesh.Application.Models.ChatMessages;
using AgentMesh.Application.Models.Conversation;
using AgentMesh.Application.Models.Workflows;
using AgentMesh.Application.Services.Agents;
using AgentMesh.Models;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class ConversationSummarizerEWAgenticStep(
        ConversationSummarizerAgent agent,
        ConversationContext chatContext,
        RequestDateTimeParameter requestDateTimeParameter,
        SummarizeLanguageParameter summarizeLanguageParameter,
        MessagesToSummarizeParameter messagesToSummarizeParameter) : IEWAgenticStep
    {
        public string Name => "Conversation Summarizer";
        
        public string? AgentName => "ConversationSummarizer";

        public bool IsInputTokensCountSource => false;

        public bool IsOutputTokensCountSource => true;

        public async Task<EWAgenticStepResultRecord> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var lastSummarizedMessageTimeStamp = messagesToSummarizeParameter.ParameterValue?.LastOrDefault()?.Date ?? DateTime.MinValue;
            var countOfMessagesToKeep = chatContext.Conversation.Count() - messagesToSummarizeParameter.ParameterValue!.Count();

            var agentOutput = await agent.ExecuteAsync([requestDateTimeParameter,
                summarizeLanguageParameter,
                messagesToSummarizeParameter], cancellationToken);

            // scarta i messaggi che sono stati riassunti
            var newConversation = chatContext.Conversation.Skip(messagesToSummarizeParameter.ParameterValue!.Count())
                .ToList();

            // e aggiungi il messaggio di riepilogo all'inizio della conversazione
            newConversation.Insert(0, new ContextMessage
            {
                Role = ContextMessageRole.Assistant,
                Text = $"Summary of previous conversation: {agentOutput.Result}",
                Date = lastSummarizedMessageTimeStamp
            });

            chatContext.Conversation = newConversation.ToList();
            chatContext.TokensCount = agentOutput.OutputTokenCount; // non è preciso

            return new EWAgenticStepResultRecord(agentOutput.InputTokenCount, agentOutput.OutputTokenCount);
        }
    }
}
