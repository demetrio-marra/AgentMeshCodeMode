namespace AgentMesh.Models.SearchQueriesGenerator
{
    public class SearchQueriesGeneratorAgentInput
    {
        public List<ContextMessage> ContextMessages { get; set; } = [];
        public string UserLastRequest { get; set; } = string.Empty;
        public string UserIntent { get; set; } = string.Empty;
    }
}
