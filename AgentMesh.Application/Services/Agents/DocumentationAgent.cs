using AgentMesh.Application.Contracts;
using AgentMesh.Application.Utils;
using Microsoft.Extensions.Logging;
using AgentMesh.Application.Models.Agents;
using AgentMesh.Application.Models.Workflows;

namespace AgentMesh.Application.Services.Agents
{
    public sealed class DocumentationAgent(
        IOpenAIClientFactory openAIClientFactory,
        Resilience resilience,
        ILogger<DocumentationAgent> logger,
        IAgentInputSerializer agentInputSerializer) : AbstractAgent<string>(logger,
            "Documentation",
            openAIClientFactory, 
            resilience,
            agentInputSerializer)
    {
        protected override IEnumerable<AgentInputParameterConfiguration> GetAgentInputParameterConfiguration()
        {
            return [
                new()
                {
                    ParameterName = RequestDateTimeParameter.ParamName,
                    ParameterTags = [ParameterTags.AgentSystemParameterTag]
                },
                new() {
                    ParameterName = DomainsKnowledgeBaseDocumentsContentParameter.ParamName,
                    ParameterTags = [ParameterTags.AgentSystemParameterTag] 
                }
            ];
        }

        protected override string ParseStructuredResponse(string rawResponseText) => rawResponseText;
    }
}
