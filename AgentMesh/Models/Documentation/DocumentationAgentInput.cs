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
            var dictionary = UserRequest.ToDictionary();
            dictionary.Add("Agent memories", AgentMemories.Any() ? ListsFormatter.ToBulletList(AgentMemories) : "(No memories)");
            dictionary.Add("Knowledge base documents content", $"Size: {KnowledgeBaseDocumentsContent.Length}");
            dictionary.Add("Language of the user", LanguageOfTheUser);

            return dictionary;
        }
    }
}

