using AgentMesh.Models.KnowledgeBase;
using AgentMesh.Utils;

namespace AgentMesh.Application.Models.KnowledgeBase
{
    public class KnowledgeBaseGetDocsOutput
    {
        public IEnumerable<KnowledgeBaseGetDocsOutputItem> Results { get; set; } = [];

        public Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>
            {
                { "Results", Results.Any() ? ListsFormatter.ToBulletList(Results.Select(r => r.File)) : "(No results found)" }
            };
        }
    }
}
