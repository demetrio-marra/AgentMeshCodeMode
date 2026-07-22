using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Models;
using AgentMesh.Application.Utils;
using AgentMesh.Application.Models.RequestCanonicalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using AgentMesh.Models.RequestCanonicalization;
using AgentMesh.Models.RequestAnalysis;
using AgentMesh.Models.KnowledgeBase;

namespace AgentMesh.Application.Services
{
    public class RequestCanonicalizationAgent(
        [FromKeyedServices(RequestCanonicalizationAgentConfiguration.AgentName)] IOpenAIClient openAIClient,
        Resilience resilience,
        ILogger<RequestCanonicalizationAgent> logger) : AgentBase<RequestCanonicalizationAgent.ParsedResponse>(logger, RequestCanonicalizationAgentConfiguration.AgentName, openAIClient, resilience)
    {
        private readonly ILogger<RequestCanonicalizationAgent> _logger = logger;
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

        private static string ToQueryType(KnowledgeBaseQuerySearchType searchType)
            => searchType switch
            {
                KnowledgeBaseQuerySearchType.Keyword => "lex",
                KnowledgeBaseQuerySearchType.Semantic => "vec",
                KnowledgeBaseQuerySearchType.HypotheticalDocument => "hyde",
                _ => throw new ArgumentOutOfRangeException(nameof(searchType), searchType, "Unsupported query search type.")
            };

        public async Task<RequestCanonicalizationAgentOutput> ExecuteAsync(RequestCanonicalizationAgentInput input, CancellationToken cancellationToken = default)
        {
            var systemMessages = new List<string>
            {
                $"Today date is {DateTime.UtcNow:yyyy-MM-dd}.",
                $"Knowledge base language is: {input.LanguageOfKnowledgeBase}.",
            };

            if (!string.IsNullOrWhiteSpace(input.DomainsKnowledgeBaseDocumentsContent))
            {
                systemMessages.Add($"Knowledge base documents content:\n{input.DomainsKnowledgeBaseDocumentsContent}");
            }

            systemMessages.Add($"This is the documentation queries generation reference:\n{input.DocumentationQueriesGenerationReference}");


            var req = input.StructuredUserRequest;

            var userPayload = new
            {
                req.Intent,
                req.ConversationTopic,
                req.UserRequestedActions,
                req.UserProvidedData,
                req.UserPreferences,
                req.MissingValues,
                req.LanguageOfTheUser,

                domainsKnowledgeBaseQuery = input.DomainsKnowledgeBaseQuery.Select(q => new
                {
                    type = ToQueryType(q.SearchType),
                    q.Query
                })
            };

            var inputMessages = new List<AgentMessage>
            {
                new() { Role = AgentMessageRole.System, Content = string.Join(Environment.NewLine + Environment.NewLine, systemMessages) },
                new() { Role = AgentMessageRole.User, Content = JsonSerializer.Serialize(userPayload, AgentResponseJsonSerializationUtils.DefaultSerializeOptions) }
            };

            var result = await ExecuteWithRetryAsync(inputMessages, cancellationToken);

            return new RequestCanonicalizationAgentOutput
            {
                CanonicalizedStructuredUserRequest = new AgentMesh.Models.RequestAnalysis.StructuredUserRequest
                {
                    Intent = result.Result.Intent,
                    ConversationTopic = result.Result.ConversationTopic,
                    UserRequestedActions = result.Result.UserRequestedActions,
                    UserProvidedData = result.Result.UserProvidedData,
                    UserPreferences = result.Result.UserPreferences,
                    MissingValues = req.MissingValues, // from request
                    LanguageOfTheUser = req.LanguageOfTheUser // from request
                },
                CanonicalizedIntentCategory = result.Result.CanonicalizedIntentCategory,
                CanonicalizedDomainsKnowledgeBaseQuery = result.Result.CanonicalizedQueries.Select(TranslateKnowledgeBaseQuery).ToList(),
                TokenCount = result.TotalTokenCount,
                InputTokenCount = result.InputTokenCount,
                OutputTokenCount = result.OutputTokenCount
            };
        }

        protected override ParsedResponse ParseStructuredResponse(string rawResponseText)
        {
            if (string.IsNullOrWhiteSpace(rawResponseText))
            {
                throw new EmptyAgentResponseException();
            }

            try
            {
                var parsedResponse = JsonSerializer.Deserialize<ParsedResponse>(rawResponseText);
                if (parsedResponse == null)
                {
                    _logger.LogWarning("The model's response could not be deserialized into the expected format. Response text: {ResponseText}", rawResponseText);
                    throw new BadStructuredResponseException(rawResponseText, "The model's response could not be deserialized into the expected format.");
                }

                if (string.IsNullOrWhiteSpace(parsedResponse.Intent))
                {
                    _logger.LogWarning("The model's response contains empty canonicalized intent. Response text: {ResponseText}", rawResponseText);
                    throw new BadStructuredResponseException(rawResponseText, "The model's response contains empty canonicalized intent.");
                }

                if (string.IsNullOrWhiteSpace(parsedResponse.CanonicalizedIntentCategoryRaw))
                {
                    _logger.LogWarning("The model's response contains empty canonicalized intent category. Response text: {ResponseText}", rawResponseText);
                    throw new BadStructuredResponseException(rawResponseText, "The model's response contains empty canonicalized intent category.");
                }

                parsedResponse.CanonicalizedQueries ??= [];
                parsedResponse.Intent = parsedResponse.Intent.Trim();
                parsedResponse.CanonicalizedIntentCategory = ParseUserIntentCategory(parsedResponse.CanonicalizedIntentCategoryRaw, rawResponseText);

                return parsedResponse;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize the model's response. Response text: {ResponseText}", rawResponseText);
                throw new BadStructuredResponseException(rawResponseText, "Failed to parse the model's response.", ex);
            }
        }

        private static UserIntentCategory ParseUserIntentCategory(string userIntentCategory, string rawOutput)
        {
            if (Enum.TryParse<UserIntentCategory>(userIntentCategory, true, out var parsedCategory))
            {
                return parsedCategory;
            }

            throw new BadStructuredResponseException(rawOutput, $"Unknown user intent category: {userIntentCategory}");
        }

        public class ParsedResponse
        {
            [JsonPropertyName("intent")]
            public string Intent { get; set; } = string.Empty;

            [JsonPropertyName("conversationTopic")]
            public string? ConversationTopic { get; set; }

            [JsonPropertyName("userRequestedActions")]
            public IEnumerable<string> UserRequestedActions { get; set; } = [];

            [JsonPropertyName("userProvidedData")]
            public IEnumerable<string> UserProvidedData { get; set; } = [];

            [JsonPropertyName("userPreferences")]
            public IEnumerable<string> UserPreferences { get; set; } = [];

            [JsonPropertyName("canonicalizedIntentCategory")]
            public string CanonicalizedIntentCategoryRaw { get; set; } = string.Empty;

            [JsonPropertyName("canonicalizedQueries")]
            public IEnumerable<QueryItem> CanonicalizedQueries { get; set; } = [];

            [JsonPropertyName("canonicalizedIntentCategory")]
            public UserIntentCategory CanonicalizedIntentCategory { get; set; }
        }

        public class QueryItem
        {
            [JsonPropertyName("type")]
            public string Type { get; set; } = string.Empty;

            [JsonPropertyName("query")]
            public string Query { get; set; } = string.Empty;
        }
    }
}
