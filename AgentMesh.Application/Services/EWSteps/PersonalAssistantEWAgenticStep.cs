using AgentMesh.Application.Models.CodeSandbox;
using AgentMesh.Application.Models.PersonalAssistant;
using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Application.Services.Agents;
using AgentMesh.Models.RequestAnalysis;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class PersonalAssistantEWAgenticStep(
        PersonalAssistantAgent personalAssistantAgent,
        UserIntentParameter userIntentParameter,
        IntentCategoryParameter intentCategoryParameter,
        ConversationTopicParameter conversationTopicParameter,
        UserPreferencesParameter userPreferencesParameter,
        UserProvidedDataParameter userProvidedDataParameter,
        UserRequestedActionsParameter userRequestedActionsParameter,
        LanguageOfTheUserParameter languageOfTheUserParameter,
        PastMemoriesQueryResultsParameter pastMemoriesQueryResultsParameter,
        FunctionalAnalystRejectedParameter functionalAnalystRejectedParameter,
        FunctionalAnalystRejectReasonsParameter functionalAnalystRejectReasonsParameter,
        TechnicalAnalystRejectedParameter technicalAnalystRejectedParameter,
        TechnicalAnalystRejectReasonsParameter technicalAnalystRejectReasonsParameter,
        CodeExecutionResultTypeParameter codeExecutionResultTypeParameter,
        SandboxResultParameter sandboxResultParameter,
        DomainExpertOutputParameter domainExpertOutputParameter,
        DocumentationContentParameter documentationContentParameter,
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
            var intentCategory = intentCategoryParameter.ParameterValue ?? UserIntentCategory.Other;
            var faRejected = functionalAnalystRejectedParameter.ParameterValue ?? false;
            var taRejected = technicalAnalystRejectedParameter.ParameterValue;
            var codeExecResultType = codeExecutionResultTypeParameter.ParameterValue;

            string? data = null;
            var requestFailed = false;
            string? requestFailureReason = null;

            if (intentCategory == UserIntentCategory.Documentation)
            {
                data = documentationContentParameter.ParameterValue;
            }
            else if (intentCategory == UserIntentCategory.TaskExecution)
            {
                if (faRejected)
                {
                    requestFailed = true;
                    requestFailureReason = $"""
                        The request made by the user was rejected. The reason for rejection is as follows:
                        {functionalAnalystRejectReasonsParameter.ParameterValue}
                        """;
                }
                else if (taRejected)
                {
                    requestFailed = true;
                    requestFailureReason = $"""
                        The request made by the user was rejected. The reason for rejection is as follows:
                        {technicalAnalystRejectReasonsParameter.ParameterValue}
                        """;
                }
                else if (codeExecResultType != SandboxResultType.Success)
                {
                    requestFailed = true;
                    requestFailureReason = sandboxResultParameter.ParameterValue;
                }
                else
                {
                    data = sandboxResultParameter.ParameterValue;

                    var domainExpertOutput = domainExpertOutputParameter.ParameterValue;
                    if (!string.IsNullOrEmpty(domainExpertOutput))
                    {
                        data += $"""

                            {domainExpertOutput}
                            """;
                    }
                }
            }

            var agentInput = new PersonalAssistantAgentInput
            {
                Data = data,
                RequestFailed = requestFailed,
                RequestFailureReason = requestFailureReason,
                LanguageOfTheUser = languageOfTheUserParameter.ParameterValue,
                CanonicalizedIntent = userIntentParameter.ParameterValue ?? string.Empty,
                ConversationTopic = conversationTopicParameter.ParameterValue ?? string.Empty,
                UserPreferences = userPreferencesParameter.ParameterValue ?? [],
                UserProvidedData = userProvidedDataParameter.ParameterValue ?? [],
                UserRequestedActions = userRequestedActionsParameter.ParameterValue ?? [],
                Memories = (pastMemoriesQueryResultsParameter.ParameterValue ?? []).Select(m => m.Memory)
            };

            var agentOutput = await personalAssistantAgent.ExecuteAsync(agentInput, cancellationToken);

            personalAssistantOpeningSentenceParameter.ParameterValue = agentOutput.OpeningSentence;
            personalAssistantClosingSentenceParameter.ParameterValue = agentOutput.ClosingSentence;
            personalAssistantConvenienceErrorSentenceParameter.ParameterValue = agentOutput.ConvenienceErrorSentence;

            string? finalAnswer;
            if (requestFailed)
            {
                finalAnswer = agentOutput.ConvenienceErrorSentence;
            }
            else
            {
                finalAnswer = string.Join(Environment.NewLine + Environment.NewLine,
                    new[] { agentOutput.OpeningSentence, data, agentOutput.ClosingSentence }
                    .Where(s => !string.IsNullOrWhiteSpace(s)));
            }

            finalAnswerParameter.ParameterValue = finalAnswer;

            return new EWAgenticStepResultRecord(agentOutput.InputTokenCount, agentOutput.OutputTokenCount);
        }
    }
}
