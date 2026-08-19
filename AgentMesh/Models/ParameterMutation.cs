namespace AgentMesh.Models
{
    /// <summary>
    /// Represents a change to a parameter value that a step wants to commit.
    /// Immutable record of: which parameter, what was the old value, what is the new value.
    /// </summary>
    public record struct ParameterMutation(
        int ParameterVersion,
        Type ParameterType,
        object? NewValue);
}
