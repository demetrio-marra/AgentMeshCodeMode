namespace AgentMesh.Models.Workflows
{
    public class WorkflowStepUsageEntry
    {
        public string StepName { get; set; } = string.Empty;
        public TimeSpan Elapsed { get; set; }
        public bool IsAgentic { get; set; }
        public AgentTokenUsageEntry? TokensUsage { get; set; }
    }
}
