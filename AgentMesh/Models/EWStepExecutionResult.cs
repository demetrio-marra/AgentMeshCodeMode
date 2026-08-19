namespace AgentMesh.Models
{
    /// <summary>
    /// Result of a step execution.
    /// Contains output mutations to be committed to the parameter store, and optionally agent metrics.
    /// </summary>
    public class EWStepExecutionResult
    {
        /// <summary>
        /// Mutations produced by this step's execution.
        /// These will be atomically committed to the parameter store.
        /// </summary>
        public IReadOnlyDictionary<Type, object?> OutputMutations { get; set; } = new Dictionary<Type, object?>().AsReadOnly();
    }
}
