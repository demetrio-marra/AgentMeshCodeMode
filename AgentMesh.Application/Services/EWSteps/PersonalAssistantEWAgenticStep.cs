using AgentMesh.Application.Models.Parameters;
using AgentMesh.Application.Services.Agents;
using AgentMesh.Models;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class PersonalAssistantEWAgenticStep(
        PersonalAssistantAgent personalAssistantAgent,
        RequestRejectedFlagParameter requestRejectedFlagParameter,
        ExecutionErrorParameter executionErrorParameter,
        PipelineResultDataParameter pipelineResultDataParameter) : IEWAgenticStep
    {
        public string Name => "Personal Assistant";

        public string? AgentName => "PersonalAssistant";

        public bool CountInputTokensAsContextTokens => false;

        public bool CountOutputTokensAsContextTokens => true;

        public IEnumerable<Type> InputParameterTypes => [
            typeof(RequestDateTimeParameter),
            typeof(RequestRejectedFlagParameter),
            typeof(RequestRejectedReasonParameter),
            typeof(ExecutionErrorParameter),
            typeof(PipelineResultDataParameter),
            typeof(LanguageOfTheUserParameter),
            typeof(UserIntentParameter),
            typeof(ConversationTopicParameter),
            typeof(UserPreferencesParameter),
            typeof(UserProvidedDataParameter),
            typeof(UserRequestedActionsParameter),
            typeof(PastMemoriesQueryResultsParameter)
            ];

        public IEnumerable<Type> OutputParameterTypes => [
            typeof(PersonalAssistantOpeningSentenceParameter),
            typeof(PersonalAssistantClosingSentenceParameter),
            typeof(PersonalAssistantDirectAnswerParameter),
            typeof(FinalAnswerParameter)
            ];

        public async Task<EWStepExecutionResult> ExecuteAsync(IReadOnlyDictionary<Type, object?> Values, CancellationToken cancellationToken = default)
        {
            var data = pipelineResultDataParameter.ValueAs(Values[typeof(PipelineResultDataParameter)]);
            var requestFailed = requestRejectedFlagParameter.ValueAs(Values[typeof(RequestRejectedFlagParameter)]);
            var executionError = executionErrorParameter.ValueAs(Values[typeof(ExecutionErrorParameter)]);

            var agentOutput = await personalAssistantAgent.ExecuteAsync(Values, cancellationToken);

            string? finalAnswer;
            if (!string.IsNullOrWhiteSpace(agentOutput.Result.DirectAnswer))
            {
                finalAnswer = agentOutput.Result.DirectAnswer;
            }
            else
            {
                finalAnswer = string.Join(Environment.NewLine + Environment.NewLine,
                    new[] { agentOutput.Result.OpeningSentence, data, agentOutput.Result.ClosingSentence }
                    .Where(s => !string.IsNullOrWhiteSpace(s)));
            }

            return new EWAgenticStepExecutionResult
            {
                InputTokens = agentOutput.InputTokenCount,
                OutputTokens = agentOutput.OutputTokenCount,
                OutputMutations = new Dictionary<Type, object?>
                {
                    { typeof(PersonalAssistantOpeningSentenceParameter), agentOutput.Result.OpeningSentence },
                    { typeof(PersonalAssistantClosingSentenceParameter), agentOutput.Result.ClosingSentence },
                    { typeof(PersonalAssistantDirectAnswerParameter), agentOutput.Result.DirectAnswer },
                    { typeof(FinalAnswerParameter), finalAnswer }
                }
            };
        }
    }
}
