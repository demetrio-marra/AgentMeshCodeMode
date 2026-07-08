using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Models;
using AgentMesh.Models.IntentCanonicalization;
using AgentMesh.Models.IntentExtractor;
using AgentMesh.Models.KnowledgeBase;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentMesh.Application.Services
{
    public class IntentCanonicalizationAgent(
        [FromKeyedServices(IntentCanonicalizationAgentConfiguration.AgentName)] IOpenAIClient openAIClient,
        Resilience resilience,
        ILogger<IntentCanonicalizationAgent> logger) : AgentBase<IntentCanonicalizationAgent.ParsedResponse>(logger, IntentCanonicalizationAgentConfiguration.AgentName, openAIClient, resilience), IIntentCanonicalizationAgent
    {
        private readonly ILogger<IntentCanonicalizationAgent> _logger = logger;
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

        public async Task<IntentCanonicalizationAgentOutput> ExecuteAsync(IntentCanonicalizationAgentInput input, CancellationToken cancellationToken = default)
        {
            var entitiesByDomainText = input.EntitiesByDomain.Any()
                ? string.Join("\n", input.EntitiesByDomain.SelectMany(kvp => kvp.Value.Select(entity => $"- [{kvp.Key}] {entity}")))
                : "(No entities by domain)";

            var supportingIntentInformationText = input.SupportingIntentInformation.Any()
                ? string.Join("\n", input.SupportingIntentInformation.Select(i => $"- {i}"))
                : "(No supporting intent information)";

            var nonCanonicalizedQueriesText = input.NonCanonicalizedQueries.Any()
                ? string.Join("\n", input.NonCanonicalizedQueries.Select(query => $"- Type: {ToQueryType(query.SearchType)}, Query: {query.Query}"))
                : "(No non-canonicalized queries)";

            var domainDocuments = input.DomainDocumentationContents;

            var userMessage = $"""
Captured user intent:
{input.Intent}

Captured user intent category:
{input.UserIntentCategory}

Entities by domain:
{entitiesByDomainText}

Supporting intent information:
{supportingIntentInformationText}

Domains knowledge base documents:
{domainDocuments}

Non-canonicalized queries for API search:
{nonCanonicalizedQueriesText}

Language of Knowledge Base:
{(string.IsNullOrWhiteSpace(input.LanguageOfKnowledgeBase) ? "(Not provided)" : input.LanguageOfKnowledgeBase)}
""";

            var inputMessages = new List<AgentMessage>
            {
                new() { Role = AgentMessageRole.System, Content = $"Today date is {DateTime.UtcNow:yyyy-MM-dd}." },
                new() { Role = AgentMessageRole.User, Content = userMessage }
            };

            var result = await ExecuteWithRetryAsync(inputMessages, cancellationToken);

            return new IntentCanonicalizationAgentOutput
            {
                DomainedIntent = result.Result.DomainedIntent,
                CanonicalizedIntentCategory = result.Result.CanonicalizedIntentCategory,
                CanonicalizedAPIQueries = result.Result.CanonicalizedAPIQueries.Select(TranslateKnowledgeBaseQuery).ToList(),
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

                if (string.IsNullOrWhiteSpace(parsedResponse.DomainedIntent))
                {
                    _logger.LogWarning("The model's response contains empty canonicalized intent. Response text: {ResponseText}", rawResponseText);
                    throw new BadStructuredResponseException(rawResponseText, "The model's response contains empty canonicalized intent.");
                }

                if (string.IsNullOrWhiteSpace(parsedResponse.CanonicalizedIntentCategoryRaw))
                {
                    _logger.LogWarning("The model's response contains empty canonicalized intent category. Response text: {ResponseText}", rawResponseText);
                    throw new BadStructuredResponseException(rawResponseText, "The model's response contains empty canonicalized intent category.");
                }

                parsedResponse.CanonicalizedAPIQueries ??= [];
                parsedResponse.DomainedIntent = parsedResponse.DomainedIntent.Trim();
                parsedResponse.CanonicalizedIntentCategory = ParseUserIntentCategory(parsedResponse.CanonicalizedIntentCategoryRaw, rawResponseText);

                return parsedResponse;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize the model's response. Response text: {ResponseText}", rawResponseText);
                throw new BadStructuredResponseException(rawResponseText, "Failed to parse the model's response.", ex);
            }
        }

        private static UserIntentCategoryValues ParseUserIntentCategory(string userIntentCategory, string rawOutput)
        {
            if (Enum.TryParse<UserIntentCategoryValues>(userIntentCategory, true, out var parsedCategory))
            {
                return parsedCategory;
            }

            throw new BadStructuredResponseException(rawOutput, $"Unknown user intent category: {userIntentCategory}");
        }

        public class ParsedResponse
        {
            [JsonPropertyName("domainedIntent")]
            public string DomainedIntent { get; set; } = string.Empty;

            [JsonPropertyName("canonicalizedIntentCategory")]
            public string CanonicalizedIntentCategoryRaw { get; set; } = string.Empty;

            [JsonPropertyName("canonicalizedAPIQueries")]
            public IEnumerable<QueryItem> CanonicalizedAPIQueries { get; set; } = [];

            [JsonIgnore]
            public UserIntentCategoryValues CanonicalizedIntentCategory { get; set; }
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
