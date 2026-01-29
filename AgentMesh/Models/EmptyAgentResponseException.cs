namespace AgentMesh.Models
{
    public class EmptyAgentResponseException : BadAgentResponseException
    {
        public EmptyAgentResponseException() : base("Empty agent response.")
        {
        }
    }
}
