using AgentMesh.Application.Contracts;
using AgentMesh.Application.Utils;
using Microsoft.Extensions.Logging;
using AgentMesh.Application.Models.Agents;
using AgentMesh.Application.Models.Workflows.Parameters;

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
                    ParameterName = EWParameterNames.RequestDateTime,
                    ParameterTags = [ApplicationConstants.AgentSystemParameterTag]
                },
                new() {
                    ParameterName = EWParameterNames.DomainsKnowledgeBaseDocumentsContent,
                    ParameterTags = [ApplicationConstants.AgentSystemParameterTag] 
                }
            ];
        }

        protected override string ParseStructuredResponse(string rawResponseText) => rawResponseText;
    }
}
