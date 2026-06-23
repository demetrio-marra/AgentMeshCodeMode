namespace AgentMesh.Models.Workflows
{
    public class WorkflowResult
    {
        public string Response { get; set; } = string.Empty;
        public List<WorkflowStepUsageEntry> UsageStatistics { get; set; } = new();
    }
}
