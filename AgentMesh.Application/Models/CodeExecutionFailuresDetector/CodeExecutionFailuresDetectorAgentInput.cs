namespace AgentMesh.Application.Models.CodeExecutionFailuresDetector
{
    public class CodeExecutionFailuresDetectorAgentInput
    {
        public string CodeWithLineNumbers { get; set; } = string.Empty;

        public string ExecutionResult { get; set; } = string.Empty;

        public Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>
            {
                { "Code with line numbers", CodeWithLineNumbers },
                { "Execution result", ExecutionResult }
            };
        }
    }
}
