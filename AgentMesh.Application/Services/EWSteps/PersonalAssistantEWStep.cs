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
        EWParametersProvider ewParametersProvider) : IEWStep
    {
        public string Name => "Personal Assistant";

        public bool IsAgentic => true;

        public string? AgentName => PersonalAssistantAgentConfiguration.AgentName;

        public bool IsPipelineFirst => false;

        public bool IsPipelineLast => true;

        public IEnumerable<string> InputParameters => [
            EWParameterNames.UserIntent,
            EWParameterNames.IntentCategory,
            EWParameterNames.ConversationTopic,
            EWParameterNames.UserPreferences,
            EWParameterNames.UserProvidedData,
            EWParameterNames.UserRequestedActions,
            EWParameterNames.LanguageOfTheUser,
            EWParameterNames.PastMemoriesQueryResults,
            EWParameterNames.FunctionalAnalystRejected,
            EWParameterNames.FunctionalAnalystRejectReasons,
            EWParameterNames.TechnicalAnalystRejected,
            EWParameterNames.TechnicalAnalystRejectReasons,
            EWParameterNames.CodeExecutionResultType,
            EWParameterNames.SandboxResult,
            EWParameterNames.DomainExpertOutput,
            EWParameterNames.DocumentationContent
        ];

        private readonly PersonalAssistantAgent _personalAssistantAgent = personalAssistantAgent;
        private readonly EWParametersProvider _ewParametersProvider = ewParametersProvider;

        public async Task<EWStepResultRecord> ExecuteAsync(IEnumerable<IEWParameter> inputParameters, CancellationToken cancellationToken = default)
        {
            var intentParameter = inputParameters.Single(p => p.Name == EWParameterNames.UserIntent);
            if (intentParameter is not UserIntentParameter typedIntent)
                throw new InvalidOperationException($"Parameter {EWParameterNames.UserIntent} is not of type UserIntentParameter");

            var intentCategoryParameter = inputParameters.Single(p => p.Name == EWParameterNames.IntentCategory);
            if (intentCategoryParameter is not IntentCategoryParameter typedIntentCategory)
                throw new InvalidOperationException($"Parameter {EWParameterNames.IntentCategory} is not of type IntentCategoryParameter");

            var topicParameter = inputParameters.Single(p => p.Name == EWParameterNames.ConversationTopic);
            if (topicParameter is not ConversationTopicParameter typedTopic)
                throw new InvalidOperationException($"Parameter {EWParameterNames.ConversationTopic} is not of type ConversationTopicParameter");

            var preferencesParameter = inputParameters.Single(p => p.Name == EWParameterNames.UserPreferences);
            if (preferencesParameter is not UserPreferencesParameter typedPreferences)
                throw new InvalidOperationException($"Parameter {EWParameterNames.UserPreferences} is not of type UserPreferencesParameter");

            var providedDataParameter = inputParameters.Single(p => p.Name == EWParameterNames.UserProvidedData);
            if (providedDataParameter is not UserProvidedDataParameter typedProvidedData)
                throw new InvalidOperationException($"Parameter {EWParameterNames.UserProvidedData} is not of type UserProvidedDataParameter");

            var requestedActionsParameter = inputParameters.Single(p => p.Name == EWParameterNames.UserRequestedActions);
            if (requestedActionsParameter is not UserRequestedActionsParameter typedRequestedActions)
                throw new InvalidOperationException($"Parameter {EWParameterNames.UserRequestedActions} is not of type UserRequestedActionsParameter");

            var languageParameter = inputParameters.Single(p => p.Name == EWParameterNames.LanguageOfTheUser);
            if (languageParameter is not LanguageOfTheUserParameter typedLanguage)
                throw new InvalidOperationException($"Parameter {EWParameterNames.LanguageOfTheUser} is not of type LanguageOfTheUserParameter");

            var memoriesParameter = inputParameters.Single(p => p.Name == EWParameterNames.PastMemoriesQueryResults);
            if (memoriesParameter is not PastMemoriesQueryResultsParameter typedMemories)
                throw new InvalidOperationException($"Parameter {EWParameterNames.PastMemoriesQueryResults} is not of type PastMemoriesQueryResultsParameter");

            var faRejectedParameter = inputParameters.Single(p => p.Name == EWParameterNames.FunctionalAnalystRejected);
            if (faRejectedParameter is not FunctionalAnalystRejectedParameter typedFaRejected)
                throw new InvalidOperationException($"Parameter {EWParameterNames.FunctionalAnalystRejected} is not of type FunctionalAnalystRejectedParameter");

            var faRejectReasonsParameter = inputParameters.Single(p => p.Name == EWParameterNames.FunctionalAnalystRejectReasons);
            if (faRejectReasonsParameter is not FunctionalAnalystRejectReasonsParameter typedFaRejectReasons)
                throw new InvalidOperationException($"Parameter {EWParameterNames.FunctionalAnalystRejectReasons} is not of type FunctionalAnalystRejectReasonsParameter");

            var taRejectedParameter = inputParameters.Single(p => p.Name == EWParameterNames.TechnicalAnalystRejected);
            if (taRejectedParameter is not TechnicalAnalystRejectedParameter typedTaRejected)
                throw new InvalidOperationException($"Parameter {EWParameterNames.TechnicalAnalystRejected} is not of type TechnicalAnalystRejectedParameter");

            var taRejectReasonsParameter = inputParameters.Single(p => p.Name == EWParameterNames.TechnicalAnalystRejectReasons);
            if (taRejectReasonsParameter is not TechnicalAnalystRejectReasonsParameter typedTaRejectReasons)
                throw new InvalidOperationException($"Parameter {EWParameterNames.TechnicalAnalystRejectReasons} is not of type TechnicalAnalystRejectReasonsParameter");

            var codeExecResultTypeParameter = inputParameters.Single(p => p.Name == EWParameterNames.CodeExecutionResultType);
            if (codeExecResultTypeParameter is not CodeExecutionResultTypeParameter typedCodeExecResultType)
                throw new InvalidOperationException($"Parameter {EWParameterNames.CodeExecutionResultType} is not of type CodeExecutionResultTypeParameter");

            var sandboxResultParameter = inputParameters.Single(p => p.Name == EWParameterNames.SandboxResult);
            if (sandboxResultParameter is not SandboxResultParameter typedSandboxResult)
                throw new InvalidOperationException($"Parameter {EWParameterNames.SandboxResult} is not of type SandboxResultParameter");

            var domainExpertOutputParameter = inputParameters.Single(p => p.Name == EWParameterNames.DomainExpertOutput);
            if (domainExpertOutputParameter is not DomainExpertOutputParameter typedDomainExpertOutput)
                throw new InvalidOperationException($"Parameter {EWParameterNames.DomainExpertOutput} is not of type DomainExpertOutputParameter");

            var documentationContentParameter = inputParameters.Single(p => p.Name == EWParameterNames.DocumentationContent);
            if (documentationContentParameter is not DocumentationContentParameter typedDocumentationContent)
                throw new InvalidOperationException($"Parameter {EWParameterNames.DocumentationContent} is not of type DocumentationContentParameter");

            var intentCategory = typedIntentCategory.ParameterValue ?? UserIntentCategory.Other;
            var faRejected = typedFaRejected.ParameterValue ?? false;
            var taRejected = typedTaRejected.ParameterValue;
            var codeExecResultType = typedCodeExecResultType.ParameterValue;

            string? data = null;
            var requestFailed = false;
            string? requestFailureReason = null;

            if (intentCategory == UserIntentCategory.Documentation)
            {
                var docContent = (typedDocumentationContent.ParameterValue ?? []).FirstOrDefault();
                data = docContent?.Content;
            }
            else if (intentCategory == UserIntentCategory.TaskExecution)
            {
                if (faRejected)
                {
                    requestFailed = true;
                    requestFailureReason = $"""
                        The request made by the user was rejected. The reason for rejection is as follows:
                        {typedFaRejectReasons.ParameterValue}
                        """;
                }
                else if (taRejected)
                {
                    requestFailed = true;
                    requestFailureReason = $"""
                        The request made by the user was rejected. The reason for rejection is as follows:
                        {typedTaRejectReasons.ParameterValue}
                        """;
                }
                else if (codeExecResultType != SandboxResultType.Success)
                {
                    requestFailed = true;
                    requestFailureReason = typedSandboxResult.ParameterValue;
                }
                else
                {
                    data = typedSandboxResult.ParameterValue;

                    var domainExpertOutput = typedDomainExpertOutput.ParameterValue;
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
                LanguageOfTheUser = typedLanguage.ParameterValue,
                CanonicalizedIntent = typedIntent.ParameterValue ?? string.Empty,
                ConversationTopic = typedTopic.ParameterValue ?? string.Empty,
                UserPreferences = typedPreferences.ParameterValue ?? [],
                UserProvidedData = typedProvidedData.ParameterValue ?? [],
                UserRequestedActions = typedRequestedActions.ParameterValue ?? [],
                Memories = (typedMemories.ParameterValue ?? []).Select(m => m.Memory)
            };

            var agentOutput = await _personalAssistantAgent.ExecuteAsync(agentInput, cancellationToken);

            _ewParametersProvider.UpdateParameterValue(EWParameterNames.PersonalAssistantOpeningSentence, agentOutput.OpeningSentence);
            _ewParametersProvider.UpdateParameterValue(EWParameterNames.PersonalAssistantClosingSentence, agentOutput.ClosingSentence);
            _ewParametersProvider.UpdateParameterValue(EWParameterNames.PersonalAssistantConvenienceErrorSentence, agentOutput.ConvenienceErrorSentence);

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

            _ewParametersProvider.UpdateParameterValue(EWParameterNames.FinalAnswer, finalAnswer);

            return new EWStepResultRecord(agentOutput.InputTokenCount, agentOutput.OutputTokenCount);
        }
    }
}
