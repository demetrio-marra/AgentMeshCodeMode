using AgentMesh.Application.Contracts;
using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Models.Agents;
using AgentMesh.Application.Models.Knowledge;
using AgentMesh.Application.Models.Parameters;
using AgentMesh.Application.Models.Workflows;
using AgentMesh.Application.Services.Helpers;
using AgentMesh.Application.Utils;
using AgentMesh.Utils;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentMesh.Application.Services.Agents
{
    public sealed class KnowledgeQueryBuilderForCoderAgent(
        AgentMesh.Application.Contracts.IOpenAIClientFactory openAIClientFactory,
        Resilience resilience,
        ILogger<KnowledgeQueryBuilderForCoderAgent> logger,
        IAgentInputSerializer agentInputSerializer) : AbstractAgent<KnowledgeQuery>(
            logger,
            "KnowledgeQueryBuilderForCoder",
            openAIClientFactory,
            resilience,
            agentInputSerializer)
    {
        private readonly ILogger<KnowledgeQueryBuilderForCoderAgent> _logger = logger;

        protected override IEnumerable<AgentInputParameterConfiguration> GetAgentInputParameterConfiguration()
        {
            return
            [
                new()
                {
                    ParameterType = typeof(RequestDateTimeParameter),
                    ParameterTags = [ParameterTags.AgentSystemParameterTag]
                }
            ];
        }

        protected override KnowledgeQuery ParseStructuredResponse(string rawResponseText)
        {
            try
            {
                var response = JsonSerializer.Deserialize<ParsedResponse>(rawResponseText, SerializationUtils.DefaultDeserializeOptions);

                if (response == null)
                {
                    _logger.LogWarning("The model's response could not be deserialized into the expected format. Response text: {ResponseText}", rawResponseText);
                    throw new BadStructuredResponseException(rawResponseText, "The model's response could not be deserialized into the expected format.");
                }

                if (string.IsNullOrWhiteSpace(response.QueryText))
                {
                    _logger.LogWarning("The model's response contains an empty query text. Response text: {ResponseText}", rawResponseText);
                    throw new BadStructuredResponseException(rawResponseText, "The model's response contains an empty query text.");
                }

                return new KnowledgeQuery
                {
                    QueryText = response.QueryText,
                    PrimaryRelevanceKeywords = response.PrimaryRelevanceKeywords ?? [],
                    SecondaryRelevanceKeywords = response.SecondaryRelevanceKeywords ?? []
                };
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize the model's response. Response text: {ResponseText}", rawResponseText);
                throw new BadStructuredResponseException(rawResponseText, "Failed to parse the model's response.", ex);
            }
        }

        private sealed class ParsedResponse
        {
            [JsonPropertyName("queryText")]
            public string QueryText { get; set; } = string.Empty;

            [JsonPropertyName("primaryRelevanceKeywords")]
            public IEnumerable<string>? PrimaryRelevanceKeywords { get; set; }

            [JsonPropertyName("secondaryRelevanceKeywords")]
            public IEnumerable<string>? SecondaryRelevanceKeywords { get; set; }
        }
    }
}
