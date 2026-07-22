using AgentMesh.Utils;

namespace AgentMesh.Application.Models.CodeFixer
{
    public class CodeFixerAgentInput
    {
        public string CodeToFix { get; set; } = string.Empty;

        public IEnumerable<string> Issues { get; set; } = [];

        public Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>
            {
                { "Code to fix", CodeToFix },
                { "Issues", Issues.Any() ? ListsFormatter.ToBulletList(Issues) : "(No issues provided)" }
            };
        }
    }
}
