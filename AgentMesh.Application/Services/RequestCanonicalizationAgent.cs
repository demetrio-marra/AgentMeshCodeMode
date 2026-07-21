using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Models;
using AgentMesh.Models.KnowledgeBase;
using AgentMesh.Models.RequestAnalysis;
using AgentMesh.Models.RequestCanonicalization;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentMesh.Application.Services
{
    public class RequestCanonicalizationAgent(
        [FromKeyedServices(RequestCanonicalizationAgentConfiguration.AgentName)] IOpenAIClient openAIClient,
        Resilience resilience,
        ILogger<RequestCanonicalizationAgent> logger) : AgentBase<RequestCanonicalizationAgent.ParsedResponse>(logger, RequestCanonicalizationAgentConfiguration.AgentName, openAIClient, resilience), IRequestCanonicalizationAgent
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
            var req = input.StructuredUserRequest;

            var userRequestedActionsText = req.UserRequestedActions.Any()
                ? string.Join("\n", req.UserRequestedActions.Select(a => $"- {a}"))
                : "(None)";

            var userProvidedDataText = req.UserProvidedData.Any()
                ? string.Join("\n", req.UserProvidedData.Select(d => $"- {d}"))
                : "(None)";

            var userPreferencesText = req.UserPreferences.Any()
                ? string.Join("\n", req.UserPreferences.Select(p => $"- {p}"))
                : "(None)";

            var missingValuesText = req.MissingValues.Any()
                ? string.Join("\n", req.MissingValues.Select(m => $"- {m}"))
                : "(None)";

            var queriesText = input.DomainsKnowledgeBaseQuery.Any()
                ? string.Join("\n", input.DomainsKnowledgeBaseQuery.Select(q => $"- Type: {ToQueryType(q.SearchType)}, Query: {q.Query}"))
                : "(None)";

            var userMessage = $"""
Structured user request:
Intent: {req.Intent}
ConversationTopic: {req.ConversationTopic ?? "(None)"}
UserRequestedActions:
{userRequestedActionsText}
UserProvidedData:
{userProvidedDataText}
UserPreferences:
{userPreferencesText}

Non-canonicalized domain knowledge base queries:
{queriesText}
""";

            var knowledgeBaseMessage = $"""
Domains knowledge base documents:
{(string.IsNullOrWhiteSpace(input.DomainsKnowledgeBaseDocumentsContent) ? "(No knowledge base results)" : input.DomainsKnowledgeBaseDocumentsContent)}

Language of Knowledge Base:
{(string.IsNullOrWhiteSpace(input.LanguageOfKnowledgeBase) ? "(Not provided)" : input.LanguageOfKnowledgeBase)}
""";

            var inputMessages = new List<AgentMessage>
            {
                new() { Role = AgentMessageRole.System, Content = $"Today date is {DateTime.UtcNow:yyyy-MM-dd}." },
                new() { Role = AgentMessageRole.System, Content = knowledgeBaseMessage }
            };

            if (!string.IsNullOrWhiteSpace(input.QmdQueryTypesReference))
            {
                inputMessages.Add(new AgentMessage
                {
                    Role = AgentMessageRole.System,
                    Content = $"QMD query types reference:\n{input.QmdQueryTypesReference}"
                });
            }

            inputMessages.Add(new AgentMessage { Role = AgentMessageRole.User, Content = userMessage });

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
