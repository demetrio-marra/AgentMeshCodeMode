using AgentMesh.Application.Models.Parameters;
using AgentMesh.Application.Services.Agents;
using AgentMesh.Models;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class TechnicalAnalystEWAgenticStep(
        TechnicalAnalystAgent technicalAnalystAgent,
        KnowledgeBaseAPIDocumentsContentParameter knowledgeBaseAPIDocumentsContentParameter) : IEWAgenticStep
    {
        public string Name => "Technical Analyst";

        public string? AgentName => "TechnicalAnalyst";

        public bool CountInputTokensAsContextTokens => false;

        public bool CountOutputTokensAsContextTokens => false;

        public IEnumerable<Type> InputParameterTypes => [
            typeof(RequestDateTimeParameter),
            typeof(UserIntentParameter),
            typeof(ConversationTopicParameter),
            typeof(BusinessRequirementsParameter),
            typeof(UserRequestedActionsParameter),
            typeof(UserProvidedDataParameter),
            typeof(UserPreferencesParameter),
            typeof(PastMemoriesQueryResultsParameter),
            typeof(KnowledgeBaseAPIDocumentsContentParameter)
            ];

        public IEnumerable<Type> OutputParameterTypes => [
            typeof(KnowledgeBaseAPIDocumentsContentParameter),
            typeof(TechnicalSpecificationParameter),
            typeof(RequestRejectedFlagParameter),
            typeof(RequestRejectedReasonParameter)
            ];

        public async Task<EWStepExecutionResult> ExecuteAsync(IReadOnlyDictionary<Type, object?> Values, CancellationToken cancellationToken = default)
        {
            var agentOutput = await technicalAnalystAgent.ExecuteAsync(Values, cancellationToken);

            var outputMutations = new Dictionary<Type, object?>
            {
                { typeof(TechnicalSpecificationParameter), agentOutput.Result.TechnicalSpecification },
                { typeof(RequestRejectedFlagParameter), agentOutput.Result.RequestRejected },
                { typeof(RequestRejectedReasonParameter), agentOutput.Result.RequestRejectionReason }
            };

            if (agentOutput.Result.FilteredApisDocumentationFiles != null && agentOutput.Result.FilteredApisDocumentationFiles.Any())
            {
                var selectedDocuments = agentOutput.Result.FilteredApisDocumentationFiles.ToList();
                var currentDocuments = knowledgeBaseAPIDocumentsContentParameter.ValueAs(Values[typeof(KnowledgeBaseAPIDocumentsContentParameter)]) ?? [];
                var filteredKbDocuments = currentDocuments
                    .Where(doc => selectedDocuments.Contains(doc.File, StringComparer.OrdinalIgnoreCase))
                    .ToList();

                outputMutations[typeof(KnowledgeBaseAPIDocumentsContentParameter)] = filteredKbDocuments;
            }

            return new EWAgenticStepExecutionResult
            {
                InputTokens = agentOutput.InputTokenCount,
                OutputTokens = agentOutput.OutputTokenCount,
                OutputMutations = outputMutations
            };
        }
    }
}
