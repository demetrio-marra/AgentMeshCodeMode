namespace AgentMesh.Models.Workflows
{
    public abstract class BaseWorkflowState
    {
        public List<WorkflowStepUsageEntry> TokenUsageEntries { get; set; } = [];

        public void AddTokenUsage(string agentName, int inputTokenCount, int outputTokenCount, TimeSpan? elapsed = null, string? stepName = null)
        {
            TokenUsageEntries.Add(new WorkflowStepUsageEntry
            {
                StepName = stepName ?? agentName,
                Elapsed = elapsed ?? TimeSpan.Zero,
                IsAgentic = true,
                TokensUsage = new AgentTokenUsageEntry
                {
                    AgentName = agentName,
                    InputTokens = inputTokenCount,
                    OutputTokens = outputTokenCount
                }
            });
        }

        public void AddStepUsage(string stepName, TimeSpan elapsed, bool isAgentic, AgentTokenUsageEntry? tokensUsage = null)
        {
            TokenUsageEntries.Add(new WorkflowStepUsageEntry
            {
                StepName = stepName,
                Elapsed = elapsed,
                IsAgentic = isAgentic,
                TokensUsage = tokensUsage
            });
        }
    }
}
