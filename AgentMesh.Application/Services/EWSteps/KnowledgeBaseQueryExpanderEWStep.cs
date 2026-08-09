using AgentMesh.Application.Models.KnowledgeBaseQueryExpander;
using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Application.Services.Agents;
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
        private const string QmdQueryTypesFileName = "QMDQueryTypes.md";

        public string Name => "Knowledge Base Query Expander";

        public bool IsAgentic => true;

        public string? AgentName => "KnowledgeBaseQueryExpander";

        public bool IsPipelineFirst => false;

        public bool IsPipelineLast => false;

        public async Task<EWStepResultRecord> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var intentCategory = intentCategoryParameter.ParameterValue ?? UserIntentCategory.Other;

            var sr = new StructuredUserRequest
            {
                Intent = userIntentParameter.ParameterValue ?? string.Empty,
                IntentCategory = intentCategory,
                UserRequestedActions = userRequestedActionsParameter.ParameterValue ?? [],
                UserProvidedData = userProvidedDataParameter.ParameterValue ?? []
            };

            var agentInput = new KnowledgeBaseQueryExpanderAgentInput
            {
                StructuredUserRequest = sr,
                GenerateHydeQueries = intentCategory == UserIntentCategory.Documentation,
                DocumentationQueriesGenerationReference = LoadDocumentationQueriesGenerationReference()
            };

            var agentOutput = await knowledgeBaseQueryExpanderAgent.ExecuteAsync(agentInput, cancellationToken);

            var searchQueries = agentOutput.SearchQueries.ToList();
            if (intentCategory != UserIntentCategory.Documentation)
            {
                searchQueries = searchQueries
                    .Where(q => q.SearchType != AgentMesh.Models.KnowledgeBase.KnowledgeBaseQuerySearchType.HypotheticalDocument)
                    .ToList();
            }

            domainsKnowledgeBaseQueryParameter.ParameterValue = searchQueries;

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
