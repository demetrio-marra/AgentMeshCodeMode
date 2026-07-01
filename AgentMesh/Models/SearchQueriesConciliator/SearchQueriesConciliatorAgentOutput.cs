using System.Text.Json.Serialization;

namespace AgentMesh.Models.SearchQueriesConciliator
{
    public class SearchQueriesConciliatorAgentOutput : IAgentOutput
    {
        public IEnumerable<KnowledgeBaseSearchQuery> ConciliatedKnowledgeBaseSearchQueries { get; set; } = [];
        public IEnumerable<MemorySearchQuery> ConciliatedMemorySearchQueries { get; set; } = [];
        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }

        public class KnowledgeBaseSearchQuery
        {
            [JsonPropertyName("type")]
            public string Type { get; set; } = string.Empty;

            [JsonPropertyName("query")]
            public string Query { get; set; } = string.Empty;

            [JsonPropertyName("source")]
            public string Source { get; set; } = string.Empty;

            public override string ToString()
            {
                return $"Type: {Type}, Query: {Query}, Source: {Source}";
            }
        }

        public class MemorySearchQuery
        {
            [JsonPropertyName("query")]
            public string Query { get; set; } = string.Empty;

            [JsonPropertyName("source")]
            public string Source { get; set; } = string.Empty;

            public override string ToString()
            {
                return $"Query: {Query}, Source: {Source}";
            }
        }
    }
}
