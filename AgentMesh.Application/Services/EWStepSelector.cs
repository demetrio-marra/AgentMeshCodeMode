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

        private bool _rerankerHasRun = false;
        private bool _pipelineDone = false;

        public IEnumerable<IEWStep> NextStepsToRun()
        {
            if (_pipelineDone)
            {
                if (finalAnswerParameter.GetDisplayValue() == EWParameterConstants.NoDataPlaceholder)
                {
                    return [personalAssistantEWStep];
                }
                else
                {
                    return [];
                }
            }

            var pipelineBranch = GuessPipelineBranch();
            return pipelineBranch switch
            {
                PipelineBranchValue.OtherTopics => HandleOtherTopicsBranch(),
                PipelineBranchValue.Documenting => HandleDocumentingBranch(),
                PipelineBranchValue.TaskExecution => HandleTaskExecutionBranch(),
                PipelineBranchValue.None => [requestAnalyzerEWStep],
                _ => [requestAnalyzerEWStep],
            };
        }

        private IEnumerable<IEWStep> HandleOtherTopicsBranch()
        {
            if (pastMemoriesQueryParameter.GetDisplayValue() == EWParameterConstants.NoDataPlaceholder
               && missingValuesParameter.GetDisplayValue() != EWParameterConstants.NoDataPlaceholder)
            {
                return [agentMemoryQueryExpanderEWStep];
            }

            if (pastMemoriesQueryResultsParameter.GetDisplayValue() == EWParameterConstants.NoDataPlaceholder
                && pastMemoriesQueryParameter.GetDisplayValue() != EWParameterConstants.NoDataPlaceholder)
            {
                return [agentMemoryServiceEWStep];
            }

            if (finalAnswerParameter.GetDisplayValue() == EWParameterConstants.NoDataPlaceholder)
            {
                return [personalAssistantEWStep];
            }
            else
            {
                return [];
            }
        }

        private IEnumerable<IEWStep> HandleDocumentingBranch()
        {
            if (pastMemoriesQueryParameter.GetDisplayValue() == EWParameterConstants.NoDataPlaceholder
                && missingValuesParameter.GetDisplayValue() != EWParameterConstants.NoDataPlaceholder)
            {
                return [agentMemoryQueryExpanderEWStep];
            }
            
            if (pastMemoriesQueryResultsParameter.GetDisplayValue() == EWParameterConstants.NoDataPlaceholder
                && pastMemoriesQueryParameter.GetDisplayValue() != EWParameterConstants.NoDataPlaceholder)
            {
                return [agentMemoryServiceEWStep];
            }

            if (domainsKnowledgeBaseQueryParameter.GetDisplayValue() == EWParameterConstants.NoDataPlaceholder)
            {
                return [knowledgeBaseQueryExpanderEWStep];
            }

            if (domainsKnowledgeBaseQueryParameter.GetDisplayValue() != EWParameterConstants.NoDataPlaceholder
                && knowledgeBaseQueryResultsParameter.GetDisplayValue() == EWParameterConstants.NoDataPlaceholder)
            {
                return [domainsKnowledgeBaseServiceSearchEWStep];
            }

            if (_rerankerHasRun == false)
            {
                _rerankerHasRun = true;
                return [rerankerEWStep];
            }

            if (domainsKnowledgeBaseDocumentsContentParameter.GetDisplayValue() == EWParameterConstants.NoDataPlaceholder
                && domainsKnowledgeBaseQueryParameter.GetDisplayValue() != EWParameterConstants.NoDataPlaceholder)
            {
                return [domainsKnowledgeBaseDocumentsExtractorEWStep];
            }

            _pipelineDone = true;
            return [documentationEWStep];
        }
        
        private IEnumerable<IEWStep> HandleTaskExecutionBranch()
        {
            return [];
        }

        private PipelineBranchValue GuessPipelineBranch()
        {
            if (!intentCategoryParameter.ParameterValue.HasValue)
            {
                return PipelineBranchValue.None;
            }
            else
            {
                return intentCategoryParameter.ParameterValue.Value switch
                {
                    UserIntentCategory.Other => PipelineBranchValue.OtherTopics,
                    UserIntentCategory.Documentation => PipelineBranchValue.Documenting,
                    UserIntentCategory.TaskExecution => PipelineBranchValue.TaskExecution,
                    _ => PipelineBranchValue.None
                };
            }
        }

        private enum PipelineBranchValue
        {
            None,
            OtherTopics,
            Documenting,
            TaskExecution
        }

        private enum OtherTopicsSteps
        {
            None,
            RequestAnalyzer,
            PersonalAssistantEWStep
        }

        private enum DocumentingSteps
        {
            None,
            RequestAnalyzer,
            AgentMemoryQueryExpander,
            AgentMemoryService,
            KnowledgeBaseQueryExpander,
            DomainsKnowledgeBaseServiceSearch,
            Reranker,
            DomainsKnowledgeBaseDocumentsExtractor,
            Documentation,
            PersonalAssistantEWStep
        }

        private enum TaskExecutionSteps
        {
            None,
            RequestAnalyzer,
            AgentMemoryQueryExpander,
            AgentMemoryService,
            KnowledgeBaseQueryExpander,
            DomainsKnowledgeBaseServiceSearch,
            Reranker,
            DomainsKnowledgeBaseDocumentsExtractor,
            FunctionalAnalyst,
            APIsKnowledgeBaseServiceSearch,
            APIKnowledgeBaseDocumentsExtractor,
            TechnicalAnalyst,
            Coder,
            JSSandbox,
            CodeExecutionFailuresDetector,
            CodeFixerForRuntimeErrors,
            DomainExpert,
            PersonalAssistantEWStep 
        }
    }
}
