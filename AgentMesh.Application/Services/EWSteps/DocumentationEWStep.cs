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
        EWParametersProvider ewParametersProvider) : IEWStep
    {
        public string Name => "Documentation";

        public bool IsAgentic => true;

        public string? AgentName => DocumentationAgentConfiguration.AgentName;

        public bool IsPipelineFirst => false;

        public bool IsPipelineLast => false;

        public IEnumerable<string> InputParameters => [
            EWParameterNames.UserIntent,
            EWParameterNames.PastMemoriesQueryResults,
            EWParameterNames.DomainsKnowledgeBaseDocumentsContent,
            EWParameterNames.LanguageOfTheUser
        ];

        private readonly DocumentationAgent _documentationAgent = documentationAgent;
        private readonly EWParametersProvider _ewParametersProvider = ewParametersProvider;

        public async Task<EWStepResultRecord> ExecuteAsync(IEnumerable<IEWParameter> inputParameters, CancellationToken cancellationToken = default)
        {
            var intentParameter = inputParameters.Single(p => p.Name == EWParameterNames.UserIntent);
            if (intentParameter is not UserIntentParameter typedIntent)
                throw new InvalidOperationException($"Parameter {EWParameterNames.UserIntent} is not of type UserIntentParameter");

            var memoriesParameter = inputParameters.Single(p => p.Name == EWParameterNames.PastMemoriesQueryResults);
            if (memoriesParameter is not PastMemoriesQueryResultsParameter typedMemories)
                throw new InvalidOperationException($"Parameter {EWParameterNames.PastMemoriesQueryResults} is not of type PastMemoriesQueryResultsParameter");

            var docsParameter = inputParameters.Single(p => p.Name == EWParameterNames.DomainsKnowledgeBaseDocumentsContent);
            if (docsParameter is not DomainsKnowledgeBaseDocumentsContentParameter typedDocs)
                throw new InvalidOperationException($"Parameter {EWParameterNames.DomainsKnowledgeBaseDocumentsContent} is not of type DomainsKnowledgeBaseDocumentsContentParameter");

            var languageParameter = inputParameters.Single(p => p.Name == EWParameterNames.LanguageOfTheUser);
            if (languageParameter is not LanguageOfTheUserParameter typedLanguage)
                throw new InvalidOperationException($"Parameter {EWParameterNames.LanguageOfTheUser} is not of type LanguageOfTheUserParameter");

            var sr = new StructuredUserRequest
            {
                Intent = typedIntent.ParameterValue ?? string.Empty
            };

            var kbContent = WorkflowExecutorFormatting.SerializeDocumentation(typedDocs.ParameterValue ?? []);

            var agentInput = new DocumentationAgentInput
            {
                UserRequest = sr,
                AgentMemories = (typedMemories.ParameterValue ?? []).Select(m => m.Memory),
                KnowledgeBaseDocumentsContent = kbContent,
                LanguageOfTheUser = typedLanguage.ParameterValue ?? string.Empty
            };

            var agentOutput = await _documentationAgent.ExecuteAsync(agentInput, cancellationToken);

            var documentationContent = agentOutput.Content != null
                ? new[] { new KnowledgeBaseDocumentContent { Content = agentOutput.Content } }
                : Enumerable.Empty<KnowledgeBaseDocumentContent>();

            _ewParametersProvider.UpdateParameterValue(EWParameterNames.DocumentationContent, (IEnumerable<KnowledgeBaseDocumentContent>)documentationContent);

            return new EWStepResultRecord(agentOutput.InputTokenCount, agentOutput.OutputTokenCount);
        }
    }
}
