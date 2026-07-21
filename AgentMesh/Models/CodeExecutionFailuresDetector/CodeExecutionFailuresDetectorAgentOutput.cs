namespace AgentMesh.Models.CodeExecutionFailuresDetector
{
    public class CodeExecutionFailuresDetectorAgentOutput : IAgentOutput
    {
        public string Analysis { get; set; } = string.Empty;
        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }

        public Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>
            {
                { "Analysis", Analysis }
            };
        }
    }
}
