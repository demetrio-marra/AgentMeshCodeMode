using AgentMesh.Application.Contracts;
using AgentMesh.Application.Exceptions;
using AgentMesh.Application.Utils;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using AgentMesh.Application.Models.Agents;
using AgentMesh.Application.Models.Workflows;
using AgentMesh.Application.Services.Helpers;
using AgentMesh.Models;

namespace AgentMesh.Application.Services.Agents
{
    public sealed class RelevantFactsEvaluatorAgent(
        IOpenAIClientFactory openAIClientFactory,
        Resilience resilience,
        ILogger<RelevantFactsEvaluatorAgent> logger,
        IAgentInputSerializer agentInputSerializer) : AbstractAgent<List<ContextMessage>>(logger, 
            "RelevantFactsEvaluator",
            openAIClientFactory, 
            resilience,
            agentInputSerializer)
    {
        private readonly ILogger<RelevantFactsEvaluatorAgent> _logger = logger;


        protected override IEnumerable<AgentInputParameterConfiguration> GetAgentInputParameterConfiguration()
        {
            return [
                new AgentInputParameterConfiguration
                {
                    ParameterName = RequestDateTimeParameter.ParamName,
                    ParameterTags = [ParameterTags.AgentSystemParameterTag]
                }
                ];
        }

        protected override List<ContextMessage> ParseStructuredResponse(string rawResponseText)
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<IEnumerable<ContextMessage>>(rawResponseText);
                if (parsed == null)
                {
                    throw new BadStructuredResponseException(rawResponseText, "The model's response could not be deserialized into a list of user messages.");
                }

                return [.. parsed];
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize relevant facts evaluator response. Response text: {ResponseText}", rawResponseText);
                throw new BadStructuredResponseException(rawResponseText, "Failed to parse the model's response as a JSON array of user messages.", ex);
            }
        }
    }
}
