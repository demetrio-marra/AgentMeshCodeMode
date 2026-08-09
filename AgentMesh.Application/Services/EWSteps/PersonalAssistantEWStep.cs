using AgentMesh.Application.Configuration;
using AgentMesh.Application.Models.CodeSandbox;
using AgentMesh.Application.Models.PersonalAssistant;
using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Models.RequestAnalysis;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.EWSteps
{
    public class PersonalAssistantEWStep(
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
        FinalAnswerParameter finalAnswerParameter) : IEWStep
    {
        public string Name => "Personal Assistant";

        public bool IsAgentic => true;

        public string? AgentName => PersonalAssistantAgentConfiguration.AgentName;

        public bool IsPipelineFirst => false;

        public bool IsPipelineLast => true;

        private readonly PersonalAssistantAgent _personalAssistantAgent = personalAssistantAgent;
        private readonly UserIntentParameter _userIntentParameter = userIntentParameter;
        private readonly IntentCategoryParameter _intentCategoryParameter = intentCategoryParameter;
        private readonly ConversationTopicParameter _conversationTopicParameter = conversationTopicParameter;
        private readonly UserPreferencesParameter _userPreferencesParameter = userPreferencesParameter;
        private readonly UserProvidedDataParameter _userProvidedDataParameter = userProvidedDataParameter;
        private readonly UserRequestedActionsParameter _userRequestedActionsParameter = userRequestedActionsParameter;
        private readonly LanguageOfTheUserParameter _languageOfTheUserParameter = languageOfTheUserParameter;
        private readonly PastMemoriesQueryResultsParameter _pastMemoriesQueryResultsParameter = pastMemoriesQueryResultsParameter;
        private readonly FunctionalAnalystRejectedParameter _functionalAnalystRejectedParameter = functionalAnalystRejectedParameter;
        private readonly FunctionalAnalystRejectReasonsParameter _functionalAnalystRejectReasonsParameter = functionalAnalystRejectReasonsParameter;
        private readonly TechnicalAnalystRejectedParameter _technicalAnalystRejectedParameter = technicalAnalystRejectedParameter;
        private readonly TechnicalAnalystRejectReasonsParameter _technicalAnalystRejectReasonsParameter = technicalAnalystRejectReasonsParameter;
        private readonly CodeExecutionResultTypeParameter _codeExecutionResultTypeParameter = codeExecutionResultTypeParameter;
        private readonly SandboxResultParameter _sandboxResultParameter = sandboxResultParameter;
        private readonly DomainExpertOutputParameter _domainExpertOutputParameter = domainExpertOutputParameter;
        private readonly DocumentationContentParameter _documentationContentParameter = documentationContentParameter;
        private readonly PersonalAssistantOpeningSentenceParameter _personalAssistantOpeningSentenceParameter = personalAssistantOpeningSentenceParameter;
        private readonly PersonalAssistantClosingSentenceParameter _personalAssistantClosingSentenceParameter = personalAssistantClosingSentenceParameter;
        private readonly PersonalAssistantConvenienceErrorSentenceParameter _personalAssistantConvenienceErrorSentenceParameter = personalAssistantConvenienceErrorSentenceParameter;
        private readonly FinalAnswerParameter _finalAnswerParameter = finalAnswerParameter;

        public async Task<EWStepResultRecord> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var intentCategory = _intentCategoryParameter.ParameterValue ?? UserIntentCategory.Other;
            var faRejected = _functionalAnalystRejectedParameter.ParameterValue ?? false;
            var taRejected = _technicalAnalystRejectedParameter.ParameterValue;
            var codeExecResultType = _codeExecutionResultTypeParameter.ParameterValue;

            string? data = null;
            var requestFailed = false;
            string? requestFailureReason = null;

            if (intentCategory == UserIntentCategory.Documentation)
            {
                data = _documentationContentParameter.ParameterValue;
            }
            else if (intentCategory == UserIntentCategory.TaskExecution)
            {
                if (faRejected)
                {
                    requestFailed = true;
                    requestFailureReason = $"""
                        The request made by the user was rejected. The reason for rejection is as follows:
                        {_functionalAnalystRejectReasonsParameter.ParameterValue}
                        """;
                }
                else if (taRejected)
                {
                    requestFailed = true;
                    requestFailureReason = $"""
                        The request made by the user was rejected. The reason for rejection is as follows:
                        {_technicalAnalystRejectReasonsParameter.ParameterValue}
                        """;
                }
                else if (codeExecResultType != SandboxResultType.Success)
                {
                    requestFailed = true;
                    requestFailureReason = _sandboxResultParameter.ParameterValue;
                }
                else
                {
                    data = _sandboxResultParameter.ParameterValue;

                    var domainExpertOutput = _domainExpertOutputParameter.ParameterValue;
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
                LanguageOfTheUser = _languageOfTheUserParameter.ParameterValue,
                CanonicalizedIntent = _userIntentParameter.ParameterValue ?? string.Empty,
                ConversationTopic = _conversationTopicParameter.ParameterValue ?? string.Empty,
                UserPreferences = _userPreferencesParameter.ParameterValue ?? [],
                UserProvidedData = _userProvidedDataParameter.ParameterValue ?? [],
                UserRequestedActions = _userRequestedActionsParameter.ParameterValue ?? [],
                Memories = (_pastMemoriesQueryResultsParameter.ParameterValue ?? []).Select(m => m.Memory)
            };

            var agentOutput = await _personalAssistantAgent.ExecuteAsync(agentInput, cancellationToken);

            _personalAssistantOpeningSentenceParameter.ParameterValue = agentOutput.OpeningSentence;
            _personalAssistantClosingSentenceParameter.ParameterValue = agentOutput.ClosingSentence;
            _personalAssistantConvenienceErrorSentenceParameter.ParameterValue = agentOutput.ConvenienceErrorSentence;

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

            _finalAnswerParameter.ParameterValue = finalAnswer;

            return new EWStepResultRecord(agentOutput.InputTokenCount, agentOutput.OutputTokenCount);
        }
    }
}
