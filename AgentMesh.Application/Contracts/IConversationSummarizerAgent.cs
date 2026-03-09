using AgentMesh.Application.Models;
using AgentMesh.Services;

namespace AgentMesh.Application.Contracts
{
    public interface IConversationSummarizerAgent : IExecutor<ConversationSummarizerAgentInput, ConversationSummarizerAgentOutput>
    {
    }
}
