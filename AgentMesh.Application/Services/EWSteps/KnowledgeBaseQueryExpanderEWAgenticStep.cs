using AgentMesh.Application.Models.KnowledgeBase;
using AgentMesh.Application.Models.Parameters;
using AgentMesh.Application.Services.Agents;
using AgentMesh.Models;
using AgentMesh.Models.RequestAnalysis;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class KnowledgeBaseQueryExpanderEWAgenticStep(
        KnowledgeBaseQueryExpanderAgent knowledgeBaseQueryExpanderAgent,
        IntentCategoryParameter intentCategoryParameter) : IEWAgenticStep
    {
        public string Name => "Knowledge Base Query Expander";

        public string? AgentName => "KnowledgeBaseQueryExpander";

        public bool CountInputTokensAsContextTokens => false;

        public bool CountOutputTokensAsContextTokens => false;

        public IEnumerable<Type> InputParameterTypes => [
            typeof(RequestDateTimeParameter),
            typeof(LanguageOfTheDocumentationParameter),
            typeof(QMDQueryTypesDocumentationParameter),
            typeof(UserIntentParameter),
            typeof(IntentCategoryParameter),
            typeof(UserRequestedActionsParameter),
            typeof(UserProvidedDataParameter)
            ];

        public IEnumerable<Type> OutputParameterTypes => [typeof(DomainsKnowledgeBaseQueryParameter)];

        public async Task<EWStepExecutionResult> ExecuteAsync(IReadOnlyDictionary<Type, object?> Values, CancellationToken cancellationToken = default)
        {
            var agentOutput = await knowledgeBaseQueryExpanderAgent.ExecuteAsync(Values, cancellationToken);

            var searchQueries = agentOutput.Result.ToList();
            var intentCategory = intentCategoryParameter.ValueAs(Values[typeof(IntentCategoryParameter)]);
            if (intentCategory != UserIntentCategory.Documentation)
            {
                searchQueries = [.. searchQueries.Where(q => q.SearchType != AgentMesh.Models.KnowledgeBase.KnowledgeBaseQuerySearchType.HypotheticalDocument)];
            }

            var domainQueries = searchQueries.Select(s => new KnowledgeBaseQueryInputItem
            {
                Query = s.Query,
                SearchType = s.SearchType
            }).ToList();

            return new EWAgenticStepExecutionResult
            {
                InputTokens = agentOutput.InputTokenCount,
                OutputTokens = agentOutput.OutputTokenCount,
                OutputMutations = new Dictionary<Type, object?>
                {
                    { typeof(DomainsKnowledgeBaseQueryParameter), domainQueries }
                }
            };
        }
    }
}
