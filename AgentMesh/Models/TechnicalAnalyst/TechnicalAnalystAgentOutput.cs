using System.Collections.Generic;
using AgentMesh.Models.KnowledgeBase;

namespace AgentMesh.Models.TechnicalAnalyst
{
    public class TechnicalAnalystAgentOutput : IAgentOutput
    {
        public IEnumerable<KnowledgeBaseQueryInputItem> APISKnowledgeBaseQuery { get; set; } = [];
        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }
    }
}
