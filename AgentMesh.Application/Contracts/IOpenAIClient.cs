using AgentMesh.Application.Models.ChatClient;
using AgentMesh.Application.Models.ChatMessages;

namespace AgentMesh.Application.Contracts
{
    public interface IOpenAIClient
    {
        Task<ChatClientResponse> GenerateResponseAsync(IEnumerable<string> userInput, CancellationToken cancellationToken = default);
        Task<ChatClientResponse> GenerateResponseAsync(IEnumerable<AgentMessage> messages, CancellationToken cancellationToken = default);
    }
}
