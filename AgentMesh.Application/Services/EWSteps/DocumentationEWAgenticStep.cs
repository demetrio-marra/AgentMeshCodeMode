using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Application.Services.Agents;
using AgentMesh.Models.Workflows;
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
        DocumentationContentParameter documentationContentParameter) : IEWAgenticStep
    {
        public string Name => "Documentation";

        public string? AgentName => "Documentation";

        public bool IsInputTokensCountSource => false;

        public bool IsOutputTokensCountSource => false;

        public async Task<EWAgenticStepResultRecord> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var agentOutput = await documentationAgent.ExecuteAsync([
                requestDateTimeParameter,
                userIntentParameter,
                pastMemoriesQueryResultsParameter,
                domainsKnowledgeBaseDocumentsContentParameter,
                languageOfTheUserParameter], cancellationToken);

            documentationContentParameter.ParameterValue = agentOutput.Result;

            return new EWAgenticStepResultRecord(agentOutput.InputTokenCount, agentOutput.OutputTokenCount);
        }
    }
}
