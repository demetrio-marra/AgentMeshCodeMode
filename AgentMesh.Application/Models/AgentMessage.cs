namespace AgentMesh.Application.Models
{
    public class AgentMessage
    {
        public AgentMessageRole Role { get; set; } = AgentMessageRole.User;
        public string Content { get; set; } = string.Empty;
    }
}
