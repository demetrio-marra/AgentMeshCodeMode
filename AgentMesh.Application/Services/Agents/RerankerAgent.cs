using AgentMesh.Application.Contracts;
using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Utils;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using AgentMesh.Application.Models.Agents;
using AgentMesh.Application.Models.Workflows;
using AgentMesh.Application.Services.Helpers;
using AgentMesh.Application.Models.Parameters;

namespace AgentMesh.Application.Services.Agents
{
    public sealed class RerankerAgent(
        IOpenAIClientFactory openAIClientFactory,
        Resilience resilience,
        ILogger<RerankerAgent> logger,
        IAgentInputSerializer agentInputSerializer) : AbstractAgent<List<string>>(logger, 
            "Reranker",
            openAIClientFactory, 
            resilience,
            agentInputSerializer)
    {
        private readonly ILogger<RerankerAgent> _logger = logger;


        protected override IEnumerable<AgentInputParameterConfiguration> GetAgentInputParameterConfiguration()
        {
            return [
                new () { ParameterName = RequestDateTimeParameter.ParamName, ParameterTags = [ParameterTags.AgentSystemParameterTag] },
                new () { ParameterName = KnowledgeBaseQueryResultsParameter.ParamName, ParameterTags = [ParameterTags.AgentSystemParameterTag] }
                ];
        }

        protected override List<string> ParseStructuredResponse(string rawResponseText)
        {
            try
            {
                var responseDTO = JsonSerializer.Deserialize<ParsedResponse>(rawResponseText, AgentResponseJsonSerializationUtils.DefaultDeserializeOptions) ?? throw new BadStructuredResponseException(rawResponseText, "The model's response could not be deserialized into reranker output.");
                if (responseDTO.SelectedFiles == null)
                {
                    throw new BadStructuredResponseException(rawResponseText, "The model's response did not contain the 'selectedFiles' property.");
                }

                return [.. responseDTO.SelectedFiles
                    .Where(file => !string.IsNullOrWhiteSpace(file))
                    .Select(file => file.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)];
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize the reranker response. Response text: {ResponseText}", rawResponseText);
                throw new BadStructuredResponseException(rawResponseText, "Failed to parse the model's response.", ex);
            }
        }

        
        private class ParsedResponse
        {
            [JsonPropertyName("selectedFiles")]
            public IEnumerable<string> SelectedFiles { get; set; } = [];
        }
    }
}
