using AgentMesh.Application.Contracts;
using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Utils;
using AgentMesh.Application.Models.FunctionalAnalyst;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using AgentMesh.Application.Models.Agents;
using AgentMesh.Application.Models.Workflows.Parameters;

namespace AgentMesh.Application.Services.Agents
{
    public sealed class FunctionalAnalystAgent(
        IOpenAIClientFactory openAIClientFactory,
        Resilience resilience,
        ILogger<FunctionalAnalystAgent> logger,
        IAgentInputSerializer agentInputSerializer) : AbstractAgent<FunctionalAnalysisResult>(logger, 
            "FunctionalAnalyst", 
            openAIClientFactory, 
            resilience,
            agentInputSerializer)
    {
        private readonly ILogger<FunctionalAnalystAgent> _logger = logger;

        protected override IEnumerable<AgentInputParameterConfiguration> GetAgentInputParameterConfiguration()
        {
            return [
                new AgentInputParameterConfiguration
                {
                    ParameterName = EWParameterNames.RequestDateTime,
                    ParameterTags = [ApplicationConstants.AgentSystemParameterTag]
                },
                new AgentInputParameterConfiguration
                {
                    ParameterName = EWParameterNames.DomainsKnowledgeBaseDocumentsContent,
                    ParameterTags = [ApplicationConstants.AgentSystemParameterTag]
                }
            ];
        }

        protected override FunctionalAnalysisResult ParseStructuredResponse(string rawResponseText)
        {
            try
            {
                var responseDTO = JsonSerializer.Deserialize<ParsedResponse>(rawResponseText);

                if (responseDTO == null)
                {
                    _logger.LogWarning("The model's response could not be deserialized into the expected format. Response text: {ResponseText}", rawResponseText);
                    throw new BadStructuredResponseException(rawResponseText, "The model's response could not be deserialized into the expected format.");
                }

                if (!responseDTO.RequestRejected && string.IsNullOrWhiteSpace(responseDTO.BusinessRequirements))
                {
                    _logger.LogWarning("The model's response contains empty business requirements for a non-rejected request. Response text: {ResponseText}", rawResponseText);
                    throw new BadStructuredResponseException(rawResponseText, "The model's response contains empty business requirements for a non-rejected request.");
                }

                if (responseDTO.RequestRejected && string.IsNullOrWhiteSpace(responseDTO.ReasonOfRejection))
                {
                    _logger.LogWarning("The model's response rejected the request without providing reasonOfRejection. Response text: {ResponseText}", rawResponseText);
                    throw new BadStructuredResponseException(rawResponseText, "The model's response rejected the request without providing reasonOfRejection.");
                }

                return new FunctionalAnalysisResult
                {
                    BusinessRequirements = responseDTO.BusinessRequirements,
                    RequestRejected = responseDTO.RequestRejected,
                    ReasonOfRejection = responseDTO.ReasonOfRejection
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
            [JsonPropertyName("businessRequirements")]
            public string BusinessRequirements { get; set; } = string.Empty;

            [JsonRequired]
            [JsonPropertyName("requestRejected")]
            public bool RequestRejected { get; set; }

            [JsonPropertyName("reasonOfRejection")]
            public string? ReasonOfRejection { get; set; }
        }
    }
}
