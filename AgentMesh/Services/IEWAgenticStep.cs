using AgentMesh.Models;

namespace AgentMesh.Services
{
    public interface IEWAgenticStep : IEWStep
    {
        string? AgentName { get; }

        bool CountInputTokensAsContextTokens { get; }

        bool CountOutputTokensAsContextTokens { get; }
    }
}
