using AgentMesh.Models.RequestAnalysis;
using AgentMesh.Utils;

namespace AgentMesh.Models.Documentation
{
    public class DocumentationAgentInput
    {
        public StructuredUserRequest UserRequest { get; set; } = new();
        public IEnumerable<string> AgentMemories { get; set; } = [];
        public string KnowledgeBaseDocumentsContent { get; set; } = string.Empty;
        public string LanguageOfTheUser { get; set; } = string.Empty;

        public Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>
            {
                { "User request", System.Text.Json.JsonSerializer.Serialize(UserRequest) },
                { "Agent memories", AgentMemories.Any() ? ListsFormatter.ToBulletList(AgentMemories) : "(No memories)" },
                { "Knowledge base documents content", $"Size: {KnowledgeBaseDocumentsContent.Length}" },
                { "Language of the user", LanguageOfTheUser }
            };
        }
    }
}

