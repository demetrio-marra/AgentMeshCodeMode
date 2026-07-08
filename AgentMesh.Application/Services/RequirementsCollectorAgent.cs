using AgentMesh.Application.Contracts;
using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Models;
using AgentMesh.Models.KnowledgeBase;
using AgentMesh.Models.RequirementsCollector;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentMesh.Application.Services
{
    public class RequirementsCollectorAgent(
        [FromKeyedServices(RequirementsCollectorAgentConfiguration.AgentName)] IOpenAIClient openAIClient,
        Resilience resilience,
        ILogger<RequirementsCollectorAgent> logger) : AgentBase<RequirementsCollectorAgent.ParsedResponse>(logger, RequirementsCollectorAgentConfiguration.AgentName, openAIClient, resilience), IRequirementsCollectorAgent
    {
        private readonly ILogger<RequirementsCollectorAgent> _logger = logger;
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

        public async Task<RequirementsCollectorAgentOutput> ExecuteAsync(
            RequirementsCollectorAgentInput input,
            CancellationToken cancellationToken = default)
        {
            var entitiesByDomainText = input.EntitiesByDomain.Any()
                ? string.Join("\n", input.EntitiesByDomain.SelectMany(kvp => kvp.Value.Select(entity => $"- [{kvp.Key}] {entity}")))
                : "(No entities)";

            var supportingIntentInformationText = input.SupportingIntentInformation.Any()
                ? string.Join("\n", input.SupportingIntentInformation.Select(info => $"- {info}"))
                : "(No supporting intent information)";

            var userPreferencesText = input.UserPreferences.Any()
                ? string.Join("\n", input.UserPreferences.Select(preference => $"- {preference}"))
                : "(No user preferences)";

            var missingMemoriesText = input.MissingMemories.Any()
                ? string.Join("\n", input.MissingMemories.Select(memory => $"- {memory}"))
                : "(No missing memories extracted)";

            var fastKnowledgeBaseEntriesText = input.FastKnowledgeBaseQueryResults.Any()
                ? string.Join("\n", input.FastKnowledgeBaseQueryResults.Select(entry =>
                    $"- Id: {entry.Id}; Title: {entry.Title}; File: {entry.File}; Relevance: {(entry.Relevance.HasValue ? entry.Relevance.Value.ToString("0.####") : "n/a")}; Summary: {entry.Summary ?? "n/a"}"))
                : "(No fast knowledge base entries)";

            var userMessage = $"""
Captured user intent:
{input.UserIntent}

Captured user intent category:
{input.UserIntentCategory}

Captured entities by domain:
{entitiesByDomainText}

Supporting intent information:
{supportingIntentInformationText}

User preferences:
{userPreferencesText}

Missing memories:
{missingMemoriesText}

Fast Knowledge Base results:
{fastKnowledgeBaseEntriesText}

Language of Knowledge Base:
{(string.IsNullOrWhiteSpace(input.LanguageOfKnowledgeBase) ? "(Not provided)" : input.LanguageOfKnowledgeBase)}
""";

            var inputMessages = new List<AgentMessage>
            {
                new() { Role = AgentMessageRole.System, Content = $"Today date is {DateTime.UtcNow:yyyy-MM-dd}." },
                new() { Role = AgentMessageRole.User, Content = userMessage },
            };

            var result = await ExecuteWithRetryAsync(inputMessages, cancellationToken);

            var shouldCreateMem0Queries = !input.UserPreferences.Any() || input.MissingMemories.Any();

            return new RequirementsCollectorAgentOutput
            {
                MissingKnowledgeBaseSearchEntries = result.Result.MissingKnowledgeBaseSearchEntries.Select(TranslateKnowledgeBaseQuery).ToList(),
                MissingPastMemories = shouldCreateMem0Queries ? result.Result.MissingPastMemories : [],
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
            public IEnumerable<QueryItem> MissingKnowledgeBaseSearchEntries { get; set; } = [];
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
