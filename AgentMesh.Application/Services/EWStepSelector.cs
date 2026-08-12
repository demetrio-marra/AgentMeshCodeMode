using AgentMesh.Application.Configuration;
using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Application.Services.EWSteps;
using AgentMesh.Models;
using AgentMesh.Models.RequestAnalysis;
using AgentMesh.Services;

namespace AgentMesh.Application.Services
{
    /// <summary>
    /// To work this instance must be scoped
    /// </summary>
    /// <param name="initEWCodeStep"></param>
    /// <param name="intentCategoryParameter"></param>
    /// <param name="missingValuesParameter"></param>
    /// <param name="knowledgeBaseAPIDocumentsContentParameter"></param>
    /// <param name="pastMemoriesQueryParameter"></param>
    /// <param name="domainsKnowledgeBaseQueryParameter"></param>
    /// <param name="pastMemoriesQueryResultsParameter"></param>
    /// <param name="knowledgeBaseQueryResultsParameter"></param>
    /// <param name="domainsKnowledgeBaseDocumentsContentParameter"></param>
    /// <param name="businessRequirementsParameter"></param>
    /// <param name="technicalSpecificationParameter"></param>
    /// <param name="requestRejectedReasonParameter"></param>
    /// <param name="apisKnowledgeBaseQueryResultsParameter"></param>
    /// <param name="generatedCodeParameter"></param>
    /// <param name="pipelineResultDataParameter"></param>
    /// <param name="executionErrorParameter"></param>
    /// <param name="finalAnswerParameter"></param>
    /// <param name="workflowConfiguration"></param>
    /// <param name="requestAnalyzerEWStep"></param>
    /// <param name="agentMemoryQueryExpanderEWStep"></param>
    /// <param name="agentMemoryServiceEWStep"></param>
    /// <param name="knowledgeBaseQueryExpanderEWStep"></param>
    /// <param name="domainsKnowledgeBaseServiceSearchEWStep"></param>
    /// <param name="rerankerEWStep"></param>
    /// <param name="domainsKnowledgeBaseDocumentsExtractorEWStep"></param>
    /// <param name="documentationEWStep"></param>
    /// <param name="functionalAnalystEWStep"></param>
    /// <param name="apisKnowledgeBaseServiceSearchEWStep"></param>
    /// <param name="apiKnowledgeBaseDocumentsExtractorEWStep"></param>
    /// <param name="technicalAnalystEWStep"></param>
    /// <param name="coderEWStep"></param>
    /// <param name="jsSandboxEWStep"></param>
    /// <param name="domainExpertEWStep"></param>
    /// <param name="personalAssistantEWStep"></param>
    public class EWStepSelector(
        InitEWCodeStep initEWCodeStep,

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
        RequestRejectedReasonParameter requestRejectedReasonParameter,
        APISKnowledgeBaseQueryResultsParameter apisKnowledgeBaseQueryResultsParameter,
        GeneratedCodeParameter generatedCodeParameter,
        PipelineResultDataParameter pipelineResultDataParameter,
        ExecutionErrorParameter executionErrorParameter,
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
        public IEWCodeStep GetInitStep() => initEWCodeStep;


        // here list all variables needed to control the execution of steps that reads and writes the same parameters.
        private bool _rerankerHasRun = false;
        private bool _domainExpertHasRun = false;


        public IEnumerable<IEWStep> NextStepsToRun()
        {
            if (RunOnce([requestAnalyzerEWStep], out var steps))
            {
                return steps;
            }

            var pipelineBranch = GuessPipelineBranch();
            return pipelineBranch switch
            {
                PipelineBranchValue.OtherTopics => HandleOtherTopicsBranch(),
                PipelineBranchValue.Documenting => HandleDocumentingBranch(),
                PipelineBranchValue.TaskExecution => HandleTaskExecutionBranch(),
                _ => throw new NotImplementedException()
            };
        }


        private IEnumerable<IEWStep> HandleOtherTopicsBranch()
        {
            var steps = Enumerable.Empty<IEWStep>();

            // Equivalent to:
            // - if agent has already run, do not run it
            // - if missingValuesParameter is null or empty, do not run it
            // Conditions are AND
            if (RunOnce([agentMemoryQueryExpanderEWStep], out steps,
                () => missingValuesParameter.ParameterValue?.Any() ?? false))
            {
                return steps;
            }
           
            if (RunOnce([agentMemoryServiceEWStep], out steps,
                () => pastMemoriesQueryParameter.ParameterValue?.Any() ?? false))
            {
                return steps;
            }

            if (RunOnce([personalAssistantEWStep], out steps))
            {
                return steps;
            }

            return steps;
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

            if (pipelineResultDataParameter.GetDisplayValue() == EWParameterConstants.NoDataPlaceholder)
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

            if (requestRejectedReasonParameter.GetDisplayValue() != EWParameterConstants.NoDataPlaceholder
                && finalAnswerParameter.GetDisplayValue() == EWParameterConstants.NoDataPlaceholder)
            {
                return [personalAssistantEWStep];
            }

            if (generatedCodeParameter.GetDisplayValue() == EWParameterConstants.NoDataPlaceholder
                && technicalSpecificationParameter.GetDisplayValue() != EWParameterConstants.NoDataPlaceholder)
            {
                return [coderEWStep];
            }

            if (pipelineResultDataParameter.GetDisplayValue() == EWParameterConstants.NoDataPlaceholder
                && generatedCodeParameter.GetDisplayValue() != EWParameterConstants.NoDataPlaceholder)
            {
                return [jsSandboxEWStep];
            }

            if (workflowConfiguration.EnableDomainExpert
                && !_domainExpertHasRun
                && !executionErrorParameter.ParameterValue
                && pipelineResultDataParameter.GetDisplayValue() != EWParameterConstants.NoDataPlaceholder)
            {
                _domainExpertHasRun = true;
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
            return intentCategoryParameter.ParameterValue!.Value switch
            {
                UserIntentCategory.Other => PipelineBranchValue.OtherTopics,
                UserIntentCategory.Documentation => PipelineBranchValue.Documenting,
                UserIntentCategory.TaskExecution => PipelineBranchValue.TaskExecution,
            };
        }

        private enum PipelineBranchValue
        {
            OtherTopics,
            Documenting,
            TaskExecution
        }

        private readonly Dictionary<string, bool> _runOnceDictionary = [];
        private bool RunOnce(IEnumerable<IEWStep> steps,
            out IEnumerable<IEWStep> stepsToRun,
            Func<bool>? runCondition = null)
        {
            stepsToRun = [];
            
            if (runCondition != null
                && !runCondition())
            {
                return false;
            }

            var ls = new List<IEWStep>();
            foreach (var step in steps)
            {
                if (!_runOnceDictionary.TryGetValue(step.Name, out bool hasRun) || !hasRun)
                {
                    ls.Add(step);
                    _runOnceDictionary[step.Name] = true;
                }
            }
            stepsToRun = ls;

            return ls.Any();
        }
    }
}
