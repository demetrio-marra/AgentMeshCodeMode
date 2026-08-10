using AgentMesh.Application.Configuration;
using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Application.Services.EWSteps;
using AgentMesh.Models.RequestAnalysis;
using AgentMesh.Models.Workflows;
using AgentMesh.Services;

namespace AgentMesh.Application.Services
{
    public class EWStepSelector(
        IntentCategoryParameter intentCategoryParameter,
        MissingValuesParameter missingValuesParameter,
        KnowledgeBaseAPIDocumentsContentParameter knowledgeBaseAPIDocumentsContentParameter,
        PastMemoriesQueryParameter pastMemoriesQueryParameter,
        DomainsKnowledgeBaseQueryParameter domainsKnowledgeBaseQueryParameter,
        PastMemoriesQueryResultsParameter pastMemoriesQueryResultsParameter,
        KnowledgeBaseQueryResultsParameter knowledgeBaseQueryResultsParameter,
        DomainsKnowledgeBaseDocumentsContentParameter domainsKnowledgeBaseDocumentsContentParameter,
        BusinessRequirementsParameter businessRequirementsParameter,
        TechnicalSpecificationParameter technicalSpecificationParameter,
        TechnicalAnalystRejectedParameter technicalAnalystRejectedParameter,
        APISKnowledgeBaseQueryResultsParameter apisKnowledgeBaseQueryResultsParameter,
        DocumentationContentParameter documentationContentParameter,
        GeneratedCodeParameter generatedCodeParameter,
        SandboxResultParameter sandboxResultParameter,
        DomainExpertOutputParameter domainExpertOutputParameter,
        FinalAnswerParameter finalAnswerParameter,

        CodeModeWorkflowConfiguration workflowConfiguration,

        RequestAnalyzerEWAgenticStep requestAnalyzerEWStep,
        AgentMemoryQueryExpanderEWAgenticStep agentMemoryQueryExpanderEWStep,
        AgentMemoryServiceEWCodeStep agentMemoryServiceEWStep,
        KnowledgeBaseQueryExpanderEWAgenticStep knowledgeBaseQueryExpanderEWStep,
        DomainsKnowledgeBaseServiceSearchEWCodeStep domainsKnowledgeBaseServiceSearchEWStep,
        RerankerEWAgenticStep rerankerEWStep,
        DomainsKnowledgeBaseDocumentsExtractorEWCodeStep domainsKnowledgeBaseDocumentsExtractorEWStep,
        DocumentationEWAgenticStep documentationEWStep,
        FunctionalAnalystEWAgenticStep functionalAnalystEWStep,
        APIsKnowledgeBaseServiceSearchEWCodeStep apisKnowledgeBaseServiceSearchEWStep,
        APIKnowledgeBaseDocumentsExtractorEWCodeStep apiKnowledgeBaseDocumentsExtractorEWStep,
        TechnicalAnalystEWAgenticStep technicalAnalystEWStep,
        CoderEWAgenticStep coderEWStep,
        JSSandboxEWCodeStep jsSandboxEWStep,
        DomainExpertEWAgenticStep domainExpertEWStep,
        PersonalAssistantEWAgenticStep personalAssistantEWStep) : IEWStepSelector
    {

        private bool _rerankerHasRun = false;

        public IEnumerable<IEWStep> NextStepsToRun()
        {
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

            if (documentationContentParameter.GetDisplayValue() == EWParameterConstants.NoDataPlaceholder)                
            {
                return [documentationEWStep];
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
        
        private IEnumerable<IEWStep> HandleTaskExecutionBranch()
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

            if (businessRequirementsParameter.GetDisplayValue() == EWParameterConstants.NoDataPlaceholder)
            {
                return [functionalAnalystEWStep, apisKnowledgeBaseServiceSearchEWStep];
            }

            if (apisKnowledgeBaseQueryResultsParameter.GetDisplayValue() != EWParameterConstants.NoDataPlaceholder
                && knowledgeBaseAPIDocumentsContentParameter.GetDisplayValue() == EWParameterConstants.NoDataPlaceholder)
            {
                return [apiKnowledgeBaseDocumentsExtractorEWStep];
            }

            if (technicalSpecificationParameter.GetDisplayValue() == EWParameterConstants.NoDataPlaceholder
                && businessRequirementsParameter.GetDisplayValue() != EWParameterConstants.NoDataPlaceholder)
            {
                return [technicalAnalystEWStep];
            }

            if (technicalAnalystRejectedParameter.ParameterValue == true
                && finalAnswerParameter.GetDisplayValue() == EWParameterConstants.NoDataPlaceholder)
            {
                return [personalAssistantEWStep];
            }

            if (generatedCodeParameter.GetDisplayValue() == EWParameterConstants.NoDataPlaceholder
                && technicalSpecificationParameter.GetDisplayValue() != EWParameterConstants.NoDataPlaceholder)
            {
                return [coderEWStep];
            }

            if (sandboxResultParameter.GetDisplayValue() == EWParameterConstants.NoDataPlaceholder
                && generatedCodeParameter.GetDisplayValue() != EWParameterConstants.NoDataPlaceholder)
            {
                return [jsSandboxEWStep];
            }

            if (workflowConfiguration.EnableDomainExpert
                && domainExpertOutputParameter.GetDisplayValue() == EWParameterConstants.NoDataPlaceholder
                && sandboxResultParameter.GetDisplayValue() != EWParameterConstants.NoDataPlaceholder)
            {
                return [domainExpertEWStep];
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
    }
}
