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
        private const string QmdQueryTypesFileName = "QMDQueryTypes.md";

        public string Name => "Knowledge Base Query Expander";

        public bool IsAgentic => true;

        public string? AgentName => KnowledgeBaseQueryExpanderAgentConfiguration.AgentName;

        public bool IsPipelineFirst => false;

        public bool IsPipelineLast => false;

        private readonly KnowledgeBaseQueryExpanderAgent knowledgeBaseQueryExpanderAgent = knowledgeBaseQueryExpanderAgent;
        private readonly UserIntentParameter userIntentParameter = userIntentParameter;
        private readonly IntentCategoryParameter intentCategoryParameter = intentCategoryParameter;
        private readonly UserRequestedActionsParameter userRequestedActionsParameter = userRequestedActionsParameter;
        private readonly UserProvidedDataParameter userProvidedDataParameter = userProvidedDataParameter;
        private readonly DomainsKnowledgeBaseQueryParameter domainsKnowledgeBaseQueryParameter = domainsKnowledgeBaseQueryParameter;

        public async Task<EWStepResultRecord> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var intentCategory = this.intentCategoryParameter.ParameterValue ?? UserIntentCategory.Other;

            var sr = new StructuredUserRequest
            {
                Intent = this.userIntentParameter.ParameterValue ?? string.Empty,
                IntentCategory = intentCategory,
                UserRequestedActions = this.userRequestedActionsParameter.ParameterValue ?? [],
                UserProvidedData = this.userProvidedDataParameter.ParameterValue ?? []
            };

            var agentInput = new KnowledgeBaseQueryExpanderAgentInput
            {
                StructuredUserRequest = sr,
                GenerateHydeQueries = intentCategory == UserIntentCategory.Documentation,
                DocumentationQueriesGenerationReference = LoadDocumentationQueriesGenerationReference()
            };

            var agentOutput = await this.knowledgeBaseQueryExpanderAgent.ExecuteAsync(agentInput, cancellationToken);

            var searchQueries = agentOutput.SearchQueries.ToList();
            if (intentCategory != UserIntentCategory.Documentation)
            {
                searchQueries = searchQueries
                    .Where(q => q.SearchType != AgentMesh.Models.KnowledgeBase.KnowledgeBaseQuerySearchType.HypotheticalDocument)
                    .ToList();
            }

            this.domainsKnowledgeBaseQueryParameter.ParameterValue = searchQueries;

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
