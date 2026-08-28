using AgentMesh.Application.Contracts;
using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Models.Agents;
using AgentMesh.Application.Models.Analyst;
using AgentMesh.Application.Models.Parameters;
using AgentMesh.Application.Models.Workflows;
using AgentMesh.Application.Services.Helpers;
using AgentMesh.Application.Utils;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentMesh.Application.Services.Agents
{
    public sealed class AnalystAgent(
        IOpenAIClientFactory openAIClientFactory,
        Resilience resilience,
        ILogger<AnalystAgent> logger,
        IAgentInputSerializer agentInputSerializer) : AbstractAgent<AnalystResult>(logger,
            "Analyst",
            openAIClientFactory,
            resilience,
            agentInputSerializer)
    {
        private readonly ILogger<AnalystAgent> _logger = logger;

        protected override IEnumerable<AgentInputParameterConfiguration> GetAgentInputParameterConfiguration()
        {
            return [
                new()
                {
                    ParameterType = typeof(RequestDateTimeParameter),
                    ParameterTags = [ParameterTags.AgentSystemParameterTag]
                },
                new()
                {
                    ParameterType = typeof(KnowledgeQueryResultParameter),
                    ParameterTags = [ParameterTags.AgentSystemParameterTag]
                }
            ];
        }

        protected override AnalystResult ParseStructuredResponse(string rawResponseText)
        {
            try
            {
                var response = JsonSerializer.Deserialize<ParsedResponse>(rawResponseText);
                if (response == null)
                {
                    _logger.LogWarning("The model's response could not be deserialized into the expected format. Response text: {ResponseText}", rawResponseText);
                    throw new BadStructuredResponseException(rawResponseText, "The model's response could not be deserialized into the expected format.");
                }

                response.DocumentationMissingEntities ??= Array.Empty<string>();

                if (response.Accepted)
                {
                    if (string.IsNullOrWhiteSpace(response.Specification))
                    {
                        _logger.LogWarning("The model's response contains an empty specification for an accepted request. Response text: {ResponseText}", rawResponseText);
                        throw new BadStructuredResponseException(rawResponseText, "The model's response contains an empty specification for an accepted request.");
                    }

                    response.RejectReason = null;
                    response.DocumentationMissingEntities = Array.Empty<string>();
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(response.RejectReason))
                    {
                        _logger.LogWarning("The model's response rejected the request without a rejectReason. Response text: {ResponseText}", rawResponseText);
                        throw new BadStructuredResponseException(rawResponseText, "The model's response rejected the request without a rejectReason.");
                    }

                    response.Specification = string.Empty;
                }

                return new AnalystResult
                {
                    Accepted = response.Accepted,
                    Specification = response.Specification,
                    RejectReason = response.RejectReason,
                    DocumentationMissingEntities = response.DocumentationMissingEntities
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
            [JsonRequired]
            [JsonPropertyName("accepted")]
            public bool Accepted { get; set; }

            [JsonPropertyName("specification")]
            public string Specification { get; set; } = string.Empty;

            [JsonPropertyName("rejectReason")]
            public string? RejectReason { get; set; }

            [JsonPropertyName("documentationMissingEntities")]
            public IEnumerable<string>? DocumentationMissingEntities { get; set; }
        }
    }
}
