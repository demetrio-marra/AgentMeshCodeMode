namespace AgentMesh.Application.Models.ChatMessages
{
    public class AgentResponse<T>
    {
        public required T Result { get; set; }
        public int TotalTokenCount { get; set; }
        public int InputTokenCount { get; set; }
        public int OutputTokenCount { get; set; }
    }
}
