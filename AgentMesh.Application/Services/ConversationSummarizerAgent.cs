using AgentMesh.Application.Models;
using AgentMesh.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AgentMesh.Application.Services
{
    public class ConversationSummarizerAgent : IConversationSummarizerAgent
    {
        public const string SectionName = "Agents:ConversationSummarizer";
        public const string AgentName = "ConversationSummarizer";

        private readonly IOpenAIClient _openAIClient;
        private readonly ILogger<ConversationSummarizerAgent> _logger;

        public ConversationSummarizerAgent([FromKeyedServices(ConversationSummarizerAgentConfiguration.AgentName)] IOpenAIClient openAIClient,
                                          ConversationSummarizerAgentConfiguration configuration,
                                          ILogger<ConversationSummarizerAgent> logger)
        {
            _openAIClient = openAIClient;
            _logger = logger;
        }

        public async Task<ConversationSummarizerAgentOutput> ExecuteAsync(ConversationSummarizerAgentInput input, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Executing ConversationSummarizerAgent.");
            _logger.LogDebug("ConversationSummarizerAgent Input: {Input}", System.Text.Json.JsonSerializer.Serialize(input));

            var countOfMessagesToIncludeInSummarization = input.Conversation.Count() - input.CountOfMessagesToKeep;
            if (countOfMessagesToIncludeInSummarization <= 0)
            {
                countOfMessagesToIncludeInSummarization = input.Conversation.Count();
            }

            var messagesToSummarize = input.Conversation
                .Take(countOfMessagesToIncludeInSummarization)
                .ToList();

            var lastSummarizedMessageTimeStamp = messagesToSummarize.LastOrDefault()?.Date ?? DateTime.MinValue;

            var serializedConversation = MessageSerializationUtils.SerializeConversationHistory(messagesToSummarize);

            var inputMessages = new List<AgentMessage>
            {
                new AgentMessage { Role = AgentMessageRole.System, Content = "Today date is " + DateTime.UtcNow.ToString("yyyy-MM-dd") + "." },
                new AgentMessage { Role = AgentMessageRole.System, Content = $"Summarize in {input.SummaryLanguage} language" },
                new AgentMessage { Role = AgentMessageRole.User, Content = serializedConversation }
            };

            var stopwatch = Stopwatch.StartNew();

            var result = await Resilience.ExecuteWithRetryAsync(async () =>
            {
                var response = await _openAIClient.GenerateResponseAsync(inputMessages);
                var responseText = response.Text?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(responseText))
                {
                    _logger.LogWarning("The model's response is empty");
                    throw new EmptyAgentResponseException();
                }

                return new ConversationSummarizerAgentOutput
                {
                    Summary = responseText,
                    TokenCount = response.TotalTokenCount,
                    InputTokenCount = response.InputTokenCount,
                    OutputTokenCount = response.OutputTokenCount
                };
            }, ConversationSummarizerAgentConfiguration.AgentName, _logger);

            stopwatch.Stop();
            _logger.LogDebug("ConversationSummarizerAgent completed in {ElapsedMilliseconds}ms with {TotalTokens} tokens.",
                stopwatch.ElapsedMilliseconds, result.TokenCount);

            var newConversation = input.Conversation
                .Skip(countOfMessagesToIncludeInSummarization)
                .ToList();

            // add summarized message to conversation
            newConversation.Insert(0, new ContextMessage
            {
                Role = ContextMessageRole.Assistant,
                Text = $"Summary of previous conversation: {result.Summary}",
                Date = lastSummarizedMessageTimeStamp
            });

            result.NewConversation = newConversation;

            _logger.LogDebug("ConversationSummarizerAgent Output: {Output}", System.Text.Json.JsonSerializer.Serialize(result));
            return result;
        }
    }
}
