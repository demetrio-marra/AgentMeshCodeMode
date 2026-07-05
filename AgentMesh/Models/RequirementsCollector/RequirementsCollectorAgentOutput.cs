using System.Text.Json.Serialization;

namespace AgentMesh.Models.RequirementsCollector
{
    public class RequirementsCollectorAgentOutput : IAgentOutput
    {
        public IEnumerable<string> MissingPastMemories { get; set; } = [];
        public IEnumerable<RequirementsCollectorKnowledgeBase> MissingKnowledgeBaseSearchEntries { get; set; } = [];

        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }

        public class RequirementsCollectorKnowledgeBase
        {
            [JsonPropertyName("type")]
            public string Type { get; set; } = string.Empty;

            [JsonPropertyName("query")]
            public string Query { get; set; } = string.Empty;

            public override string ToString()
            {
                return $"Type: {Type}, Query: {Query}";
            }
        }
    }
}
