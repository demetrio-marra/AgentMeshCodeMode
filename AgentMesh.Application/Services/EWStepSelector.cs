using AgentMesh.Services;
using AgentMesh.Application.Models.Workflows.Parameters;

namespace AgentMesh.Application.Services
{
    public class EWStepSelector : IEWStepSelector
    {
        private readonly UserLastRequestParameter _userLastRequestParameter;
        private readonly InitialContextMessagesParameter _initialContextMessagesParameter;
        private readonly UserIntentParameter _userIntentParameter;
        private readonly IntentCategoryParameter _intentCategoryParameter;
        private readonly LanguageOfTheUserParameter _languageOfTheUserParameter;
        private readonly ConversationTopicParameter _conversationTopicParameter;
        private readonly UserPreferencesParameter _userPreferencesParameter;
        private readonly UserProvidedDataParameter _userProvidedDataParameter;
        private readonly UserRequestedActionsParameter _userRequestedActionsParameter;
        private readonly MissingValuesParameter _missingValuesParameter;
        private readonly KnowledgeBaseAPIDocumentsContentParameter _knowledgeBaseAPIDocumentsContentParameter;
        private readonly PastMemoriesQueryParameter _pastMemoriesQueryParameter;
        private readonly DomainsKnowledgeBaseQueryParameter _domainsKnowledgeBaseQueryParameter;
        private readonly PastMemoriesQueryResultsParameter _pastMemoriesQueryResultsParameter;
        private readonly KnowledgeBaseQueryResultsParameter _knowledgeBaseQueryResultsParameter;
        private readonly DomainsKnowledgeBaseDocumentsContentParameter _domainsKnowledgeBaseDocumentsContentParameter;
        private readonly BusinessRequirementsParameter _businessRequirementsParameter;
        private readonly FunctionalAnalystRejectedParameter _functionalAnalystRejectedParameter;
        private readonly FunctionalAnalystRejectReasonsParameter _functionalAnalystRejectReasonsParameter;
        private readonly TechnicalSpecificationParameter _technicalSpecificationParameter;
        private readonly TechnicalAnalystRejectedParameter _technicalAnalystRejectedParameter;
        private readonly TechnicalAnalystRejectReasonsParameter _technicalAnalystRejectReasonsParameter;
        private readonly ShouldEngageCoderParameter _shouldEngageCoderParameter;
        private readonly APISKnowledgeBaseQueryResultsParameter _apisKnowledgeBaseQueryResultsParameter;
        private readonly SelectedAPIsFileLocationsParameter _selectedAPIsFileLocationsParameter;
        private readonly DocumentationContentParameter _documentationContentParameter;
        private readonly GeneratedCodeParameter _generatedCodeParameter;
        private readonly LastCodeWithLineNumbersParameter _lastCodeWithLineNumbersParameter;
        private readonly CodeExecutionFailuresDetectorIterationCountParameter _codeExecutionFailuresDetectorIterationCountParameter;
        private readonly CodeExecutionAnalysisParameter _codeExecutionAnalysisParameter;
        private readonly SandboxResultParameter _sandboxResultParameter;
        private readonly SandboxExecutionIdParameter _sandboxExecutionIdParameter;
        private readonly CodeExecutionResultTypeParameter _codeExecutionResultTypeParameter;
        private readonly ExecutionErrorParameter _executionErrorParameter;
        private readonly DomainExpertOutputParameter _domainExpertOutputParameter;
        private readonly PersonalAssistantOpeningSentenceParameter _personalAssistantOpeningSentenceParameter;
        private readonly PersonalAssistantClosingSentenceParameter _personalAssistantClosingSentenceParameter;
        private readonly PersonalAssistantConvenienceErrorSentenceParameter _personalAssistantConvenienceErrorSentenceParameter;
        private readonly FinalAnswerParameter _finalAnswerParameter;

