namespace AgentMesh.Application.Models.ChatClient
{
    public class ChatClientResponse
    {
        public string Text { get; set; } = string.Empty;
        public int TotalTokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }
    }
}
