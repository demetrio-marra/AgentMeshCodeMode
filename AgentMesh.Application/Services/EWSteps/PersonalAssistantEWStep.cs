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

        private readonly PersonalAssistantAgent personalAssistantAgent = personalAssistantAgent;
        private readonly UserIntentParameter userIntentParameter = userIntentParameter;
        private readonly IntentCategoryParameter intentCategoryParameter = intentCategoryParameter;
        private readonly ConversationTopicParameter conversationTopicParameter = conversationTopicParameter;
        private readonly UserPreferencesParameter userPreferencesParameter = userPreferencesParameter;
        private readonly UserProvidedDataParameter userProvidedDataParameter = userProvidedDataParameter;
        private readonly UserRequestedActionsParameter userRequestedActionsParameter = userRequestedActionsParameter;
        private readonly LanguageOfTheUserParameter languageOfTheUserParameter = languageOfTheUserParameter;
        private readonly PastMemoriesQueryResultsParameter pastMemoriesQueryResultsParameter = pastMemoriesQueryResultsParameter;
        private readonly FunctionalAnalystRejectedParameter functionalAnalystRejectedParameter = functionalAnalystRejectedParameter;
        private readonly FunctionalAnalystRejectReasonsParameter functionalAnalystRejectReasonsParameter = functionalAnalystRejectReasonsParameter;
        private readonly TechnicalAnalystRejectedParameter technicalAnalystRejectedParameter = technicalAnalystRejectedParameter;
        private readonly TechnicalAnalystRejectReasonsParameter technicalAnalystRejectReasonsParameter = technicalAnalystRejectReasonsParameter;
        private readonly CodeExecutionResultTypeParameter codeExecutionResultTypeParameter = codeExecutionResultTypeParameter;
        private readonly SandboxResultParameter sandboxResultParameter = sandboxResultParameter;
        private readonly DomainExpertOutputParameter domainExpertOutputParameter = domainExpertOutputParameter;
        private readonly DocumentationContentParameter documentationContentParameter = documentationContentParameter;
        private readonly PersonalAssistantOpeningSentenceParameter personalAssistantOpeningSentenceParameter = personalAssistantOpeningSentenceParameter;
        private readonly PersonalAssistantClosingSentenceParameter personalAssistantClosingSentenceParameter = personalAssistantClosingSentenceParameter;
        private readonly PersonalAssistantConvenienceErrorSentenceParameter personalAssistantConvenienceErrorSentenceParameter = personalAssistantConvenienceErrorSentenceParameter;
        private readonly FinalAnswerParameter finalAnswerParameter = finalAnswerParameter;

        public async Task<EWStepResultRecord> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            var intentCategory = this.intentCategoryParameter.ParameterValue ?? UserIntentCategory.Other;
            var faRejected = this.functionalAnalystRejectedParameter.ParameterValue ?? false;
            var taRejected = this.technicalAnalystRejectedParameter.ParameterValue;
            var codeExecResultType = this.codeExecutionResultTypeParameter.ParameterValue;

            string? data = null;
            var requestFailed = false;
            string? requestFailureReason = null;

            if (intentCategory == UserIntentCategory.Documentation)
            {
                data = this.documentationContentParameter.ParameterValue;
            }
            else if (intentCategory == UserIntentCategory.TaskExecution)
            {
                if (faRejected)
                {
                    requestFailed = true;
                    requestFailureReason = $"""
                        The request made by the user was rejected. The reason for rejection is as follows:
                        {this.functionalAnalystRejectReasonsParameter.ParameterValue}
                        """;
                }
                else if (taRejected)
                {
                    requestFailed = true;
                    requestFailureReason = $"""
                        The request made by the user was rejected. The reason for rejection is as follows:
                        {this.technicalAnalystRejectReasonsParameter.ParameterValue}
                        """;
                }
                else if (codeExecResultType != SandboxResultType.Success)
                {
                    requestFailed = true;
                    requestFailureReason = this.sandboxResultParameter.ParameterValue;
                }
                else
                {
                    data = this.sandboxResultParameter.ParameterValue;

                    var domainExpertOutput = this.domainExpertOutputParameter.ParameterValue;
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
                LanguageOfTheUser = this.languageOfTheUserParameter.ParameterValue,
                CanonicalizedIntent = this.userIntentParameter.ParameterValue ?? string.Empty,
                ConversationTopic = this.conversationTopicParameter.ParameterValue ?? string.Empty,
                UserPreferences = this.userPreferencesParameter.ParameterValue ?? [],
                UserProvidedData = this.userProvidedDataParameter.ParameterValue ?? [],
                UserRequestedActions = this.userRequestedActionsParameter.ParameterValue ?? [],
                Memories = (this.pastMemoriesQueryResultsParameter.ParameterValue ?? []).Select(m => m.Memory)
            };

            var agentOutput = await this.personalAssistantAgent.ExecuteAsync(agentInput, cancellationToken);

            this.personalAssistantOpeningSentenceParameter.ParameterValue = agentOutput.OpeningSentence;
            this.personalAssistantClosingSentenceParameter.ParameterValue = agentOutput.ClosingSentence;
            this.personalAssistantConvenienceErrorSentenceParameter.ParameterValue = agentOutput.ConvenienceErrorSentence;

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

            this.finalAnswerParameter.ParameterValue = finalAnswer;

            return new EWStepResultRecord(agentOutput.InputTokenCount, agentOutput.OutputTokenCount);
        }
    }
}
