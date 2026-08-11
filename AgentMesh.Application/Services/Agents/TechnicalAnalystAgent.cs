using AgentMesh.Application.Contracts;
using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Models.Agents;
using AgentMesh.Application.Models.TechnicalAnalyst;
using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Application.Utils;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentMesh.Application.Services.Agents
{
    public sealed class TechnicalAnalystAgent(
        IOpenAIClientFactory openAIClientFactory,
        Resilience resilience,
        ILogger<TechnicalAnalystAgent> logger,
        IAgentInputSerializer agentInputSerializer) : AbstractAgent<TechnicalAnalysis>(logger,
            "TechnicalAnalyst", 
            openAIClientFactory, 
            resilience,
            agentInputSerializer)
    {
        private readonly ILogger<TechnicalAnalystAgent> _logger = logger;


        protected override IEnumerable<AgentInputParameterConfiguration> GetAgentInputParameterConfiguration()
        {
            return [
                new() { ParameterName = EWParameterNames.RequestDateTime, ParameterTags = [ApplicationConstants.AgentSystemParameterTag] },
                new() { ParameterName = EWParameterNames.KnowledgeBaseAPIDocumentsContent, ParameterTags = [ApplicationConstants.AgentSystemParameterTag] },
                ];
        }

        protected override TechnicalAnalysis ParseStructuredResponse(string rawResponseText)
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

                return new TechnicalAnalysis
                {
                    TechnicalSpecification = responseDTO.TechnicalSpecification,
                    RequestRejected = responseDTO.RequestRejected,
                    FilteredApisDocumentationFiles = responseDTO.SelectedAPIsFileLocations.ToList(),
                    RequestRejectionReason = responseDTO.ReasonOfRejection
                };
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
