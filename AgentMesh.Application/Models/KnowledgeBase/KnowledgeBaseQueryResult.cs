using AgentMesh.Utils;

namespace AgentMesh.Models.KnowledgeBase
{
    public class KnowledgeBaseQueryResult
    {
        public IEnumerable<KnowledgeBaseQueryResultItem> Results { get; set; } = [];

        public Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>
            {
                { "Results", Results.Any() ? ListsFormatter.ToBulletList(Results.Select(r => $"{r.File} {r.Title} Relevance: {r.Relevance}")) : "(No results found)" }
            };
        }
    }
}
