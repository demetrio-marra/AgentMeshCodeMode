using AgentMesh.Application.Models;

namespace AgentMesh.Application.Contracts
{
    public interface IOpenAIClient
    {
        Task<OpenAIClientResponse> GenerateResponseAsync(IEnumerable<string> userInput, CancellationToken cancellationToken = default);
        Task<OpenAIClientResponse> GenerateResponseAsync(IEnumerable<AgentMessage> messages, CancellationToken cancellationToken = default);
    }
}
