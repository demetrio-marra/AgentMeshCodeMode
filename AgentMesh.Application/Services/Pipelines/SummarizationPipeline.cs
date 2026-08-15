using AgentMesh.Application.Models.Workflows;
using AgentMesh.Application.Services.EWSteps;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.Pipelines
{
    public sealed class SummarizationPipeline(IWorkflowProgressNotifier workflowProgressNotifier,

        MessagesToSummarizeParameter messagesToSummarizeParameter,
        RelevantMessagesToSaveInAgentMemoryParameter relevantMessagesToSaveInAgentMemoryParameter,
        SummarizeLanguageParameter summarizeLanguageParameter,
        SummarizedContentParameter summarizedContentParameter,
        SummarizedContentDatetimeParameter summarizedContentDatetimeParameter,

        InitSummarizationEWCodeStep initStep,
        ConversationSummarizerEWAgenticStep conversationSummarizerStep,
        RelevantFactsEvaluatorEWAgenticStep relevantFactsEvaluatorStep) : 
        EWPipeline(workflowProgressNotifier, [
            relevantMessagesToSaveInAgentMemoryParameter,
            messagesToSummarizeParameter,
            summarizeLanguageParameter,
            summarizedContentParameter,
            summarizedContentDatetimeParameter
            ]
        )
    {

        // here list all variables needed to control the execution of steps that reads and writes the same parameters.
        bool summarizationRun = false;
        bool savedToMemory = false;

        protected override IEWCodeStep GetInitStep() => initStep;

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
