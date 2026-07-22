namespace AgentMesh.Application.Models.ChatClient
{
    public interface IAgentOutput
    {
        int TokenCount { get; set; }
        int InputTokenCount { get; set; }
        int OutputTokenCount { get; set; }
    }
}
