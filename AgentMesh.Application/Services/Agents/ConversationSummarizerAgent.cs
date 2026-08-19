using AgentMesh.Application.Contracts;
using AgentMesh.Application.Models.Agents;
using AgentMesh.Application.Models.Parameters;
using AgentMesh.Application.Models.Workflows;
using AgentMesh.Application.Services.Helpers;
using AgentMesh.Application.Utils;
using Microsoft.Extensions.Logging;

namespace AgentMesh.Application.Services.Agents
{
    public sealed class ConversationSummarizerAgent(IOpenAIClientFactory openAIClientFactory,
                                      Resilience resilience,
                                      ILogger<ConversationSummarizerAgent> logger,
                                      IAgentInputSerializer agentInputSerializer) : AbstractAgent<string>(logger,
                                          "ConversationSummarizer", 
                                          openAIClientFactory,
                                          resilience,
                                          agentInputSerializer)
    {
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
                    ParameterType = typeof(SummarizeLanguageParameter),
                    ParameterTags = [ParameterTags.AgentSystemParameterTag]
                }
           ];
        }

        protected override string ParseStructuredResponse(string rawResponseText) => rawResponseText;
    }
}

