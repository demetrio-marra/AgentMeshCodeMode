using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Application.Services.Agents;
using AgentMesh.Models.RequestAnalysis;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class KnowledgeBaseQueryExpanderEWAgenticStep(
        KnowledgeBaseQueryExpanderAgent knowledgeBaseQueryExpanderAgent,
        RequestDateTimeParameter requestDateTimeParameter,
        QMDQueryTypesDocumentationParameter qmdQueryTypesDocumentationParameter,
        UserIntentParameter userIntentParameter,
        IntentCategoryParameter intentCategoryParameter,
        UserRequestedActionsParameter userRequestedActionsParameter,
        UserProvidedDataParameter userProvidedDataParameter,
        DomainsKnowledgeBaseQueryParameter domainsKnowledgeBaseQueryParameter) : IEWAgenticStep
    {
        public string Name => "Knowledge Base Query Expander";

        public string? AgentName => "KnowledgeBaseQueryExpander";

        public bool IsInputTokensCountSource => false;

        public bool IsOutputTokensCountSource => false;

        public async Task<EWAgenticStepResultRecord> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var agentOutput = await knowledgeBaseQueryExpanderAgent.ExecuteAsync([
                requestDateTimeParameter,
                qmdQueryTypesDocumentationParameter,
                userIntentParameter,
                intentCategoryParameter,
                userRequestedActionsParameter,
                userProvidedDataParameter,
                ], cancellationToken);

            var searchQueries = agentOutput.Result.ToList();
            if (intentCategoryParameter.ParameterValue!.Value != UserIntentCategory.Documentation) // safety net
            {
                searchQueries = [.. searchQueries.Where(q => q.SearchType != AgentMesh.Models.KnowledgeBase.KnowledgeBaseQuerySearchType.HypotheticalDocument)];
            }

            domainsKnowledgeBaseQueryParameter.ParameterValue = [.. searchQueries.Select(s => new Models.KnowledgeBase.KnowledgeBaseQueryInputItem
            {
                Query = s.Query,
                SearchType = s.SearchType
            })];

            return new EWAgenticStepResultRecord(agentOutput.InputTokenCount, agentOutput.OutputTokenCount);
        }
    }
}
