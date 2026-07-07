using System.Collections.Generic;
using AgentMesh.Models.KnowledgeBase;

namespace AgentMesh.Models.APIQueriesGenerator
{
    public class APIQueriesGeneratorAgentOutput : IAgentOutput
    {
        public IEnumerable<KnowledgeBaseQueryInputItem> APISKnowledgeBaseQuery { get; set; } = [];
        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }
    }
}
