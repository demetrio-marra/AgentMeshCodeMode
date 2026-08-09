using AgentMesh.Application.Configuration;
using AgentMesh.Application.Contracts;
using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Models.ChatMessages;
using AgentMesh.Application.Models.TechnicalAnalyst;
using AgentMesh.Application.Utils;
using AgentMesh.Models.Workflows;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentMesh.Application.Services
{
    public class TechnicalAnalystAgent(
        IOpenAIClientFactory openAIClientFactory,
        Resilience resilience,
        ILogger<TechnicalAnalystAgent> logger) : AgentBase<TechnicalAnalystAgent.ParsedResponse>(logger, TechnicalAnalystAgentConfiguration.AgentName, openAIClientFactory, resilience)
    {
        private readonly ILogger<TechnicalAnalystAgent> _logger = logger;

        public async Task<TechnicalAnalystAgentOutput> ExecuteAsync(
            TechnicalAnalystAgentInput input,
            CancellationToken cancellationToken = default)
        {
            var userPayload = new
            {
                input.Intent,
                input.ConversationTopic,
                input.UserRequestedActions,
                input.UserProvidedData,
                input.UserPreferences,
                input.AgentMemories,
                input.BusinessRequirements,
                input.KnowledgeBaseDocumentsContent
            };

            var inputMessages = new List<AgentMessage>
            {
                new() { Role = AgentMessageRole.System, Content = $"Today date is {DateTime.UtcNow:yyyy-MM-dd}." },
                new() { Role = AgentMessageRole.User, Content = JsonSerializer.Serialize(userPayload, AgentResponseJsonSerializationUtils.DefaultSerializeOptions) }
            };

            var result = await ExecuteWithRetryAsync(inputMessages, cancellationToken);

            return new TechnicalAnalystAgentOutput
            {
                TechnicalSpecification = result.Result.TechnicalSpecification,
                RequestRejected = result.Result.RequestRejected,
                ReasonOfRejection = result.Result.ReasonOfRejection,
                SelectedAPIsFileLocations = result.Result.SelectedAPIsFileLocations,
                TokenCount = result.TotalTokenCount,
                InputTokenCount = result.InputTokenCount,
                OutputTokenCount = result.OutputTokenCount
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

                if (!responseDTO.RequestRejected && string.IsNullOrWhiteSpace(responseDTO.TechnicalSpecification))
                {
                    _logger.LogWarning("The model's response contains an empty technical specification for a non-rejected request. Response text: {ResponseText}", rawResponseText);
                    throw new BadStructuredResponseException(rawResponseText, "The model's response contains an empty technical specification for a non-rejected request.");
                }

                if (responseDTO.RequestRejected && string.IsNullOrWhiteSpace(responseDTO.ReasonOfRejection))
                {
                    _logger.LogWarning("The model's response rejected the request without providing reasonOfRejection. Response text: {ResponseText}", rawResponseText);
                    throw new BadStructuredResponseException(rawResponseText, "The model's response rejected the request without providing reasonOfRejection.");
                }

                if (!responseDTO.RequestRejected)
                {
                    responseDTO.ReasonOfRejection = null;
                }

                if (responseDTO.SelectedAPIsFileLocations == null)
                {
                    responseDTO.SelectedAPIsFileLocations = Array.Empty<string>();
                }

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
            [JsonPropertyName("technicalSpecification")]
            public string TechnicalSpecification { get; set; } = string.Empty;

            [JsonRequired]
            [JsonPropertyName("requestRejected")]
            public bool RequestRejected { get; set; }

            [JsonPropertyName("reasonOfRejection")]
            public string? ReasonOfRejection { get; set; }

            [JsonPropertyName("selectedAPIsFileLocations")]
            public IEnumerable<string> SelectedAPIsFileLocations { get; set; } = Array.Empty<string>();
        }
    }
}
