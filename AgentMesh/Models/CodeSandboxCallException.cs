namespace AgentMesh.Models
{
    public class CodeSandboxCallException : Exception
    {
        public string ErrorType { get; } = string.Empty;

        public CodeSandboxCallException(string errorType, string error)
            : base(error)
        {
            ErrorType = errorType;
        }
    }
}
