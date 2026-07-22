using AgentMesh.Utils;

namespace AgentMesh.Application.Models.KnowledgeBase
{
    public class KnowledgeBaseGetDocsInput
    {
        public IEnumerable<string> FilePaths { get; set; } = [];

        public Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>
            {
                { "File paths", FilePaths.Any() ? ListsFormatter.ToBulletList(FilePaths) : "(No file paths specified)" }
            };
        }
    }
}
