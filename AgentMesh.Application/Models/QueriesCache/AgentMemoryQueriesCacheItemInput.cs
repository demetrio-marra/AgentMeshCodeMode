namespace AgentMesh.Application.Models.QueriesCache
{
    public class AgentMemoryQueriesCacheItemInput
    {
        public string Query { get; set; } = string.Empty;

        public Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>
            {
                { "Query", Query }
            };
        }
    }
}
