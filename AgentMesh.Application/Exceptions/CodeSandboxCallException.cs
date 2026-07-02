namespace AgentMesh.Application.Exceptions
{
    public class CodeSandboxCallException(string errorType, string error) : Exception(error)
    {
        public string ErrorType { get; } = errorType;
    }
}
