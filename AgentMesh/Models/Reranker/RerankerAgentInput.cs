using AgentMesh.Models.KnowledgeBase;
using AgentMesh.Models.RequestAnalysis;
using AgentMesh.Utils;

namespace AgentMesh.Models.Reranker
{
    public class RerankerAgentInput
    {
        public StructuredUserRequest StructuredUserRequest { get; set; } = new();
        public IEnumerable<KnowledgeBaseQueryResultItem> QueryResults { get; set; } = [];

        public Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>
            {
                { "Structured user request", System.Text.Json.JsonSerializer.Serialize(StructuredUserRequest) },
                { "Query results", QueryResults.Any() ? ListsFormatter.ToBulletList(QueryResults.Select(result => $"{result.File} {result.Title}")) : "(No query results)" }
            };
        }
    }
}
