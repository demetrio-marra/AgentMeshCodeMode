using AgentMesh.Application.Configuration;
using AgentMesh.Application.Models.CodeSandbox;
using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Application.Services.EWSteps;
using AgentMesh.Models.RequestAnalysis;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;

namespace AgentMesh.Application.Services
{
    public class EWStepSelector(
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
        FinalAnswerParameter finalAnswerParameter,

        CodeModeWorkflowConfiguration workflowConfiguration,

        RequestAnalyzerEWStep requestAnalyzerEWStep,
        AgentMemoryQueryExpanderEWStep agentMemoryQueryExpanderEWStep,
        AgentMemoryServiceEWStep agentMemoryServiceEWStep,
        KnowledgeBaseQueryExpanderEWStep knowledgeBaseQueryExpanderEWStep,
        DomainsKnowledgeBaseServiceSearchEWStep domainsKnowledgeBaseServiceSearchEWStep,
        RerankerEWStep rerankerEWStep,
        DomainsKnowledgeBaseDocumentsExtractorEWStep domainsKnowledgeBaseDocumentsExtractorEWStep,
        DocumentationEWStep documentationEWStep,
        FunctionalAnalystEWStep functionalAnalystEWStep,
        APIsKnowledgeBaseServiceSearchEWStep apisKnowledgeBaseServiceSearchEWStep,
        APIKnowledgeBaseDocumentsExtractorEWStep apiKnowledgeBaseDocumentsExtractorEWStep,
        TechnicalAnalystEWStep technicalAnalystEWStep,
        CoderEWStep coderEWStep,
        JSSandboxEWStep jsSandboxEWStep,
        CodeExecutionFailuresDetectorEWStep codeExecutionFailuresDetectorEWStep,
        CodeFixerForRuntimeErrorsEWStep codeFixerForRuntimeErrorsEWStep,
        DomainExpertEWStep domainExpertEWStep,
        PersonalAssistantEWStep personalAssistantEWStep) : IEWStepSelector
    {
        public IEnumerable<IEWStep> NextStepsToRun()
        {
            if (userLastRequestParameter.GetDisplayValue() != EWParameterConstants.NoDataPlaceholder
                && intentCategoryParameter.GetDisplayValue() == EWParameterConstants.NoDataPlaceholder)
            {
                return [requestAnalyzerEWStep];
            }
            
            if (missingValuesParameter.GetDisplayValue() != EWParameterConstants.NoDataPlaceholder
                && pastMemoriesQueryParameter.GetDisplayValue() == EWParameterConstants.NoDataPlaceholder)
            {
                return [agentMemoryQueryExpanderEWStep];
            }

            if (pastMemoriesQueryParameter.GetDisplayValue() != EWParameterConstants.NoDataPlaceholder  
                && pastMemoriesQueryResultsParameter.GetDisplayValue() == EWParameterConstants.NoDataPlaceholder)
            {
                return [agentMemoryServiceEWStep];
            }

            if (intentCategoryParameter.GetDisplayValue() != EWParameterConstants.NoDataPlaceholder 
                && intentCategoryParameter.GetDisplayValue() == UserIntentCategory.Other.ToString()
                && finalAnswerParameter.GetDisplayValue() == EWParameterConstants.NoDataPlaceholder)
            {
                return [personalAssistantEWStep];
            }

            return [];
        }
    }
}
