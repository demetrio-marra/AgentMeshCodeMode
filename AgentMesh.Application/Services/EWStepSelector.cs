using AgentMesh.Application.Configuration;
using AgentMesh.Application.Models.Workflows.Parameters;
using AgentMesh.Application.Services.EWSteps;
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
        RequestRejectedFlagParameter requestRejectedFlagParameter,
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
            var steps = Enumerable.Empty<IEWStep>();

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

            if (RunOnce([knowledgeBaseQueryExpanderEWStep], out steps))
            {
                return steps;
            }

            if (RunOnce([domainsKnowledgeBaseServiceSearchEWStep], out steps,
                () => domainsKnowledgeBaseQueryParameter.ParameterValue?.Any() ?? false))
            {
                return steps;
            }

            if (RunOnce([rerankerEWStep], out steps))
            {
                return steps;
            }

            if (RunOnce([domainsKnowledgeBaseDocumentsExtractorEWStep], out steps,
                () => knowledgeBaseQueryResultsParameter.ParameterValue?.Any() ?? false))
            {
                return steps;
            }

            if (RunOnce([documentationEWStep], out steps))
            {
                return steps;
            }

            if (RunOnce([personalAssistantEWStep], out steps))
            {
                return steps;
            }

            return steps;
        }

        private IEnumerable<IEWStep> HandleTaskExecutionBranch()
        {
            var steps = Enumerable.Empty<IEWStep>();

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

            if (RunOnce([knowledgeBaseQueryExpanderEWStep], out steps))
            {
                return steps;
            }

            if (RunOnce([domainsKnowledgeBaseServiceSearchEWStep], out steps,
                () => domainsKnowledgeBaseQueryParameter.ParameterValue?.Any() ?? false))
            {
                return steps;
            }

            if (RunOnce([rerankerEWStep], out steps))
            {
                return steps;
            }

            if (RunOnce([domainsKnowledgeBaseDocumentsExtractorEWStep], out steps,
                () => knowledgeBaseQueryResultsParameter.ParameterValue?.Any() ?? false))
            {
                return steps;
            }

            if (RunOnce([functionalAnalystEWStep, apisKnowledgeBaseServiceSearchEWStep], out steps))
            {
                return steps;
            }

            var requestWasRejected = requestRejectedFlagParameter.ParameterValue;

            if (RunOnce([apiKnowledgeBaseDocumentsExtractorEWStep], out steps,
                    () => !requestWasRejected
                        && (apisKnowledgeBaseQueryResultsParameter.ParameterValue?.Any() ?? false)))
            {
                return steps;
            }

            if (RunOnce([technicalAnalystEWStep], out steps,
                () => !requestWasRejected))
            {
                return steps;
            }

            if (RunOnce([coderEWStep], out steps,
                () => !requestWasRejected
                    && technicalSpecificationParameter.ParameterValue != null))
            {
                return steps;
            }

            if (RunOnce([jsSandboxEWStep], out steps,
                () => !requestWasRejected
                    && generatedCodeParameter.ParameterValue != null))
            {
                return steps;
            }

            if (RunOnce([domainExpertEWStep], out steps,
                () => !requestWasRejected
                    && workflowConfiguration.EnableDomainExpert
                    && !executionErrorParameter.ParameterValue
                    && pipelineResultDataParameter.ParameterValue != null))
            {
                return steps;
            }

            if (RunOnce([personalAssistantEWStep], out steps))
            {
                return steps;
            }

            return steps;
        }

        private PipelineBranchValue GuessPipelineBranch()
        {
            return intentCategoryParameter.ParameterValue!.Value switch
            {
                UserIntentCategory.Other => PipelineBranchValue.OtherTopics,
                UserIntentCategory.Documentation => PipelineBranchValue.Documenting,
                UserIntentCategory.TaskExecution => PipelineBranchValue.TaskExecution,
                _ => throw new NotImplementedException(),
            };
        }

        private enum PipelineBranchValue
        {
            OtherTopics,
            Documenting,
            TaskExecution
        }

        private readonly Dictionary<string, int> _runCountDictionary = [];
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
                if (!_runCountDictionary.TryGetValue(step.Name, out int runCount) || runCount <= 0)
                {
                    ls.Add(step);
                    _runCountDictionary[step.Name] = runCount + 1;
                }
            }
            stepsToRun = ls;

            return ls.Count != 0;
        }
    }
}
