using AgentMesh.Models.KnowledgeBase;
using AgentMesh.Utils;

namespace AgentMesh.Models.Reranker
{
    public class RerankerAgentOutput : IAgentOutput
    {
        public IEnumerable<KnowledgeBaseQueryResultItem> QueryResults { get; set; } = [];
        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }

        public Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>
            {
                { "Query results", QueryResults.Any() ? ListsFormatter.ToBulletList(QueryResults.Select(result => $"{result.File} {result.Title}")) : "(No query results)" }
            };
        }
    }
}
