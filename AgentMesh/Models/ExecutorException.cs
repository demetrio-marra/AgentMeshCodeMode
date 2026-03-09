namespace AgentMesh.Models
{
    public class ExecutorException : Exception
    {
        public string ExecutorName { get; } = string.Empty;

        public ExecutorException(string executorName, string message) : base(message)
        {
            ExecutorName = executorName;
        }

        public ExecutorException(string executorName, string message, Exception exception) : base(message, exception)
        {
            ExecutorName = executorName;
        }
    }
}
