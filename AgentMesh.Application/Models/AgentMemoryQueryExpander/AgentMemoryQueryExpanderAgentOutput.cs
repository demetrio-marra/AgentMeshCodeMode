using AgentMesh.Models;
using AgentMesh.Utils;

namespace AgentMesh.Application.Models.AgentMemoryQueryExpander
{
    public class AgentMemoryQueryExpanderAgentOutput : IAgentOutput
    {
        /// <summary>
        /// The expanded natural language queries to be used for retrieving records from the agent memory system.
        /// </summary>
        public IEnumerable<string> SearchQueries { get; set; } = [];

        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }

        public Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>
            {
                { "Search queries", SearchQueries.Any() ? ListsFormatter.ToBulletList(SearchQueries) : "(No search queries generated)" }
            };
        }
    }
}
