using AgentMesh.Application.Models.Costs;
using AgentMesh.Models;

namespace AgentMesh.Application.Models.Workflows
{
    public readonly record struct WorkflowResult
    {
        public string Message { get; init; }
        public IEnumerable<EWStepStatisticsRecord> MainPipelineStepsData { get; init; }
        public IEnumerable<AgentExecutionCost> AgentsCostData { get; init; }
        public int CountOfMessages { get; init; }
        public int CountOfTokens { get; init; }
        public bool ContextSummarizerHasRun { get; init; }
        public int? CountOfMessagesBeforeSummarization { get; init; }
        public int? CountOfTokensBeforeSummarization { get; init; }
        public decimal CumulatedCost { get; init; }
    }
}
