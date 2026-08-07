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
        UserIntentParameter userIntentParameter,
        IntentCategoryParameter intentCategoryParameter,
        UserRequestedActionsParameter userRequestedActionsParameter,
        UserProvidedDataParameter userProvidedDataParameter,
        DomainsKnowledgeBaseQueryParameter domainsKnowledgeBaseQueryParameter) : IEWStep
    {
        public string Name => "Knowledge Base Query Expander";

        public bool IsAgentic => true;

        public string? AgentName => KnowledgeBaseQueryExpanderAgentConfiguration.AgentName;

        public bool IsPipelineFirst => false;

        public bool IsPipelineLast => false;

        private readonly KnowledgeBaseQueryExpanderAgent _knowledgeBaseQueryExpanderAgent = knowledgeBaseQueryExpanderAgent;
        private readonly UserIntentParameter _userIntentParameter = userIntentParameter;
        private readonly IntentCategoryParameter _intentCategoryParameter = intentCategoryParameter;
        private readonly UserRequestedActionsParameter _userRequestedActionsParameter = userRequestedActionsParameter;
        private readonly UserProvidedDataParameter _userProvidedDataParameter = userProvidedDataParameter;
        private readonly DomainsKnowledgeBaseQueryParameter _domainsKnowledgeBaseQueryParameter = domainsKnowledgeBaseQueryParameter;

        private const string QmdQueryTypesFileName = "QMDQueryTypes.md";

        public async Task<EWStepResultRecord> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var intentCategory = _intentCategoryParameter.ParameterValue ?? UserIntentCategory.Other;

            var sr = new StructuredUserRequest
            {
                Intent = _userIntentParameter.ParameterValue ?? string.Empty,
                IntentCategory = intentCategory,
                UserRequestedActions = _userRequestedActionsParameter.ParameterValue ?? [],
                UserProvidedData = _userProvidedDataParameter.ParameterValue ?? []
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

            _domainsKnowledgeBaseQueryParameter.ParameterValue = searchQueries;

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
