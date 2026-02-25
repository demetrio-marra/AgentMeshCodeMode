namespace AgentMesh.Infrastructure.SemanticSearch
{
    public class QDrantSemanticSearchServiceConfiguration
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public bool Https { get; set; }
        public int VectorSize { get; set; }
        public int MaxResults { get; set; }
        public string BusinessProcessesCollectionName { get; set; } = string.Empty;
    }
}
