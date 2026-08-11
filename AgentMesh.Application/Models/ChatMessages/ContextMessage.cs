namespace AgentMesh.Application.Models.ChatMessages
{
    public class ContextMessage
    {
        public ContextMessageRole Role { get; set; }
        public DateTime Date { get; set; }
        public string Text { get; set; } = string.Empty;
    }
}
