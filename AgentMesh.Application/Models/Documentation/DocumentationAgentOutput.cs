using AgentMesh.Application.Models.ChatClient;
using AgentMesh.Models;

namespace AgentMesh.Application.Models.Documentation
{
    public class DocumentationAgentOutput : IAgentOutput
    {
        public string? Content { get; set; }
        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }

        public Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>
            {
                { "Content", Content ?? "(nothing found)" }
            };
        }
    }
}
