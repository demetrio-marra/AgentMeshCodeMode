namespace AgentMesh.Models
{
    public abstract class BadAgentResponseException : Exception
    {
        public BadAgentResponseException(string message) : base(message)
        {
        }
        public BadAgentResponseException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
