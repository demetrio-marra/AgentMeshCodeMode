namespace AgentMesh.Models.SearchQueriesGenerator
{
    public class SearchQueriesGeneratorAgentInput
    {
        public string UserIntent { get; set; } = string.Empty;
        public IEnumerable<string> SupportingIntentInformation { get; set; } = [];
        public IEnumerable<string> UserRequestDomains { get; set; } = [];
    }
}
