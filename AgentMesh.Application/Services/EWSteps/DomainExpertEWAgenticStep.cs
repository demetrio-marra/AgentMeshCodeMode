using AgentMesh.Application.Models.Parameters;
using AgentMesh.Application.Services.Agents;
using AgentMesh.Models;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class DomainExpertEWAgenticStep(
        DomainExpertAgent domainExpertAgent,
        PipelineResultDataParameter pipelineResultDataParameter) : IEWAgenticStep
    {
        public string Name => "Domain Expert";

        public string? AgentName => "DomainExpert";

        public bool CountInputTokensAsContextTokens => false;

        public bool CountOutputTokensAsContextTokens => false;

        public IEnumerable<Type> InputParameterTypes => [
            typeof(RequestDateTimeParameter),
            typeof(UserIntentParameter),
            typeof(ConversationTopicParameter),
            typeof(UserMentionedEntitiesParameter),
            typeof(UserProvidedDataParameter),
            typeof(UserPreferencesParameter),
            typeof(PastMemoriesQueryResultsParameter),
            typeof(KnowledgeQueryResultParameter),
            typeof(LanguageOfTheUserParameter),
            typeof(PipelineResultDataParameter)
            ];

        public IEnumerable<Type> OutputParameterTypes => [typeof(PipelineResultDataParameter)];

        public async Task<EWStepExecutionResult> ExecuteAsync(IReadOnlyDictionary<Type, object?> Values, CancellationToken cancellationToken = default)
        {
            var agentOutput = await domainExpertAgent.ExecuteAsync(Values, cancellationToken);

            var pipelineResultData = pipelineResultDataParameter.ValueAs(Values[typeof(PipelineResultDataParameter)]);
            var mergedPipelineResultData = $"""
                {pipelineResultData}

                {agentOutput.Result}
                """;

            return new EWAgenticStepExecutionResult
            {
                InputTokens = agentOutput.InputTokenCount,
                OutputTokens = agentOutput.OutputTokenCount,
                OutputMutations = new Dictionary<Type, object?>
                {
                    { typeof(PipelineResultDataParameter), mergedPipelineResultData }
                }
            };
        }
    }
}
