using AgentMesh.Application.Contracts;
using AgentMesh.Application.Models;
using AgentMesh.Application.Utils;
using AgentMesh.Models.ChatMessages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AgentMesh.Application.Services
{
    public class ConversationSummarizerAgent([FromKeyedServices(ConversationSummarizerAgentConfiguration.AgentName)] IOpenAIClient openAIClient,
                                      Resilience resilience,
                                      ILogger<ConversationSummarizerAgent> logger) : AgentBase<string>(logger, ConversationSummarizerAgentConfiguration.AgentName, openAIClient, resilience)
    {
        public const string SectionName = ConversationSummarizerAgentConfiguration.SectionName;
        public const string AgentName = ConversationSummarizerAgentConfiguration.AgentName;
        private readonly ILogger<ConversationSummarizerAgent> _logger = logger;

        public async Task<ConversationSummarizerAgentOutput> ExecuteAsync(ConversationSummarizerAgentInput input, CancellationToken cancellationToken = default)
        {
            var countOfMessagesToIncludeInSummarization = input.Conversation.Count() - input.CountOfMessagesToKeep;
            if (countOfMessagesToIncludeInSummarization <= 0)
            {
                countOfMessagesToIncludeInSummarization = input.Conversation.Count();
            }

            var messagesToSummarize = input.Conversation
                .Take(countOfMessagesToIncludeInSummarization)
                .ToList();

            var lastSummarizedMessageTimeStamp = messagesToSummarize.LastOrDefault()?.Date ?? DateTime.MinValue;

            var userPayload = new
            {
                conversation = messagesToSummarize.Select(message => new
                {
                    role = message.Role == ContextMessageRole.User ? "User" : "Assistant",
                    message.Date,
                    message.Text
                })
            };

            var systemMessages = new List<string>
            {
                $"Today date is {DateTime.UtcNow:yyyy-MM-dd}.",
                $"Summarize in {input.SummaryLanguage} language"
            };

            var inputMessages = new List<AgentMessage>
            {
                new() { Role = AgentMessageRole.System, Content = string.Join(Environment.NewLine + Environment.NewLine, systemMessages) },
                new() { Role = AgentMessageRole.User, Content = JsonSerializer.Serialize(userPayload, AgentResponseJsonSerializationUtils.DefaultSerializeOptions) }
            };

            var result = await ExecuteWithRetryAsync(inputMessages, cancellationToken);

            var newConversation = input.Conversation
                .Skip(countOfMessagesToIncludeInSummarization)
                .ToList();

            newConversation.Insert(0, new ContextMessage
            {
                Role = ContextMessageRole.Assistant,
                Text = $"Summary of previous conversation: {result.Result}",
                Date = lastSummarizedMessageTimeStamp
            });

            return new ConversationSummarizerAgentOutput
            {
                Summary = result.Result,
                NewConversation = newConversation,
                TokenCount = result.TotalTokenCount,
                InputTokenCount = result.InputTokenCount,
                OutputTokenCount = result.OutputTokenCount
            };
        }

        protected override string ParseStructuredResponse(string rawResponseText) => rawResponseText;
    }
}
