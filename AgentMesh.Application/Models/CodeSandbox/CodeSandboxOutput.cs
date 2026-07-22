namespace AgentMesh.Application.Models.CodeSandbox
{
    public class CodeSandboxOutput
    {
        public string Result { get; set; } = string.Empty;
        public string ExecutionId { get; set; } = string.Empty;

        public Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>
            {
                { "Result", Result },
                { "Execution id", ExecutionId }
            };
        }
    }
}
