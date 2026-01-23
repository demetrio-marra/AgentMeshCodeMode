namespace AgentMesh.Application.Models
{
    public class OpenAIClientResponse
    {
        public string Text { get; set; } = string.Empty;
        public int TotalTokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }
    }
}
