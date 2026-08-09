using AgentMesh.Application.Models.ChatClient;
using AgentMesh.Models.RequestAnalysis;

namespace AgentMesh.Application.Models.RequestAnalysis
{
    public class RequestAnalyzerAgentOutput : StructuredUserRequest, IAgentOutput
    {
        public int TokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }
    }
}
