using AgentMesh.Application.Models.Workflows;
using AgentMesh.Application.Services.Agents;
using AgentMesh.Models;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class DocumentationEWAgenticStep(
        DocumentationAgent documentationAgent,
        UserIntentParameter userIntentParameter,
        PastMemoriesQueryResultsParameter pastMemoriesQueryResultsParameter,
        DomainsKnowledgeBaseDocumentsContentParameter domainsKnowledgeBaseDocumentsContentParameter,
        LanguageOfTheUserParameter languageOfTheUserParameter,
        RequestDateTimeParameter requestDateTimeParameter,
        PipelineResultDataParameter pipelineResultDataParameter) : IEWAgenticStep
    {
        public string Name => "Documentation";

        public string? AgentName => "Documentation";

        public bool CountInputTokensAsContextTokens => false;

        public bool CountOutputTokensAsContextTokens => true;

        public async Task<EWAgenticStepResultRecord> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var agentOutput = await documentationAgent.ExecuteAsync([
                requestDateTimeParameter,
                userIntentParameter,
                pastMemoriesQueryResultsParameter,
                domainsKnowledgeBaseDocumentsContentParameter,
                languageOfTheUserParameter], cancellationToken);

            pipelineResultDataParameter.ParameterValue = agentOutput.Result;

            return new EWAgenticStepResultRecord(agentOutput.InputTokenCount, agentOutput.OutputTokenCount);
        }
    }
}
