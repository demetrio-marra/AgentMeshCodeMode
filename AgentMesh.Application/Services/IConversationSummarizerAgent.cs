using AgentMesh.Application.Models;
using AgentMesh.Services;

namespace AgentMesh.Application.Services
{
    public interface IConversationSummarizerAgent : IAgent<ConversationSummarizerAgentInput, ConversationSummarizerAgentOutput>
    {
    }
}
