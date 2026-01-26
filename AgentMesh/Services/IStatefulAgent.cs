namespace AgentMesh.Services
{
    public interface IStatefulAgent<T>
    {
        Task SetState(T state);
        Task<T> GetState();
    }
}
