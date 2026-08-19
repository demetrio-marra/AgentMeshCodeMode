namespace AgentMesh.Models
{
    /// <summary>
    /// Result of a TryCommit operation on the parameter store.
    /// Either succeeds with the new version, or fails with conflict (stale expectedVersion).
    /// </summary>
    public record CommitResult(
        bool Success,
        IReadOnlyCollection<CommitResultItem> CommittedMutations,
        string? ConflictReason = null);
}
