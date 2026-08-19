using AgentMesh.Application.Models.Parameters;
using AgentMesh.Application.Services.Agents;
using AgentMesh.Models;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class FunctionalAnalystEWAgenticStep(
        FunctionalAnalystAgent functionalAnalystAgent) : IEWAgenticStep
    {
        public string Name => "Functional Analyst";

        public string? AgentName => "FunctionalAnalyst";

        public bool CountInputTokensAsContextTokens => false;

        public bool CountOutputTokensAsContextTokens => false;

        public IEnumerable<Type> InputParameterTypes => [
            typeof(RequestDateTimeParameter),
            typeof(UserIntentParameter),
            typeof(ConversationTopicParameter),
            typeof(UserRequestedActionsParameter),
            typeof(UserProvidedDataParameter),
            typeof(UserPreferencesParameter),
            typeof(PastMemoriesQueryResultsParameter),
            typeof(DomainsKnowledgeBaseDocumentsContentParameter)
            ];

        public IEnumerable<Type> OutputParameterTypes => [
            typeof(BusinessRequirementsParameter),
            typeof(RequestRejectedFlagParameter),
            typeof(RequestRejectedReasonParameter)
            ];

        public async Task<EWStepExecutionResult> ExecuteAsync(IReadOnlyDictionary<Type, object?> Values, CancellationToken cancellationToken = default)
        {
            var agentOutput = await functionalAnalystAgent.ExecuteAsync(Values, cancellationToken);

            var outputMutations = new Dictionary<Type, object?>
            {
                { typeof(BusinessRequirementsParameter), agentOutput.Result.BusinessRequirements },
                { typeof(RequestRejectedFlagParameter), agentOutput.Result.RequestRejected }
            };

            if (agentOutput.Result.RequestRejected && !string.IsNullOrWhiteSpace(agentOutput.Result.ReasonOfRejection))
            {
                outputMutations[typeof(RequestRejectedReasonParameter)] = agentOutput.Result.ReasonOfRejection;
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
