using AgentMesh.Application.Configuration;
using AgentMesh.Application.Models.Documentation;
using AgentMesh.Application.Models.KnowledgeBase;
using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Application.Services.Workflows.Steps;
using AgentMesh.Models.RequestAnalysis;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class DocumentationEWStep(
        DocumentationAgent documentationAgent,
        UserIntentParameter userIntentParameter,
        PastMemoriesQueryResultsParameter pastMemoriesQueryResultsParameter,
        DomainsKnowledgeBaseDocumentsContentParameter domainsKnowledgeBaseDocumentsContentParameter,
        LanguageOfTheUserParameter languageOfTheUserParameter,
        DocumentationContentParameter documentationContentParameter) : IEWStep
    {
        public string Name => "Documentation";

        public bool IsAgentic => true;

        public string? AgentName => DocumentationAgentConfiguration.AgentName;

        public bool IsPipelineFirst => false;

        public bool IsPipelineLast => false;

        private readonly DocumentationAgent _documentationAgent = documentationAgent;
        private readonly UserIntentParameter _userIntentParameter = userIntentParameter;
        private readonly PastMemoriesQueryResultsParameter _pastMemoriesQueryResultsParameter = pastMemoriesQueryResultsParameter;
        private readonly DomainsKnowledgeBaseDocumentsContentParameter _domainsKnowledgeBaseDocumentsContentParameter = domainsKnowledgeBaseDocumentsContentParameter;
        private readonly LanguageOfTheUserParameter _languageOfTheUserParameter = languageOfTheUserParameter;
        private readonly DocumentationContentParameter _documentationContentParameter = documentationContentParameter;

        public async Task<EWStepResultRecord> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var sr = new StructuredUserRequest
            {
                Intent = _userIntentParameter.ParameterValue ?? string.Empty
            };

            var kbContent = WorkflowExecutorFormatting.SerializeDocumentation(_domainsKnowledgeBaseDocumentsContentParameter.ParameterValue ?? []);

            var agentInput = new DocumentationAgentInput
            {
                UserRequest = sr,
                AgentMemories = (_pastMemoriesQueryResultsParameter.ParameterValue ?? []).Select(m => m.Memory),
                KnowledgeBaseDocumentsContent = kbContent,
                LanguageOfTheUser = _languageOfTheUserParameter.ParameterValue ?? string.Empty
            };

            var agentOutput = await _documentationAgent.ExecuteAsync(agentInput, cancellationToken);

            _documentationContentParameter.ParameterValue = agentOutput.Content != null
                ? [new KnowledgeBaseDocumentContent { Content = agentOutput.Content }]
                : [];

            return new EWStepResultRecord(agentOutput.InputTokenCount, agentOutput.OutputTokenCount);
        }
    }
}
