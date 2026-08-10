using AgentMesh.Application.Contracts;
using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Utils;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using AgentMesh.Application.Models.Agents;
using AgentMesh.Application.Models.Workflows.Parameters;

namespace AgentMesh.Application.Services.Agents
{
    public sealed class AgentMemoryQueryExpanderAgent(
        IOpenAIClientFactory openAIClientFactory,
        Resilience resilience,
        ILogger<AgentMemoryQueryExpanderAgent> logger,
        IAgentInputSerializer agentInputSerializer) : AbstractAgent<IEnumerable<string>>(logger,
            "AgentMemoryQueryExpander", 
            openAIClientFactory, 
            resilience,
            agentInputSerializer)
    {
        private readonly ILogger<AgentMemoryQueryExpanderAgent> _logger = logger;

        protected override IEnumerable<AgentInputParameterConfiguration> GetAgentInputParameterConfiguration()
        {
            return [
                new()
                {
                    ParameterName = EWParameterNames.MissingValues,
                    ParameterTags = []
                },
                new()
                {
                    ParameterName = EWParameterNames.RequestDateTime,
                    ParameterTags = [ApplicationConstants.AgentSystemParameterTag]
                }
            ];
        }

        protected override IEnumerable<string> ParseStructuredResponse(string rawResponseText)
        {
            try
            {
                var responseDTO = JsonSerializer.Deserialize<ParsedResponse>(rawResponseText, AgentResponseJsonSerializationUtils.DefaultDeserializeOptions);

                if (responseDTO == null)
                {
                    _logger.LogWarning("The model's response could not be deserialized into the expected format. Response text: {ResponseText}", rawResponseText);
                    throw new BadStructuredResponseException(rawResponseText, "The model's response could not be deserialized into the expected format.");
                }

                return responseDTO.SearchQueries;
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
            public IEnumerable<string> SearchQueries { get; set; } = [];
        }
    }
}