        public EWStepSelector(
            UserLastRequestParameter userLastRequestParameter,
            InitialContextMessagesParameter initialContextMessagesParameter,
            UserIntentParameter userIntentParameter,
            IntentCategoryParameter intentCategoryParameter,
            LanguageOfTheUserParameter languageOfTheUserParameter,
            ConversationTopicParameter conversationTopicParameter,
            UserPreferencesParameter userPreferencesParameter,
            UserProvidedDataParameter userProvidedDataParameter,
            UserRequestedActionsParameter userRequestedActionsParameter,
            MissingValuesParameter missingValuesParameter,
            KnowledgeBaseAPIDocumentsContentParameter knowledgeBaseAPIDocumentsContentParameter,
            PastMemoriesQueryParameter pastMemoriesQueryParameter,
            DomainsKnowledgeBaseQueryParameter domainsKnowledgeBaseQueryParameter,
            PastMemoriesQueryResultsParameter pastMemoriesQueryResultsParameter,
            KnowledgeBaseQueryResultsParameter knowledgeBaseQueryResultsParameter,
            DomainsKnowledgeBaseDocumentsContentParameter domainsKnowledgeBaseDocumentsContentParameter,
            BusinessRequirementsParameter businessRequirementsParameter,
            FunctionalAnalystRejectedParameter functionalAnalystRejectedParameter,
            FunctionalAnalystRejectReasonsParameter functionalAnalystRejectReasonsParameter,
            TechnicalSpecificationParameter technicalSpecificationParameter,
            TechnicalAnalystRejectedParameter technicalAnalystRejectedParameter,
            TechnicalAnalystRejectReasonsParameter technicalAnalystRejectReasonsParameter,
            ShouldEngageCoderParameter shouldEngageCoderParameter,
            APISKnowledgeBaseQueryResultsParameter apisKnowledgeBaseQueryResultsParameter,
            SelectedAPIsFileLocationsParameter selectedAPIsFileLocationsParameter,
            DocumentationContentParameter documentationContentParameter,
            GeneratedCodeParameter generatedCodeParameter,
            LastCodeWithLineNumbersParameter lastCodeWithLineNumbersParameter,
            CodeExecutionFailuresDetectorIterationCountParameter codeExecutionFailuresDetectorIterationCountParameter,
            CodeExecutionAnalysisParameter codeExecutionAnalysisParameter,
            SandboxResultParameter sandboxResultParameter,
            SandboxExecutionIdParameter sandboxExecutionIdParameter,
            CodeExecutionResultTypeParameter codeExecutionResultTypeParameter,
            ExecutionErrorParameter executionErrorParameter,
            DomainExpertOutputParameter domainExpertOutputParameter,
            PersonalAssistantOpeningSentenceParameter personalAssistantOpeningSentenceParameter,
            PersonalAssistantClosingSentenceParameter personalAssistantClosingSentenceParameter,
            PersonalAssistantConvenienceErrorSentenceParameter personalAssistantConvenienceErrorSentenceParameter,
            FinalAnswerParameter finalAnswerParameter)
        {
            _userLastRequestParameter = userLastRequestParameter;
            _initialContextMessagesParameter = initialContextMessagesParameter;
            _userIntentParameter = userIntentParameter;
            _intentCategoryParameter = intentCategoryParameter;
            _languageOfTheUserParameter = languageOfTheUserParameter;
            _conversationTopicParameter = conversationTopicParameter;
            _userPreferencesParameter = userPreferencesParameter;
            _userProvidedDataParameter = userProvidedDataParameter;
            _userRequestedActionsParameter = userRequestedActionsParameter;
            _missingValuesParameter = missingValuesParameter;
            _knowledgeBaseAPIDocumentsContentParameter = knowledgeBaseAPIDocumentsContentParameter;
            _pastMemoriesQueryParameter = pastMemoriesQueryParameter;
            _domainsKnowledgeBaseQueryParameter = domainsKnowledgeBaseQueryParameter;
            _pastMemoriesQueryResultsParameter = pastMemoriesQueryResultsParameter;
            _knowledgeBaseQueryResultsParameter = knowledgeBaseQueryResultsParameter;
            _domainsKnowledgeBaseDocumentsContentParameter = domainsKnowledgeBaseDocumentsContentParameter;
            _businessRequirementsParameter = businessRequirementsParameter;
            _functionalAnalystRejectedParameter = functionalAnalystRejectedParameter;
            _functionalAnalystRejectReasonsParameter = functionalAnalystRejectReasonsParameter;
            _technicalSpecificationParameter = technicalSpecificationParameter;
            _technicalAnalystRejectedParameter = technicalAnalystRejectedParameter;
            _technicalAnalystRejectReasonsParameter = technicalAnalystRejectReasonsParameter;
            _shouldEngageCoderParameter = shouldEngageCoderParameter;
            _apisKnowledgeBaseQueryResultsParameter = apisKnowledgeBaseQueryResultsParameter;
            _selectedAPIsFileLocationsParameter = selectedAPIsFileLocationsParameter;
            _documentationContentParameter = documentationContentParameter;
            _generatedCodeParameter = generatedCodeParameter;
            _lastCodeWithLineNumbersParameter = lastCodeWithLineNumbersParameter;
            _codeExecutionFailuresDetectorIterationCountParameter = codeExecutionFailuresDetectorIterationCountParameter;
            _codeExecutionAnalysisParameter = codeExecutionAnalysisParameter;
            _sandboxResultParameter = sandboxResultParameter;
            _sandboxExecutionIdParameter = sandboxExecutionIdParameter;
            _codeExecutionResultTypeParameter = codeExecutionResultTypeParameter;
            _executionErrorParameter = executionErrorParameter;
            _domainExpertOutputParameter = domainExpertOutputParameter;
            _personalAssistantOpeningSentenceParameter = personalAssistantOpeningSentenceParameter;
            _personalAssistantClosingSentenceParameter = personalAssistantClosingSentenceParameter;
            _personalAssistantConvenienceErrorSentenceParameter = personalAssistantConvenienceErrorSentenceParameter;
            _finalAnswerParameter = finalAnswerParameter;
        }

        public IEnumerable<IEWStep> NextStepsToRun()
        {
            throw new NotImplementedException();
        }
    }
}
