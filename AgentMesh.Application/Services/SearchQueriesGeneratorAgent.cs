using AgentMesh.Application.Contracts;
using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Models;
using AgentMesh.Models.SearchQueriesGenerator;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentMesh.Application.Services
{
    public class SearchQueriesGeneratorAgent(
        [FromKeyedServices(SearchQueriesGeneratorAgentConfiguration.AgentName)] IOpenAIClient openAIClient,
        Resilience resilience,
        ILogger<SearchQueriesGeneratorAgent> logger) : AgentBase<SearchQueriesGeneratorAgent.ParsedResponse>(logger, SearchQueriesGeneratorAgentConfiguration.AgentName, openAIClient, resilience), ISearchQueriesGeneratorAgent
    {
        private readonly ILogger<SearchQueriesGeneratorAgent> _logger = logger;

        public async Task<SearchQueriesGeneratorAgentOutput> ExecuteAsync(
            SearchQueriesGeneratorAgentInput input,
            CancellationToken cancellationToken = default)
        {
            var conversationHistory = MessageSerializationUtils.SerializeConversationHistory(input.ContextMessages, input.UserLastRequest);
            var userMessage = $"Captured user intent:\n{input.UserIntent}\n\n{conversationHistory}";

            var inputMessages = new List<AgentMessage>
            {
                new() { Role = AgentMessageRole.System, Content = $"Today date is {DateTime.UtcNow:yyyy-MM-dd}." },
                new() { Role = AgentMessageRole.User, Content = userMessage },
            };

            var result = await ExecuteWithRetryAsync(inputMessages, cancellationToken);

            return new SearchQueriesGeneratorAgentOutput
            {
                MissingKnowledgeBaseSearchEntries = result.Result.MissingKnowledgeBaseSearchEntries,
                MissingPastMemories = result.Result.MissingPastMemories,
                InputTokenCount = result.InputTokenCount,
                OutputTokenCount = result.OutputTokenCount,
                TokenCount = result.TotalTokenCount
            };
        }

        protected override ParsedResponse ParseStructuredResponse(string rawResponseText)
        {
            try
            {
                var responseDTO = JsonSerializer.Deserialize<ParsedResponse>(rawResponseText);

                if (responseDTO == null)
                {
                    _logger.LogWarning("The model's response could not be deserialized into the expected format. Response text: {ResponseText}", rawResponseText);
                    throw new BadStructuredResponseException(rawResponseText, "The model's response could not be deserialized into the expected format.");
                }

                responseDTO.MissingPastMemories ??= [];
                responseDTO.MissingKnowledgeBaseSearchEntries ??= [];

                return responseDTO;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize the model's response. Response text: {ResponseText}", rawResponseText);
                throw new BadStructuredResponseException(rawResponseText, "Failed to parse the model's response.", ex);
            }
        }

        public class ParsedResponse
        {
            [JsonPropertyName("missingPastMemories")]
            public IEnumerable<string> MissingPastMemories { get; set; } = [];

            [JsonPropertyName("missingKnowledgeBaseSearchEntries")]
            public IEnumerable<SearchQueriesGeneratorAgentOutput.SearchQueriesGeneratorKnowledgeBase> MissingKnowledgeBaseSearchEntries { get; set; } = [];
        }
    }
}
