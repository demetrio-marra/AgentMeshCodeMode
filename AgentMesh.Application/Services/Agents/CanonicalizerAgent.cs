using AgentMesh.Application.Contracts;
using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Models.Agents;
using AgentMesh.Application.Models.Parameters;
using AgentMesh.Application.Models.RequestAnalysis;
using AgentMesh.Application.Models.Workflows;
using AgentMesh.Application.Services.Helpers;
using AgentMesh.Application.Utils;
using AgentMesh.Models.RequestAnalysis;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentMesh.Application.Services.Agents
{
    public sealed class CanonicalizerAgent(
        IOpenAIClientFactory openAIClientFactory,
        Resilience resilience,
        IAgentInputSerializer agentInputSerializer,
        ILogger<CanonicalizerAgent> logger) : AbstractAgent<StructuredUserRequest>(logger,
            "RequestCanonicalization",
            openAIClientFactory,
            resilience,
            agentInputSerializer)
    {
        private readonly ILogger<CanonicalizerAgent> _logger = logger;

        protected override IEnumerable<AgentInputParameterConfiguration> GetAgentInputParameterConfiguration()
        {
            return [
                new()
                {
                    ParameterType = typeof(KnowledgeQueryResultParameter),
                    ParameterTags = [ParameterTags.AgentSystemParameterTag]
                }
            ];
        }

        protected override StructuredUserRequest ParseStructuredResponse(string rawResponseText)
        {
            try
            {
                var responseDto = JsonSerializer.Deserialize<CanonicalizerResponseDto>(rawResponseText, AgentResponseJsonSerializationUtils.DefaultDeserializeOptions);

                if (responseDto == null)
                {
                    _logger.LogWarning("The model's response could not be deserialized into the expected format. Response text: {ResponseText}", rawResponseText);
                    throw new BadStructuredResponseException(rawResponseText, "The model's response could not be deserialized into the expected format.");
                }

                if (string.IsNullOrWhiteSpace(responseDto.Intent))
                {
                    _logger.LogWarning("The model's response contains empty intent. Response text: {ResponseText}", rawResponseText);
                    throw new BadStructuredResponseException(rawResponseText, "The model's response contains empty intent.");
                }

                if (string.IsNullOrWhiteSpace(responseDto.CanonicalizedIntentCategoryRaw))
                {
                    _logger.LogWarning("The model's response contains empty canonicalized intent category. Response text: {ResponseText}", rawResponseText);
                    throw new BadStructuredResponseException(rawResponseText, "The model's response contains empty canonicalized intent category.");
                }

                responseDto.Intent = responseDto.Intent.Trim();
                responseDto.ConversationTopic = string.IsNullOrWhiteSpace(responseDto.ConversationTopic)
                    ? null
                    : responseDto.ConversationTopic.Trim();

                responseDto.UserMentionedEntities = NormalizeList(responseDto.UserMentionedEntities);
                responseDto.UserProvidedData = NormalizeList(responseDto.UserProvidedData);
                responseDto.UserPreferences = NormalizeList(responseDto.UserPreferences);

                var ret = new StructuredUserRequest
                {
                    Intent = responseDto.Intent,
                    IntentCategory = ParseIntentCategory(responseDto.CanonicalizedIntentCategoryRaw),
                    ConversationTopic = responseDto.ConversationTopic,
                    UserMentionedEntities = responseDto.UserMentionedEntities,
                    UserProvidedData = responseDto.UserProvidedData,
                    UserPreferences = responseDto.UserPreferences
                };

                return ret;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize the model's response. Response text: {ResponseText}", rawResponseText);
                throw new BadStructuredResponseException(rawResponseText, "Failed to parse the model's response.", ex);
            }
        }

        private static UserIntentCategory ParseIntentCategory(string intentCategory)
        {
            if (Enum.TryParse<UserIntentCategory>(intentCategory, true, out var parsedIntentCategory))
            {
                return parsedIntentCategory;
            }

            throw new BadStructuredResponseException(intentCategory, $"Unknown intent category: {intentCategory}");
        }

        private static IEnumerable<string> NormalizeList(IEnumerable<string>? values)
        {
            return (values ?? [])
                .Where(entry => !string.IsNullOrWhiteSpace(entry))
                .Select(entry => entry.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private class CanonicalizerResponseDto
        {
            [JsonPropertyName("intent")]
            public string Intent { get; set; } = string.Empty;

            [JsonPropertyName("conversationTopic")]
            public string? ConversationTopic { get; set; }

            [JsonPropertyName("userMentionedEntities")]
            public IEnumerable<string>? UserMentionedEntities { get; set; }

            [JsonPropertyName("userProvidedData")]
            public IEnumerable<string>? UserProvidedData { get; set; }

            [JsonPropertyName("userPreferences")]
            public IEnumerable<string>? UserPreferences { get; set; }

            [JsonPropertyName("canonicalizedIntentCategory")]
            public string CanonicalizedIntentCategoryRaw { get; set; } = string.Empty;
        }
    }
}
