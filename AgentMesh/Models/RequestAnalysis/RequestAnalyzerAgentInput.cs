using AgentMesh.Utils;

namespace AgentMesh.Models.RequestAnalysis
{
    public class RequestAnalyzerAgentInput
    {
        public List<ContextMessage> ContextMessages { get; set; } = [];
        public string UserLastRequest { get; set; } = string.Empty;

        public Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>
            {
                { "Context messages", ContextMessages.Any() ? $"Messages count: {ContextMessages.Count}" : "(No context messages)" },
                { "User last request", UserLastRequest }
            };
        }
    }
}
