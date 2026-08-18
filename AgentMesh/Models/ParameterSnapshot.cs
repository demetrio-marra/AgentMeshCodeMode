namespace AgentMesh.Models
{
    /// <summary>
    /// Immutable snapshot of parameter values at a point in time.
    /// Used by steps to read input parameters without affecting other concurrent steps.
    /// </summary>
    public record ParameterSnapshot(
        long Version,
        IReadOnlyDictionary<Type, object?> Values,
        DateTime CapturedAtUtc);
}
