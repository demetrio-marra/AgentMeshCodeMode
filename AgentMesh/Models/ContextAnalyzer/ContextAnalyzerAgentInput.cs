using AgentMesh.Models.AgentMemory;

namespace AgentMesh.Models.ContextAnalyzer
{
    public class ContextAnalyzerAgentInput
    {
        public string UserIntent { get; set; } = string.Empty;
        public IEnumerable<AgentMemoryItem> Memories { get; set; } = Enumerable.Empty<AgentMemoryItem>();
    }
}
