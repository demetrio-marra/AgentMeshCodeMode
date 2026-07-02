namespace AgentMesh.Models.QueriesCache
{
    public class AgentMemoryQueriesCacheItem
    {
        public string FoundQuery { get; set; } = string.Empty;
        public string SearchedQuery { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;
        public double Relevance { get; set; }
    }
}
