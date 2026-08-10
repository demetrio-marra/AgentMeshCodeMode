using AgentMesh.Application.Models.Documentation;
using AgentMesh.Application.Models.RequestAnalysis;
using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Application.Services.Agents;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;
using AgentMesh.Utils;
using System.Text.Json;

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

        public async Task<EWStepResultRecord> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var sr = new StructuredUserRequest
            {
                Intent = userIntentParameter.ParameterValue ?? string.Empty
            };

            var kbContent = JsonSerializer.Serialize(domainsKnowledgeBaseDocumentsContentParameter.ParameterValue ?? [], SerializationUtils.DefaultSerializeOptions);

            var agentInput = new DocumentationAgentInput
            {
                UserRequest = sr,
                AgentMemories = (pastMemoriesQueryResultsParameter.ParameterValue ?? []).Select(m => m.Memory),
                KnowledgeBaseDocumentsContent = kbContent,
                LanguageOfTheUser = languageOfTheUserParameter.ParameterValue ?? string.Empty
            };

            var agentOutput = await documentationAgent.ExecuteAsync([
                requestDateTimeParameter,
                userIntentParameter,
                pastMemoriesQueryResultsParameter,
                domainsKnowledgeBaseDocumentsContentParameter,
                languageOfTheUserParameter], cancellationToken);

            documentationContentParameter.ParameterValue = agentOutput.Result;

            return new EWStepResultRecord(agentOutput.InputTokenCount, agentOutput.OutputTokenCount);
        }
    }
}
