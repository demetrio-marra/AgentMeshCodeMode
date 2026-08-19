using AgentMesh.Models;

namespace AgentMesh.Services
{
    /// <summary>
    /// Contract for a thread-safe, atomic parameter value store.
    /// Manages parameter versioning and optimistic concurrency control.
    /// All mutations go through TryCommit to ensure consistency even under concurrent step execution.
    /// </summary>
    public interface IParameterStore
    {
        /// <summary>
        /// Sets the initial values for the specified types.
        /// </summary>
        /// <param name="initialValues">A dictionary containing the initial values mapped by their corresponding types.</param>
        void SetInitialValues(IDictionary<Type, object?> initialValues);

        /// <summary>
        /// Creates an immutable snapshot of the specified parameter values at the current version.
        /// Safe to use as input for parallel step execution.
        /// </summary>
        /// <param name="parameterTypes">Types of parameters to snapshot (must be concrete EWParameter<T> implementations)</param>
        /// <returns>Snapshot containing current values and version</returns>
        ParametersSnapshot CreateSnapshot(IEnumerable<Type> parameterTypes);

        /// <summary>
        /// Atomically commits a set of parameter mutations.
        /// Uses optimistic concurrency: if expectedVersion doesn't match current version, returns conflict.
        /// </summary>
        /// <param name="stepName">Name of the step performing the commit (for audit trail)</param>
        /// <param name="mutations">Collection of parameter changes to apply</param>
        /// <returns>Collection of CommitResultItem indicating the result of each mutation</returns>
        IReadOnlyCollection<CommitResultItem> TryCommit(string stepName, IReadOnlyCollection<ParameterMutation> mutations);
    }
}
