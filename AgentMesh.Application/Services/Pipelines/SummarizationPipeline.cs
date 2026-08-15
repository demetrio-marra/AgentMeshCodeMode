using AgentMesh.Application.Models.Workflows;
using AgentMesh.Application.Services.EWSteps;
using AgentMesh.Models;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.Pipelines
{
    public sealed class SummarizationPipeline(IWorkflowProgressNotifier workflowProgressNotifier,

        MessagesToSummarizeParameter messagesToSummarizeParameter,
        RelevantMessagesToSaveInAgentMemoryParameter relevantMessagesToSaveInAgentMemoryParameter,
        SummarizeLanguageParameter summarizeLanguageParameter,
        SummarizedContentParameter summarizedContentParameter,
        SummarizedContentDatetimeParameter summarizedContentDatetimeParameter,

        ConversationSummarizerEWAgenticStep conversationSummarizerStep,
        RelevantFactsEvaluatorEWAgenticStep relevantFactsEvaluatorStep) : 

        EWPipeline(workflowProgressNotifier, [
            relevantMessagesToSaveInAgentMemoryParameter,
            messagesToSummarizeParameter,
            summarizeLanguageParameter,
            summarizedContentParameter,
            summarizedContentDatetimeParameter
            ]
        ), ISummarizationPipeline
    {

        // here list all variables needed to control the execution of steps that reads and writes the same parameters.
        bool summarizationRun = false;
        bool savedToMemory = false;

        public string SummarizedContent => summarizedContentParameter.ParameterValue!;

        public DateTime SummarizedContentDatetime => summarizedContentDatetimeParameter.ParameterValue;

        public IEnumerable<ContextMessage> ChatMessagesToSummarize { set => messagesToSummarizeParameter.ParameterValue = value.ToList(); }

        protected override IEnumerable<IEWStep> GetNextStepsToRun()
        {
            if (!summarizationRun)
            {
                summarizationRun = true;
                return [conversationSummarizerStep,
                    relevantFactsEvaluatorStep
                ];
            }

            if ((relevantMessagesToSaveInAgentMemoryParameter.ParameterValue?.Any() ?? false)
                && !savedToMemory)
            {
                savedToMemory = true;
                return [conversationSummarizerStep];
            }

            return [];
        }
    }
}
