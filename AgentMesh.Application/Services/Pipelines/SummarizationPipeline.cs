using AgentMesh.Application.Models.Parameters;
using AgentMesh.Application.Services.EWSteps;
using AgentMesh.Models;
using AgentMesh.Services;

namespace AgentMesh.Application.Services.Pipelines
{
    public sealed class SummarizationPipeline(IWorkflowProgressNotifier workflowProgressNotifier,

        IParameterStore parameterStore,
        IEnumerable<IEWParameterConfiguration> parameterConfigurations,

        RelevantMessagesToSaveInAgentMemoryParameter relevantMessagesToSaveInAgentMemoryParameter,
        SummarizedContentParameter summarizedContentParameter,
        SummarizedContentDatetimeParameter summarizedContentDatetimeParameter,

        ConversationSummarizerEWAgenticStep conversationSummarizerStep,
        RelevantFactsEvaluatorEWAgenticStep relevantFactsEvaluatorStep) : 

        EWPipeline(workflowProgressNotifier,
            parameterStore,
            parameterConfigurations
        ), ISummarizationPipeline
    {

        // here list all variables needed to control the execution of steps that reads and writes the same parameters.
        bool summarizationRun = false;
        bool savedToMemory = false;

        public string SummarizedContent => summarizedContentParameter.ValueAs(GetParameterRawValue(typeof(SummarizedContentParameter))) ?? string.Empty;

        public DateTime SummarizedContentDatetime => summarizedContentDatetimeParameter.ValueAs(GetParameterRawValue(typeof(SummarizedContentDatetimeParameter)));

        public void SetParameterInitialValues(string summarizationLanguage, IEnumerable<ContextMessage> chatMessagesToSummarize, DateTime requestDateTime)
        {
            SetInitialParameters(new Dictionary<Type, object?>
            {
                { typeof(SummarizeLanguageParameter), summarizationLanguage },
                { typeof(MessagesToSummarizeParameter), chatMessagesToSummarize },
                { typeof(RequestDateTimeParameter), requestDateTime }
            });
        }

        protected override IEnumerable<IEWStep> GetNextStepsToRun()
        {
            if (!summarizationRun)
            {
                summarizationRun = true;
                return [conversationSummarizerStep,
                    relevantFactsEvaluatorStep
                ];
            }

            if ((relevantMessagesToSaveInAgentMemoryParameter.ValueAs(GetParameterRawValue(typeof(SummarizedContentDatetimeParameter)))?.Any() ?? false)
                && !savedToMemory)
            {
                savedToMemory = true;
                return [conversationSummarizerStep];
            }

            return [];
        }
    }
}
