using AgentMesh.Models;
using AgentMesh.Utils;
using AgentMesh.Models.KnowledgeBase;

namespace AgentMesh.Application.Models.KnowledgeBaseQueryExpander
{
    public class KnowledgeBaseQueryExpanderAgentOutput : IAgentOutput
    {
        public IEnumerable<KnowledgeBaseQueryInputItem> SearchQueries { get; set; } = [];

        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }

        public Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>
            {
                { "Search queries", SearchQueries.Any() ? ListsFormatter.ToBulletList(SearchQueries.Select(q => $"{q.Query} [{q.SearchType}]")) : "(No search queries generated)" }
            };
        }
    }
}
