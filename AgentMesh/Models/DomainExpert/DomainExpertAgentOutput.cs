using System.Text.Json.Serialization;

namespace AgentMesh.Models.DomainExpert
{
    public class DomainExpertAgentOutput : IAgentOutput
    {
        public IEnumerable<KnowledgeBaseAPIQuery> KnowledgeBaseAPIQueries { get; set; } = [];
        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }

        public class KnowledgeBaseAPIQuery
        {
            [JsonPropertyName("type")]
            public string Type { get; set; } = string.Empty;

            [JsonPropertyName("query")]
            public string Query { get; set; } = string.Empty;

            public override string ToString() => $"Type: {Type}, Query: {Query}";
        }
    }
}
