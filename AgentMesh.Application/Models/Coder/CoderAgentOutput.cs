using AgentMesh.Application.Models.ChatClient;
using AgentMesh.Models;

namespace AgentMesh.Application.Models.Coder
{
    public class CoderAgentOutput : IAgentOutput
    {
        public string CodeToRun { get; set; } = string.Empty;
        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }

        public Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>
            {
                { "Code to run", CodeToRun }
            };
        }
    }
}
