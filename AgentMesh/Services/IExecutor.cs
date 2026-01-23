namespace AgentMesh.Services
{
    /// <summary>
    /// Represents an executor that can process an input of type InputDTO and produce an output of type OutputDTO asynchronously.
    /// </summary>
    /// <typeparam name="InputDTO">Input type</typeparam>
    /// <typeparam name="OutputDTO">Output type</typeparam>
    public interface IExecutor<InputDTO, OutputDTO>
    {
        Task<OutputDTO> ExecuteAsync(InputDTO input, CancellationToken cancellationToken = default);
    }
}
