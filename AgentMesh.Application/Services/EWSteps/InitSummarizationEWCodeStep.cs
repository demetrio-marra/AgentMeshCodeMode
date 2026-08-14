using AgentMesh.Application.Models.ChatMessages;
using AgentMesh.Application.Models.Conversation;
using AgentMesh.Application.Models.Workflows;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    /// <summary>
    /// Prepara i parametri necessari per il passo di codice EW di riepilogo della conversazione. 
    /// Questo passo raccoglie i messaggi dalla conversazione corrente e li assegna ai parametri appropriati per il successivo processo di riepilogo.
    /// </summary>
    /// <param name="conversationContext"></param>
    /// <param name="conversationSummarizerAgentConfiguration"></param>
    /// <param name="messagesToSummarizeParameter"></param>
    /// <param name="summarizeLanguageParameter"></param>
    public class InitSummarizationEWCodeStep(
        ConversationContext conversationContext,
        ConversationSummarizerAgentConfiguration conversationSummarizerAgentConfiguration,
        MessagesToSummarizeParameter messagesToSummarizeParameter,
        SummarizeLanguageParameter summarizeLanguageParameter) : IEWCodeStep
    {
        public string Name => "Init Summarization";

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            if (!conversationContext.Conversation.Any())
            {
                throw new ArgumentException("Conversation context is empty. Cannot initialize EW code step.");
            }

            var countOfMessagesToIncludeInSummarization = conversationContext.Conversation.Count() - conversationSummarizerAgentConfiguration.NumMessageToPreseve;
            if (countOfMessagesToIncludeInSummarization <= 0)
            {
                countOfMessagesToIncludeInSummarization = conversationContext.Conversation.Count();
            }

            var messagesToSummarize = conversationContext.Conversation
                .Take(countOfMessagesToIncludeInSummarization)
                .ToList();

            messagesToSummarizeParameter.ParameterValue = [.. messagesToSummarize.Select(message => new ContextMessage
            {
                Role = message.Role,
                Date = message.Date,
                Text = message.Text
            })];

            summarizeLanguageParameter.ParameterValue = conversationSummarizerAgentConfiguration.SummarizeLanguage;

            await Task.CompletedTask;
        }
    }
}
