namespace AgentMesh.Models.Workflows
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
