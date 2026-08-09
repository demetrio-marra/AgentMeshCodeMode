using AgentMesh.Utils;

namespace AgentMesh.Application.Models.KnowledgeBase
{
    public class KnowledgeBaseQueryInput
    {
        public IEnumerable<string> Collections { get; set; } = [];
        public IEnumerable<KnowledgeBaseQueryInputItem> Queries { get; set; } = [];
        public string? UserIntent { get; set; }

        public Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>
            {
                { "Collections", Collections.Any() ? ListsFormatter.ToBulletList(Collections) : "(No collections specified)" },
                { "Queries", Queries.Any() ? ListsFormatter.ToBulletList(Queries.Select(q => $"{q}")) : "(No queries specified)" },
                { "User Intent", UserIntent ?? "(No user intent provided)" }
            };
        }
    }
}
