using AgentMesh.Application.Contracts;
using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Models.Agents;
using AgentMesh.Application.Models.Parameters;
using AgentMesh.Application.Models.RequestAnalysis;
using AgentMesh.Application.Models.Workflows;
using AgentMesh.Application.Services.Helpers;
using AgentMesh.Application.Utils;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentMesh.Application.Services.Agents
{
    public sealed class RequestAnalyzerAgent(
        IOpenAIClientFactory openAIClientFactory,
        Resilience resilience,
        IAgentInputSerializer agentInputSerializer,
        ILogger<RequestAnalyzerAgent> logger) : AbstractAgent<StructuredUserRequest>(logger,
            "RequestAnalyzer",
            openAIClientFactory,
            resilience,
            agentInputSerializer)
    {
        private readonly ILogger<RequestAnalyzerAgent> _logger = logger;

        protected override IEnumerable<AgentInputParameterConfiguration> GetAgentInputParameterConfiguration()
        {
            return
            [
                new AgentInputParameterConfiguration
                {
                    ParameterType = typeof(RequestDateTimeParameter),
                    ParameterTags = [ParameterTags.AgentSystemParameterTag]
                }
            ];
        }

        protected override StructuredUserRequest ParseStructuredResponse(string rawResponseText)
        {
            try
            {
                var responseDTO = JsonSerializer.Deserialize<ParsedResponse>(rawResponseText, AgentResponseJsonSerializationUtils.DefaultDeserializeOptions);

                if (responseDTO == null)
                {
                    _logger.LogWarning("The model's response could not be deserialized into the expected format. Response text: {ResponseText}", rawResponseText);
                    throw new BadStructuredResponseException(rawResponseText, "The model's response could not be deserialized into the expected format.");
                }

                if (string.IsNullOrWhiteSpace(responseDTO.Intent))
                {
                    _logger.LogWarning("The model's response contains empty intent. Response text: {ResponseText}", rawResponseText);
                    throw new BadStructuredResponseException(rawResponseText, "The model's response contains empty intent.");
                }

                if (string.IsNullOrWhiteSpace(responseDTO.LanguageOfTheUser))
                {
                    _logger.LogWarning("The model's response contains empty language of the user. Response text: {ResponseText}", rawResponseText);
                    throw new BadStructuredResponseException(rawResponseText, "The model's response contains empty language of the user.");
                }

                if (!responseDTO.IsSmallTalk.HasValue)
                {
                    _logger.LogWarning("The model's response contains missing isSmallTalk flag. Response text: {ResponseText}", rawResponseText);
                    throw new BadStructuredResponseException(rawResponseText, "The model's response contains missing isSmallTalk flag.");
                }

                responseDTO.LanguageOfTheUser = responseDTO.LanguageOfTheUser.Trim();
                responseDTO.Intent = responseDTO.Intent.Trim();
                responseDTO.ConversationTopic = responseDTO.ConversationTopic?.Trim() ?? string.Empty;

                responseDTO.UserRequestedActions = responseDTO.UserRequestedActions
                    .Where(entry => !string.IsNullOrWhiteSpace(entry))
                    .Select(entry => entry.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                responseDTO.UserPreferences = responseDTO.UserPreferences
                    .Where(entry => !string.IsNullOrWhiteSpace(entry))
                    .Select(entry => entry.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                responseDTO.UserProvidedData = responseDTO.UserProvidedData
                    .Where(entry => !string.IsNullOrWhiteSpace(entry))
                    .Select(entry => entry.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                responseDTO.MissingValues = responseDTO.MissingValues
                    .Where(entry => !string.IsNullOrWhiteSpace(entry))
                    .Select(entry => entry.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                var ret = new StructuredUserRequest
                {
                    Intent = responseDTO.Intent,
                    ConversationTopic = responseDTO.ConversationTopic,
                    UserRequestedActions = responseDTO.UserRequestedActions.ToArray(),
                    UserPreferences = responseDTO.UserPreferences.ToArray(),
                    UserProvidedData = responseDTO.UserProvidedData.ToArray(),
                    MissingValues = responseDTO.MissingValues.ToArray(),
                    IsSmallTalk = responseDTO.IsSmallTalk.Value,
                    LanguageOfTheUser = responseDTO.LanguageOfTheUser
                };
                return ret;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize the model's response. Response text: {ResponseText}", rawResponseText);
                throw new BadStructuredResponseException(rawResponseText, "Failed to parse the model's response.", ex);
            }
        }

        private class ParsedResponse
        {
            [JsonPropertyName("intent")]
            public string Intent { get; set; } = string.Empty;

            [JsonPropertyName("conversationTopic")]
            public string ConversationTopic { get; set; } = string.Empty;

            [JsonPropertyName("userRequestedActions")]
            public IEnumerable<string> UserRequestedActions { get; set; } = [];

            [JsonPropertyName("userPreferences")]
            public IEnumerable<string> UserPreferences { get; set; } = [];

            [JsonPropertyName("userProvidedData")]
            public IEnumerable<string> UserProvidedData { get; set; } = [];

            [JsonPropertyName("missingValues")]
            public IEnumerable<string> MissingValues { get; set; } = [];

            [JsonPropertyName("isSmallTalk")]
            public bool? IsSmallTalk { get; set; }

            [JsonPropertyName("languageOfTheUser")]
            public string LanguageOfTheUser { get; set; } = string.Empty;
        }
    }
}

