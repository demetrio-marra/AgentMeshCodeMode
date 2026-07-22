using AgentMesh.Models;
using AgentMesh.Application.Models.KnowledgeBase;

namespace AgentMesh.Application.Models.DomainExpert
{
    public class DomainExpertAgentOutput : IAgentOutput
    {
        public string DomainExpertComment { get; set; } = string.Empty;
        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }

        public Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>
            {
                { "Domain expert comment", DomainExpertComment }
            };
        }
    }
}
