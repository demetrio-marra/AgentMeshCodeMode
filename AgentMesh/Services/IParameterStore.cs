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
        /// Creates an immutable snapshot of the specified parameter values at the current version.
        /// Safe to use as input for parallel step execution.
        /// </summary>
        /// <param name="parameterTypes">Types of parameters to snapshot (must be concrete EWParameter<T> implementations)</param>
        /// <returns>Snapshot containing current values and version</returns>
        ParameterSnapshot CreateSnapshot(IEnumerable<Type> parameterTypes);

        /// <summary>
        /// Atomically commits a set of parameter mutations.
        /// Uses optimistic concurrency: if expectedVersion doesn't match current version, returns conflict.
        /// </summary>
        /// <param name="stepName">Name of the step performing the commit (for audit trail)</param>
        /// <param name="expectedVersion">Version step expects; must match current version or commit fails</param>
        /// <param name="mutations">Collection of parameter changes to apply</param>
        /// <returns>CommitResult indicating success, new version, or conflict reason</returns>
        CommitResult TryCommit(
            string stepName,
            long expectedVersion,
            IReadOnlyCollection<ParameterMutation> mutations);

        /// <summary>
        /// Gets the current store version.
        /// </summary>
        long GetCurrentVersion();
    }
}
