using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Models;
using AgentMesh.Models.KnowledgeBase;
using AgentMesh.Models.QueryExpander;
using AgentMesh.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentMesh.Application.Services
{
    public class QueryExpanderAgent(
        [FromKeyedServices(QueryExpanderAgentConfiguration.AgentName)] IOpenAIClient openAIClient,
        Resilience resilience,
        ILogger<QueryExpanderAgent> logger) : AgentBase<QueryExpanderAgent.ParsedResponse>(logger, QueryExpanderAgentConfiguration.AgentName, openAIClient, resilience), IQueryExpanderAgent
    {
        private readonly ILogger<QueryExpanderAgent> _logger = logger;
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

        public async Task<QueryExpanderAgentOutput> ExecuteAsync(
            QueryExpanderAgentInput input,
            CancellationToken cancellationToken = default)
        {
            var userRequestedActionsText = input.StructuredUserRequest.UserRequestedActions.Any()
                ? string.Join("\n", input.StructuredUserRequest.UserRequestedActions.Select(action => $"- {action}"))
                : "(No requested actions)";

            var userProvidedDataText = input.StructuredUserRequest.UserProvidedData.Any()
                ? string.Join("\n", input.StructuredUserRequest.UserProvidedData.Select(data => $"- {data}"))
                : "(No provided data)";

            var userPreferencesText = input.StructuredUserRequest.UserPreferences.Any()
                ? string.Join("\n", input.StructuredUserRequest.UserPreferences.Select(preference => $"- {preference}"))
                : "(No user preferences)";

            var missingValuesText = input.StructuredUserRequest.MissingValues.Any()
                ? string.Join("\n", input.StructuredUserRequest.MissingValues.Select(value => $"- {value}"))
                : "(No missing values)";

            var userMessage = $"""
User intent:
{input.StructuredUserRequest.Intent}

Conversation topic:
{(string.IsNullOrWhiteSpace(input.StructuredUserRequest.ConversationTopic) ? "(Not specified)" : input.StructuredUserRequest.ConversationTopic)}

Requested actions:
{userRequestedActionsText}

Provided data:
{userProvidedDataText}

User preferences:
{userPreferencesText}

Missing values:
{missingValuesText}

User language:
{input.StructuredUserRequest.LanguageOfTheUser}
""";

            var inputMessages = new List<AgentMessage>
            {
                new() { Role = AgentMessageRole.System, Content = $"Today date is {DateTime.UtcNow:yyyy-MM-dd}." },
            };

            if (input.GenerateHydeQueries)
            {
                inputMessages.Add(new AgentMessage
                {
                    Role = AgentMessageRole.System,
                    Content = "You are allowed to generate hypothetical document (HyDE) queries. Use them only when necessary."
                });
            }
            else
            {
                inputMessages.Add(new AgentMessage
                {
                    Role = AgentMessageRole.System,
                    Content = "You are NOT allowed to generate hypothetical document (HyDE) queries. Do not generate them under any circumstances."
                });
            }

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

            return new QueryExpanderAgentOutput
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
