using AgentMesh.Application.Configuration;
using AgentMesh.Application.Models.KnowledgeBaseQueryExpander;
using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Models.RequestAnalysis;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class KnowledgeBaseQueryExpanderEWStep(
        KnowledgeBaseQueryExpanderAgent knowledgeBaseQueryExpanderAgent,
        EWParametersProvider ewParametersProvider) : IEWStep
    {
        public string Name => "Knowledge Base Query Expander";

        public bool IsAgentic => true;

        public string? AgentName => KnowledgeBaseQueryExpanderAgentConfiguration.AgentName;

        public bool IsPipelineFirst => false;

        public bool IsPipelineLast => false;

        public IEnumerable<string> InputParameters => [
            EWParameterNames.UserIntent,
            EWParameterNames.IntentCategory,
            EWParameterNames.UserRequestedActions,
            EWParameterNames.UserProvidedData
        ];

        private readonly KnowledgeBaseQueryExpanderAgent _knowledgeBaseQueryExpanderAgent = knowledgeBaseQueryExpanderAgent;
        private readonly EWParametersProvider _ewParametersProvider = ewParametersProvider;

        private const string QmdQueryTypesFileName = "QMDQueryTypes.md";

        public async Task<EWStepResultRecord> ExecuteAsync(IEnumerable<IEWParameter> inputParameters, CancellationToken cancellationToken = default)
        {
            var intentParameter = inputParameters.Single(p => p.Name == EWParameterNames.UserIntent);
            if (intentParameter is not UserIntentParameter typedIntent)
                throw new InvalidOperationException($"Parameter {EWParameterNames.UserIntent} is not of type UserIntentParameter");

            var intentCategoryParameter = inputParameters.Single(p => p.Name == EWParameterNames.IntentCategory);
            if (intentCategoryParameter is not IntentCategoryParameter typedIntentCategory)
                throw new InvalidOperationException($"Parameter {EWParameterNames.IntentCategory} is not of type IntentCategoryParameter");

            var userRequestedActionsParameter = inputParameters.Single(p => p.Name == EWParameterNames.UserRequestedActions);
            if (userRequestedActionsParameter is not UserRequestedActionsParameter typedUserRequestedActions)
                throw new InvalidOperationException($"Parameter {EWParameterNames.UserRequestedActions} is not of type UserRequestedActionsParameter");

            var userProvidedDataParameter = inputParameters.Single(p => p.Name == EWParameterNames.UserProvidedData);
            if (userProvidedDataParameter is not UserProvidedDataParameter typedUserProvidedData)
                throw new InvalidOperationException($"Parameter {EWParameterNames.UserProvidedData} is not of type UserProvidedDataParameter");

            var intentCategory = typedIntentCategory.ParameterValue ?? UserIntentCategory.Other;

            var sr = new StructuredUserRequest
            {
                Intent = typedIntent.ParameterValue ?? string.Empty,
                IntentCategory = intentCategory,
                UserRequestedActions = typedUserRequestedActions.ParameterValue ?? [],
                UserProvidedData = typedUserProvidedData.ParameterValue ?? []
            };

            var agentInput = new KnowledgeBaseQueryExpanderAgentInput
            {
                StructuredUserRequest = sr,
                GenerateHydeQueries = intentCategory == UserIntentCategory.Documentation,
                DocumentationQueriesGenerationReference = LoadDocumentationQueriesGenerationReference()
            };

            var agentOutput = await _knowledgeBaseQueryExpanderAgent.ExecuteAsync(agentInput, cancellationToken);

            var searchQueries = agentOutput.SearchQueries.ToList();
            if (intentCategory != UserIntentCategory.Documentation)
            {
                searchQueries = searchQueries
                    .Where(q => q.SearchType != AgentMesh.Models.KnowledgeBase.KnowledgeBaseQuerySearchType.HypotheticalDocument)
                    .ToList();
            }

            _ewParametersProvider.UpdateParameterValue(EWParameterNames.DomainsKnowledgeBaseQuery, (IEnumerable<AgentMesh.Models.KnowledgeBase.KnowledgeBaseQueryInputItem>)searchQueries);

            return new EWStepResultRecord(agentOutput.InputTokenCount, agentOutput.OutputTokenCount);
        }

        private string? LoadDocumentationQueriesGenerationReference()
        {
            var candidatePaths = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "Prompts", QmdQueryTypesFileName),
                Path.Combine(Directory.GetCurrentDirectory(), "Prompts", QmdQueryTypesFileName),
                Path.Combine(Directory.GetCurrentDirectory(), "AgentMeshCLI", "Prompts", QmdQueryTypesFileName)
            };

            foreach (var candidatePath in candidatePaths)
            {
                if (File.Exists(candidatePath))
                    return File.ReadAllText(candidatePath);
            }

            return null;
        }
    }
}
