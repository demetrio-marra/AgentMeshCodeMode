using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Application.Services.Agents;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class PersonalAssistantEWAgenticStep(
        PersonalAssistantAgent personalAssistantAgent,
        RequestDateTimeParameter requestDateTimeParameter,
        UserIntentParameter userIntentParameter,
        ConversationTopicParameter conversationTopicParameter,
        UserPreferencesParameter userPreferencesParameter,
        UserProvidedDataParameter userProvidedDataParameter,
        UserRequestedActionsParameter userRequestedActionsParameter,
        LanguageOfTheUserParameter languageOfTheUserParameter,
        PastMemoriesQueryResultsParameter pastMemoriesQueryResultsParameter,
        RequestRejectedReasonParameter requestRejectedReasonParameter,
        RequestRejectedFlagParameter requestRejectedFlagParameter,
        PipelineResultDataParameter pipelineResultDataParameter,
        ExecutionErrorParameter executionErrorParameter,
        PersonalAssistantOpeningSentenceParameter personalAssistantOpeningSentenceParameter,
        PersonalAssistantClosingSentenceParameter personalAssistantClosingSentenceParameter,
        PersonalAssistantConvenienceErrorSentenceParameter personalAssistantConvenienceErrorSentenceParameter,
        FinalAnswerParameter finalAnswerParameter) : IEWAgenticStep
    {
        public string Name => "Personal Assistant";

        public string? AgentName => "PersonalAssistant";

        public bool IsInputTokensCountSource => false;

        public bool IsOutputTokensCountSource => true;

       
        public async Task<EWAgenticStepResultRecord> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var data = pipelineResultDataParameter.ParameterValue;
            var requestFailed = requestRejectedFlagParameter.ParameterValue;

            var agentOutput = await personalAssistantAgent.ExecuteAsync([
                requestDateTimeParameter,
                requestRejectedFlagParameter,
                requestRejectedReasonParameter,
                executionErrorParameter,
                pipelineResultDataParameter,
                languageOfTheUserParameter,
                userIntentParameter,
                conversationTopicParameter,
                userPreferencesParameter,
                userProvidedDataParameter,
                userRequestedActionsParameter,
                pastMemoriesQueryResultsParameter
                ], cancellationToken);

            personalAssistantOpeningSentenceParameter.ParameterValue = agentOutput.Result.OpeningSentence;
            personalAssistantClosingSentenceParameter.ParameterValue = agentOutput.Result.ClosingSentence;
            personalAssistantConvenienceErrorSentenceParameter.ParameterValue = agentOutput.Result.ConvenienceErrorSentence;

            string? finalAnswer;
            if (requestFailed)
            {
                finalAnswer = agentOutput.Result.ConvenienceErrorSentence;
            }
            else
            {
                finalAnswer = string.Join(Environment.NewLine + Environment.NewLine,
                    new[] { agentOutput.Result.OpeningSentence, data, agentOutput.Result.ClosingSentence }
                    .Where(s => !string.IsNullOrWhiteSpace(s)));
            }

            finalAnswerParameter.ParameterValue = finalAnswer;

            return new EWAgenticStepResultRecord(agentOutput.InputTokenCount, agentOutput.OutputTokenCount);
        }
    }
}
