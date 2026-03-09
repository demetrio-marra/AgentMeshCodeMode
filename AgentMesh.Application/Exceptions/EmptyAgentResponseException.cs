namespace AgentMesh.Application.Exceptions
{
    public class EmptyAgentResponseException : BadAgentResponseException
    {
        public EmptyAgentResponseException() : base("Empty agent response.")
        {
        }
    }
}
