using AgentMesh.Application.Contracts;
using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Models.Agents;
using AgentMesh.Application.Models.Knowledge;
using AgentMesh.Application.Models.Parameters;
using AgentMesh.Application.Models.Workflows;
using AgentMesh.Application.Services.Helpers;
using AgentMesh.Application.Utils;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentMesh.Application.Services.Agents
{
    public sealed class KnowledgeRerankerAgent(
        IOpenAIClientFactory openAIClientFactory,
        Resilience resilience,
        ILogger<KnowledgeRerankerAgent> logger,
        IAgentInputSerializer agentInputSerializer) : AbstractAgent<KnowledgeRerankerResult>(logger,
            "KnowledgeReranker",
            openAIClientFactory,
            resilience,
            agentInputSerializer)
    {
        private readonly ILogger<KnowledgeRerankerAgent> _logger = logger;

        protected override IEnumerable<AgentInputParameterConfiguration> GetAgentInputParameterConfiguration()
        {
            return [
                new() { ParameterType = typeof(RequestDateTimeParameter), ParameterTags = [ParameterTags.AgentSystemParameterTag] },
                new() { ParameterType = typeof(KnowledgeQueryResultParameter), ParameterTags = [ParameterTags.AgentSystemParameterTag] }
                ];
        }

        protected override KnowledgeRerankerResult ParseStructuredResponse(string rawResponseText)
        {
            try
            {
                var responseDTO = JsonSerializer.Deserialize<ParsedResponse>(rawResponseText, AgentResponseJsonSerializationUtils.DefaultDeserializeOptions)
                    ?? throw new BadStructuredResponseException(rawResponseText, "The model's response could not be deserialized into knowledge reranker output.");

                if (responseDTO.EntityIds == null)
                {
                    throw new BadStructuredResponseException(rawResponseText, "The model's response did not contain the 'entityIds' property.");
                }

                if (responseDTO.RelationIds == null)
                {
                    throw new BadStructuredResponseException(rawResponseText, "The model's response did not contain the 'relationIds' property.");
                }

                if (responseDTO.ContentIds == null)
                {
                    throw new BadStructuredResponseException(rawResponseText, "The model's response did not contain the 'contentIds' property.");
                }

                return new KnowledgeRerankerResult
                {
                    EntityIds = responseDTO.EntityIds
                        .Where(id => !string.IsNullOrWhiteSpace(id))
                        .Select(id => id.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToArray(),
                    RelationIds = responseDTO.RelationIds
                        .Where(id => !string.IsNullOrWhiteSpace(id))
                        .Select(id => id.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToArray(),
                    ContentIds = responseDTO.ContentIds
                        .Where(contentId => !string.IsNullOrWhiteSpace(contentId))
                        .Select(contentId => contentId.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToArray()
                };
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize the knowledge reranker response. Response text: {ResponseText}", rawResponseText);
                throw new BadStructuredResponseException(rawResponseText, "Failed to parse the model's response.", ex);
            }
        }

        private sealed class ParsedResponse
        {
            [JsonPropertyName("entityIds")]
            public IEnumerable<string>? EntityIds { get; set; }

            [JsonPropertyName("relationIds")]
            public IEnumerable<string>? RelationIds { get; set; }

            [JsonPropertyName("contentIds")]
            public IEnumerable<string>? ContentIds { get; set; }
        }
    }
}
