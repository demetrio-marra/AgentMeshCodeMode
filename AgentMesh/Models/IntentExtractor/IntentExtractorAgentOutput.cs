using System.Text.Json.Serialization;

namespace AgentMesh.Models.IntentExtractor
{
    public class IntentExtractorAgentOutput : IAgentOutput
    {
        public string UserIntent { get; set; } = string.Empty;
        public IEnumerable<string> MissingPastMemories { get; set; } = [];
        public IEnumerable<IntentExtractorKnowledgeBase> MissingKnowledgeBaseSearchEntries { get; set; } = [];
        public string? LanguageOfTheUser { get; set; }

        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }

        public class IntentExtractorKnowledgeBase
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
