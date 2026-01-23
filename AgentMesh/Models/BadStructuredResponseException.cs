namespace AgentMesh.Models
{
    public class BadStructuredResponseException : Exception
    {
        public string RawOutput { get; set; } = string.Empty;

        public BadStructuredResponseException(string rawOutput, string message) : base(message)
        {
            RawOutput = rawOutput;
        }

        public BadStructuredResponseException(string rawOutput, string message, Exception innerException) : base(message, innerException)
        {
            RawOutput = rawOutput;
        }
    }
}
