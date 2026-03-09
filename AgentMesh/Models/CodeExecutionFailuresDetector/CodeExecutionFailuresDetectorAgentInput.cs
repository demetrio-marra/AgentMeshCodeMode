namespace AgentMesh.Models.CodeExecutionFailuresDetector
{
    public class CodeExecutionFailuresDetectorAgentInput
    {
        public string CodeWithLineNumbers { get; set; } = string.Empty;

        public string ExecutionResult { get; set; } = string.Empty;
    }
}
