namespace AgentMesh.Infrastructure.QDrant
{
    public class QDrantQueriesCacheServiceConfiguration
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public bool Https { get; set; }
        public int VectorSize { get; set; }
        public int MaxResults { get; set; }
        public string QueriesCacheCollectionName { get; set; } = string.Empty;
    }
}
