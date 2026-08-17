namespace AgentMesh.Services
{
    public interface IEWCodeStep : IEWStep
    {
        Task ExecuteAsync(CancellationToken cancellationToken = default);
    }
}
