namespace AgentMesh.Models
{
    public class CodeExecutionFailuresDetectorAgentInput
    {
        public string CodeWithLineNumbers { get; set; } = string.Empty;

        public string ExecutionResult { get; set; } = string.Empty;
    }
}
