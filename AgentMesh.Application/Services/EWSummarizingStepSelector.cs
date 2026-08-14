using AgentMesh.Application.Models.Workflows;
using AgentMesh.Application.Services.EWSteps;
using AgentMesh.Services;

namespace AgentMesh.Application.Services
{
   /// <summary>
   /// Summarization selector
   /// </summary>
   /// <param name="initStep"></param>
   /// <param name="relevantMessagesToSaveInAgentMemoryParameter"></param>
   /// <param name="conversationSummarizer"></param>
   /// <param name="relevantFactsEvaluator"></param>
    public class EWSummarizingStepSelector(
        InitSummarizationEWCodeStep initStep,

        RelevantMessagesToSaveInAgentMemoryParameter relevantMessagesToSaveInAgentMemoryParameter,

        ConversationSummarizerEWAgenticStep conversationSummarizer,
        RelevantFactsEvaluatorEWAgenticStep relevantFactsEvaluator) : IEWStepSelector
    {
        public IEWCodeStep GetInitStep() => initStep;


        // here list all variables needed to control the execution of steps that reads and writes the same parameters.
        bool summarizationRun = false;
        bool savedToMemory = false;

        public IEnumerable<IEWStep> NextStepsToRun()
        {
            if (!summarizationRun)
            {
                summarizationRun = true;
                return [conversationSummarizer,
                    relevantFactsEvaluator
                ];
            }

            if ((relevantMessagesToSaveInAgentMemoryParameter.ParameterValue?.Any() ?? false)
                && !savedToMemory)
            {
                savedToMemory = true;
                return [conversationSummarizer];
            }

            return [];
        }
    }
}
