using AgentMesh.Application.Configuration;
using AgentMesh.Application.Models.Workflows;
using AgentMesh.Application.Services.EWSteps;
using AgentMesh.Models;
using AgentMesh.Models.RequestAnalysis;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.Pipelines
{
    public sealed class ChatRequestPipeline(
        RequestDateTimeParameter requestDateTimeParameter,
        UserLastRequestParameter userLastRequestParameter,
        InitialContextMessagesParameter initialContextMessagesParameter,
        UserIntentParameter userIntentParameter,
        IntentCategoryParameter intentCategoryParameter,
        LanguageOfTheUserParameter languageOfTheUserParameter,
        LanguageOfTheDocumentationParameter languageOfTheDocumentationParameter,
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
        RequestRejectedReasonParameter requestRejectedReasonParameter,
        TechnicalSpecificationParameter technicalSpecificationParameter,
        APISKnowledgeBaseQueryResultsParameter apisKnowledgeBaseQueryResultsParameter,
        GeneratedCodeParameter generatedCodeParameter,
        SandboxExecutionIdParameter sandboxExecutionIdParameter,
        CodeExecutionResultTypeParameter codeExecutionResultTypeParameter,
        RequestRejectedFlagParameter requestRejectedFlagParameter,
        ExecutionErrorParameter executionErrorParameter,
        PipelineResultDataParameter pipelineResultDataParameter,
        PersonalAssistantOpeningSentenceParameter personalAssistantOpeningSentenceParameter,
        PersonalAssistantClosingSentenceParameter personalAssistantClosingSentenceParameter,
        PersonalAssistantConvenienceErrorSentenceParameter personalAssistantConvenienceErrorSentenceParameter,
        FinalAnswerParameter finalAnswerParameter,
        QMDQueryTypesDocumentationParameter qMDQueryTypesDocumentationParameter,

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
        PersonalAssistantEWAgenticStep personalAssistantEWStep,
        IWorkflowProgressNotifier workflowProgressNotifier

        ) : EWPipeline(workflowProgressNotifier, [
            requestDateTimeParameter,
            userLastRequestParameter,
            initialContextMessagesParameter,
            userIntentParameter,
            intentCategoryParameter,
            languageOfTheUserParameter,
            languageOfTheDocumentationParameter,
            conversationTopicParameter,
            userPreferencesParameter,
            userProvidedDataParameter,
            userRequestedActionsParameter,
            missingValuesParameter,
            knowledgeBaseAPIDocumentsContentParameter,
            pastMemoriesQueryParameter,
            domainsKnowledgeBaseQueryParameter,
            pastMemoriesQueryResultsParameter,
            knowledgeBaseQueryResultsParameter,
            domainsKnowledgeBaseDocumentsContentParameter,
            businessRequirementsParameter,
            requestRejectedReasonParameter,
            technicalSpecificationParameter,
            apisKnowledgeBaseQueryResultsParameter,
            generatedCodeParameter,
            sandboxExecutionIdParameter,
            codeExecutionResultTypeParameter,
            requestRejectedFlagParameter,
            executionErrorParameter,
            pipelineResultDataParameter,
            personalAssistantOpeningSentenceParameter,
            personalAssistantClosingSentenceParameter,
            personalAssistantConvenienceErrorSentenceParameter,
            finalAnswerParameter,
            qMDQueryTypesDocumentationParameter
            ]
        ), IChatRequestPipeline
    {
        public string FinalResponse => finalAnswerParameter.ParameterValue!;

        public IEnumerable<ContextMessage> InitialChatHistory { set => initialContextMessagesParameter.ParameterValue = value.ToList(); }

        public string UserLastRequest { set => userLastRequestParameter.ParameterValue = value; }

        protected override IEnumerable<IEWStep> GetNextStepsToRun()
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
