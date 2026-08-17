namespace AgentMesh.Application.Exceptions
{
    public class WorkflowExecutionException : Exception
    {
        public WorkflowExecutionException(string message) : base(message)
        {
        }

        public WorkflowExecutionException(string message, Exception exception) : base(message, exception)
        {
        }
    }
}
