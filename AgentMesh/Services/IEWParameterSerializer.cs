namespace AgentMesh.Services
{
    public interface IEWParameterSerializer
    {
        string Serialize<T>(T obj);
    }
}
