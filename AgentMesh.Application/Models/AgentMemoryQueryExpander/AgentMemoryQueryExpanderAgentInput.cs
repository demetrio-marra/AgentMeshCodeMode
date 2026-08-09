using AgentMesh.Application.Models.AgentMemory;
using AgentMesh.Utils;

namespace AgentMesh.Application.Models.AgentMemoryQueryExpander
{
    public class AgentMemoryQueryExpanderAgentInput
    {
        /// <summary>
        /// The memory topics or subjects extracted from the user's request, used as seeds to generate natural language search queries for the agent memory system.
        /// </summary>
        public IEnumerable<AgentMemoryItem> MemoryTopics { get; set; } = [];

        public Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>
            {
                { "Memory topics", MemoryTopics.Any() ? ListsFormatter.ToBulletList(MemoryTopics.Select(m => m.Memory)) : "(No memory topics)" }
            };
        }
    }
}
