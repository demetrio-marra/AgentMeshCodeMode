using AgentMesh.Application.Configuration;
using AgentMesh.Application.Models.Parameters;
using AgentMesh.Application.Services.EWSteps;
using AgentMesh.Models;
using AgentMesh.Models.RequestAnalysis;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.Pipelines
{
    public sealed class ChatRequestPipeline(
        IEnumerable<IEWParameterConfiguration> parameterConfigurations,
        IParameterStore parameterStore,

        FinalAnswerParameter finalAnswerParameter,

        CodeModeWorkflowConfiguration workflowConfiguration,

        MissingValuesParameter missingValuesParameter,
        PastMemoriesQueryParameter pastMemoriesQueryParameter,
        RequestRejectedFlagParameter requestRejectedFlagParameter,
        GeneratedCodeParameter generatedCodeParameter,
        ExecutionErrorParameter executionErrorParameter,
        PipelineResultDataParameter pipelineResultDataParameter,
        IntentCategoryParameter intentCategoryParameter,
        IsSmallTalkParameter isSmallTalkParameter,

        RequestAnalyzerEWAgenticStep requestAnalyzerEWStep,
        AgentMemoryQueryExpanderEWAgenticStep agentMemoryQueryExpanderEWStep,
        AgentMemoryServiceEWCodeStep agentMemoryServiceEWStep,
        DocumentationEWAgenticStep documentationEWStep,
        CoderEWAgenticStep coderEWStep,
        JSSandboxEWCodeStep jsSandboxEWStep,
        DomainExpertEWAgenticStep domainExpertEWStep,
        PersonalAssistantEWAgenticStep personalAssistantEWStep,
        RequestDataToKnowledgeQueryEWCodeStep requestDataToKnowledgeQueryEWCodeStep,
        KnowledgeEWCodeStep knowledgeEWCodeStep,
        KnowledgeRerankerEWAgenticStep knowledgeRerankerEWAgenticStep,
        CanonicalizerEWAgenticStep canonicalizerEWAgenticStep,
        AnalystEWAgenticStep analystEWStep,
        KnowledgeQueryBuilderForCoderEWAgenticStep knowledgeQueryBuilderForCoderEWAgenticStep,
        KnowledgeForCoderRerankerEWAgenticStep knowledgeRerankerForCoderEWAgenticStep,
        KnowledgeForCoderEWCodeStep knowledgeForCoderEWCodeStep,
        IWorkflowProgressNotifier workflowProgressNotifier
        ) : EWPipeline(workflowProgressNotifier,
            parameterStore,
            parameterConfigurations
        ), IChatRequestPipeline
    {
        public void SetParameterInitialValues(string userLastRequest, IEnumerable<ContextMessage> initialChatHistory, DateTime requestDateTime)
        {
            SetInitialParameters(new Dictionary<Type, object?>
            {
                { typeof(UserLastRequestParameter), userLastRequest },
                { typeof(InitialContextMessagesParameter), initialChatHistory },
                { typeof(RequestDateTimeParameter), requestDateTime },
                { typeof(LanguageOfTheDocumentationParameter), workflowConfiguration.LanguageOfKnowledgeBase }
            });
        }

        public string FinalResponse => finalAnswerParameter.ValueAs(GetParameterRawValue(typeof(FinalAnswerParameter))) ?? string.Empty;


        protected override IEnumerable<IEWStep> GetNextStepsToRun()
        {
            if (RunOnce([requestAnalyzerEWStep], out var steps))
            {
                return steps;
            }

            if (workflowConfiguration.EnableMemoryService
          && RunOnce([agentMemoryQueryExpanderEWStep], out steps,
          () => missingValuesParameter.ValueAs(GetParameterRawValue(typeof(MissingValuesParameter)))?.Any() ?? false))
            {
                return steps;
            }

            if (workflowConfiguration.EnableMemoryService
                && RunOnce([agentMemoryServiceEWStep], out steps,
              () => pastMemoriesQueryParameter.ValueAs(GetParameterRawValue(typeof(PastMemoriesQueryParameter)))?.Any() ?? false))
            {
                return steps;
            }

            var isSmallTalk = isSmallTalkParameter.ValueAs(GetParameterRawValue(typeof(IsSmallTalkParameter)))!.Value;
            if (isSmallTalk)
            {
                return HandleOtherTopicsBranch();
            }

            if (RunOnce([requestDataToKnowledgeQueryEWCodeStep], out steps))
            {
                return steps;
            }

            if (RunOnce([knowledgeEWCodeStep], out steps))
            {
                return steps;
            }

            if (RunOnce([knowledgeRerankerEWAgenticStep], out steps))
            {
                return steps;
            }

            if (RunOnce([canonicalizerEWAgenticStep], out steps))
            {
                return steps;
            }

            var pipelineBranch = GuessPipelineBranch();
            return pipelineBranch switch
            {
                PipelineBranchValue.Documenting => HandleDocumentingBranch(),
                PipelineBranchValue.TaskExecution => HandleTaskExecutionBranch(),
                _ => throw new NotImplementedException()
            };
        }


        private IEnumerable<IEWStep> HandleOtherTopicsBranch()
        {
            var steps = Enumerable.Empty<IEWStep>();

            if (RunOnce([personalAssistantEWStep], out steps))
            {
                return steps;
            }

            return steps;
        }

        private IEnumerable<IEWStep> HandleDocumentingBranch()
        {
            var steps = Enumerable.Empty<IEWStep>();

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

            if (RunOnce([analystEWStep], out steps))
            {
                return steps;
            }

            var requestWasRejected = requestRejectedFlagParameter.ValueAs(GetParameterRawValue(typeof(RequestRejectedFlagParameter)));

            if (RunOnce([knowledgeQueryBuilderForCoderEWAgenticStep], out steps,
                () => !requestWasRejected))
            {
                return steps;
            }

            if (RunOnce([knowledgeForCoderEWCodeStep], out steps))
            {
                return steps;
            }

            if (RunOnce([knowledgeRerankerForCoderEWAgenticStep], out steps))
            {
                return steps;
            }

            if (RunOnce([coderEWStep], out steps,
                () => !requestWasRejected))
            {
                return steps;
            }

            if (RunOnce([jsSandboxEWStep], out steps,
                () => !requestWasRejected
                    && generatedCodeParameter.ValueAs(GetParameterRawValue(typeof(GeneratedCodeParameter))) != null))
            {
                return steps;
            }

            if (workflowConfiguration.EnableDomainExpert
                && RunOnce([domainExpertEWStep], out steps,
                () => !requestWasRejected
                    && workflowConfiguration.EnableDomainExpert
                    && !executionErrorParameter.ValueAs(GetParameterRawValue(typeof(ExecutionErrorParameter)))
                    && pipelineResultDataParameter.ValueAs(GetParameterRawValue(typeof(PipelineResultDataParameter))) != null))
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
            return intentCategoryParameter.ValueAs(GetParameterRawValue(typeof(IntentCategoryParameter)))!.Value switch
            {
                UserIntentCategory.Documentation => PipelineBranchValue.Documenting,
                UserIntentCategory.TaskExecution => PipelineBranchValue.TaskExecution,
                _ => throw new NotImplementedException(),
            };
        }

        private enum PipelineBranchValue
        {
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
