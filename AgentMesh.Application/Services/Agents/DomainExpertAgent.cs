using AgentMesh.Application.Contracts;
using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Models.Agents;
using AgentMesh.Application.Models.Workflows;
using AgentMesh.Application.Utils;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentMesh.Application.Services.Agents
{
    public sealed class DomainExpertAgent(
        IOpenAIClientFactory openAIClientFactory,
        Resilience resilience,
        ILogger<DomainExpertAgent> logger,
        IAgentInputSerializer agentInputSerializer) : AbstractAgent<string>(logger,
            "DomainExpert", 
            openAIClientFactory, 
            resilience,
            agentInputSerializer)
    {
        private readonly ILogger<DomainExpertAgent> _logger = logger;

        protected override IEnumerable<AgentInputParameterConfiguration> GetAgentInputParameterConfiguration()
        {
            return [
                new () { ParameterName = RequestDateTimeParameter.ParamName, ParameterTags = [ParameterTags.AgentSystemParameterTag] },
                new () { ParameterName = DomainsKnowledgeBaseDocumentsContentParameter.ParamName, ParameterTags = [ParameterTags.AgentSystemParameterTag] }
                ];
        }

        protected override string ParseStructuredResponse(string rawResponseText)
        {
            try
            {
                var responseDTO = JsonSerializer.Deserialize<ParsedResponse>(rawResponseText);

                if (responseDTO == null)
                {
                    _logger.LogWarning("The model's response could not be deserialized into the expected format. Response text: {ResponseText}", rawResponseText);
                    throw new BadStructuredResponseException(rawResponseText, "The model's response could not be deserialized into the expected format.");
                }

                if (string.IsNullOrWhiteSpace(responseDTO.DomainExpertComment))
                {
                    _logger.LogWarning("The model's response contains empty domain expert comment. Response text: {ResponseText}", rawResponseText);
                    throw new BadStructuredResponseException(rawResponseText, "The model's response contains empty domain expert comment.");
                }

                return responseDTO.DomainExpertComment;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize the model's response. Response text: {ResponseText}", rawResponseText);
                throw new BadStructuredResponseException(rawResponseText, "Failed to parse the model's response.", ex);
            }
        }

        public class ParsedResponse
        {
            [JsonPropertyName("domainExpertComment")]
            public string DomainExpertComment { get; set; } = string.Empty;
        }
    }
}
