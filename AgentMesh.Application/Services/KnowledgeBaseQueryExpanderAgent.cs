using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Models;
using AgentMesh.Application.Utils;
using AgentMesh.Models.KnowledgeBase;
using AgentMesh.Models.KnowledgeBaseQueryExpander;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentMesh.Application.Services
{
    public class KnowledgeBaseQueryExpanderAgent(
        [FromKeyedServices(KnowledgeBaseQueryExpanderAgentConfiguration.AgentName)] IOpenAIClient openAIClient,
        Resilience resilience,
        ILogger<KnowledgeBaseQueryExpanderAgent> logger) : AgentBase<KnowledgeBaseQueryExpanderAgent.ParsedResponse>(logger, KnowledgeBaseQueryExpanderAgentConfiguration.AgentName, openAIClient, resilience)
    {
        private readonly ILogger<KnowledgeBaseQueryExpanderAgent> _logger = logger;
        private static readonly string[] AllowedQueryTypes = ["lex", "vec", "hyde"];

        private static KnowledgeBaseQueryInputItem TranslateKnowledgeBaseQuery(QueryItem query)
        {
            var normalizedType = AllowedQueryTypes.FirstOrDefault(type => type.Equals(query.Type, StringComparison.OrdinalIgnoreCase));

            if (normalizedType == null)
            {
                throw new ArgumentOutOfRangeException(nameof(query.Type), query.Type, $"Unsupported query type. Allowed values: {string.Join(", ", AllowedQueryTypes)}");
            }

            var searchType = normalizedType switch
            {
                "lex" => KnowledgeBaseQuerySearchType.Keyword,
                "vec" => KnowledgeBaseQuerySearchType.Semantic,
                "hyde" => KnowledgeBaseQuerySearchType.HypotheticalDocument,
                _ => throw new ArgumentOutOfRangeException(nameof(query.Type), query.Type, $"Unsupported query type. Allowed values: {string.Join(", ", AllowedQueryTypes)}")
            };

            return new KnowledgeBaseQueryInputItem
            {
                Query = query.Query,
                SearchType = searchType
            };
        }

        public async Task<KnowledgeBaseQueryExpanderAgentOutput> ExecuteAsync(
            KnowledgeBaseQueryExpanderAgentInput input,
            CancellationToken cancellationToken = default)
        {
            var userPayload = new
            {
                intent = input.StructuredUserRequest.Intent,
                conversationTopic = input.StructuredUserRequest.ConversationTopic,
                userRequestedActions = input.StructuredUserRequest.UserRequestedActions,
                userProvidedData = input.StructuredUserRequest.UserProvidedData,
                userPreferences = input.StructuredUserRequest.UserPreferences,
                missingValues = input.StructuredUserRequest.MissingValues,
                languageOfTheUser = input.StructuredUserRequest.LanguageOfTheUser
            };

            var systemMessages = new List<string>
            {
                $"Today date is {DateTime.UtcNow:yyyy-MM-dd}.",
                input.GenerateHydeQueries
                    ? "You are allowed to generate hypothetical document (HyDE) queries. Use them only when necessary."
                    : "You are NOT allowed to generate hypothetical document (HyDE) queries. Do not generate them under any circumstances.",
                $"This is the documentation queries generation reference:\n{input.DocumentationQueriesGenerationReference}"
            };

            var inputMessages = new List<AgentMessage>
            {
                new() { Role = AgentMessageRole.System, Content = string.Join(Environment.NewLine + Environment.NewLine, systemMessages) },
                new() { Role = AgentMessageRole.User, Content = JsonSerializer.Serialize(userPayload, AgentResponseJsonSerializationUtils.DefaultSerializeOptions) }
            };

            var result = await ExecuteWithRetryAsync(inputMessages, cancellationToken);

            return new KnowledgeBaseQueryExpanderAgentOutput
            {
                SearchQueries = result.Result.SearchQueries.Select(TranslateKnowledgeBaseQuery).ToList(),
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

                responseDTO.SearchQueries ??= [];

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
            [JsonPropertyName("searchQueries")]
            public IEnumerable<QueryItem> SearchQueries { get; set; } = [];
        }

        public class QueryItem
        {
            [JsonPropertyName("type")]
            public string Type { get; set; } = string.Empty;

            [JsonPropertyName("query")]
            public string Query { get; set; } = string.Empty;

            public override string ToString()
            {
                return $"Type: {Type}, Query: {Query}";
            }
        }
    }
}
