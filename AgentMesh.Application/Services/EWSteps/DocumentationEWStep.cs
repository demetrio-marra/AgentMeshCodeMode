using AgentMesh.Application.Configuration;
using AgentMesh.Application.Models.Documentation;
using AgentMesh.Application.Models.KnowledgeBase;
using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Models.RequestAnalysis;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;
using AgentMesh.Utils;
using System.Text.Json;

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

        private readonly DocumentationAgent documentationAgent = documentationAgent;
        private readonly UserIntentParameter userIntentParameter = userIntentParameter;
        private readonly PastMemoriesQueryResultsParameter pastMemoriesQueryResultsParameter = pastMemoriesQueryResultsParameter;
        private readonly DomainsKnowledgeBaseDocumentsContentParameter domainsKnowledgeBaseDocumentsContentParameter = domainsKnowledgeBaseDocumentsContentParameter;
        private readonly LanguageOfTheUserParameter languageOfTheUserParameter = languageOfTheUserParameter;
        private readonly DocumentationContentParameter documentationContentParameter = documentationContentParameter;

        public async Task<EWStepResultRecord> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var sr = new StructuredUserRequest
            {
                Intent = this.userIntentParameter.ParameterValue ?? string.Empty
            };

            var kbContent = JsonSerializer.Serialize(this.domainsKnowledgeBaseDocumentsContentParameter.ParameterValue ?? [], SerializationUtils.DefaultSerializeOptions);

            var agentInput = new DocumentationAgentInput
            {
                UserRequest = sr,
                AgentMemories = (this.pastMemoriesQueryResultsParameter.ParameterValue ?? []).Select(m => m.Memory),
                KnowledgeBaseDocumentsContent = kbContent,
                LanguageOfTheUser = this.languageOfTheUserParameter.ParameterValue ?? string.Empty
            };

            var agentOutput = await this.documentationAgent.ExecuteAsync(agentInput, cancellationToken);

            this.documentationContentParameter.ParameterValue = agentOutput.Content ?? string.Empty;

            return new EWStepResultRecord(agentOutput.InputTokenCount, agentOutput.OutputTokenCount);
        }
    }
}
